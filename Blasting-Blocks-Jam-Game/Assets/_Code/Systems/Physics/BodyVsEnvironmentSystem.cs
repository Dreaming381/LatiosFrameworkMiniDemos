using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Post-Jam Notes:
// This is the system that runs FindPairs between the rigid bodies against the environment
// and adds the collisions to the PairStream. The code is mostly copied from Free Parking.

using static Unity.Entities.SystemAPI;

namespace BB
{
    [BurstCompile]
    public partial struct BodyVsEnvironmentSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var pairStream                   = latiosWorld.worldBlackboardEntity.GetCollectionComponent<PhysicsPairStream>(false).pairStream;
            var rigidBodyLayer               = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<RigidBodyCollisionLayer>(true).layer;
            var environmentLayer             = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<StaticEnvironmentCollisionLayer>(true);
            var findBodyEnvironmentProcessor = new FindBodyVsEnvironmentProcessor
            {
                bodyLookup       = GetComponentLookup<RigidBody>(true),
                pairStream       = pairStream.AsParallelWriter(),
                deltaTime        = Time.DeltaTime,
                inverseDeltaTime = math.rcp(Time.DeltaTime),
            };
            state.Dependency = Physics.FindPairs(in rigidBodyLayer, in environmentLayer.layer, in findBodyEnvironmentProcessor).ScheduleParallelUnsafe(state.Dependency);
        }

        struct FindBodyVsEnvironmentProcessor : IFindPairsProcessor
        {
            [ReadOnly] public ComponentLookup<RigidBody> bodyLookup;
            public PairStream.ParallelWriter             pairStream;
            public float                                 deltaTime;
            public float                                 inverseDeltaTime;

            DistanceBetweenAllCache distanceBetweenAllCache;

            public void Execute(in FindPairsResult result)
            {
                ref readonly var rigidBodyA = ref bodyLookup.GetRefRO(result.entityA).ValueRO;

                var maxDistance = UnitySim.MotionExpansion.GetMaxDistance(in rigidBodyA.motionExpansion);
                Physics.DistanceBetweenAll(result.colliderA, result.transformA, result.colliderB, result.transformB, maxDistance, ref distanceBetweenAllCache);
                foreach (var distanceResult in distanceBetweenAllCache)
                {
                    var contacts = UnitySim.ContactsBetween(result.colliderA, result.transformA, result.colliderB, result.transformB, in distanceResult);
                    if (contacts.contactCount < 1 || contacts.contactCount > 32)
                    {
                        UnityEngine.Debug.Log($"Bad contacts: typeA: {result.colliderA.type}, typeB: {result.colliderB.type}");
                        continue;
                    }

                    ref var streamData           = ref pairStream.AddPairAndGetRef<ContactStreamData>(result.pairStreamKey, true, false, out var pair);
                    streamData.contactParameters = pair.Allocate<UnitySim.ContactJacobianContactParameters>(contacts.contactCount, NativeArrayOptions.UninitializedMemory);
                    streamData.contactImpulses   = pair.Allocate<float>(contacts.contactCount, NativeArrayOptions.ClearMemory);
                    streamData.hitPoint          = distanceResult.hitpointB;
                    streamData.hit               = false;
                    pair.userByte                = 0;

                    UnitySim.BuildJacobian(streamData.contactParameters.AsSpan(),
                                           out streamData.bodyParameters,
                                           rigidBodyA.inertialPoseWorldTransform,
                                           in rigidBodyA.velocity,
                                           in rigidBodyA.mass,
                                           RigidTransform.identity,
                                           default,
                                           default,
                                           contacts.contactNormal,
                                           contacts.AsSpan(),
                                           rigidBodyA.coefficientOfRestitution,
                                           rigidBodyA.coefficientOfFriction,
                                           UnitySim.kMaxDepenetrationVelocityDynamicStatic,
                                           9.81f,
                                           deltaTime,
                                           inverseDeltaTime);
                }
            }
        }
    }
}

