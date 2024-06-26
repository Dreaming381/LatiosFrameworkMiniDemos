using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Post-Jam Notes:
// This code does the solving of the PairStream data. It is mostly based on Free Parking,
// but also includes the solver for locking the rotation for the character controller
// (no longer used) as well as some extra logic for keeping track of hits to spawn audio
// on the last solver iteration. Contacts were buggy during the jam, so there's a lot of
// debug residue left over in this system as well.

using static Unity.Entities.SystemAPI;

namespace BB
{
    [BurstCompile]
    public partial struct SolveBodiesSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
            latiosWorld.worldBlackboardEntity.AddOrSetCollectionComponentAndDisposeOld(new PhysicsPairStream
            {
                pairStream = new PairStream(BuildCollisionLayerConfig.defaultSettings, Allocator.Persistent)
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var pairStream     = latiosWorld.worldBlackboardEntity.GetCollectionComponent<PhysicsPairStream>(false).pairStream;
            int numIterations  = 4;
            var solveProcessor = new SolveBodiesProcessor
            {
                colliderLookup         = GetComponentLookup<Collider>(true),
                worldTransformLookup   = GetComponentLookup<WorldTransform>(true),
                entityStorageLookup    = GetEntityStorageInfoLookup(),
                rigidBodyLookup        = GetComponentLookup<RigidBody>(false),
                invNumSolverIterations = math.rcp(numIterations),
                deltaTime              = Time.DeltaTime,
                firstIteration         = true,
                lastIteration          = false,
                icb                    = latiosWorld.syncPoint.CreateInstantiateCommandBuffer<WorldTransform>().AsParallelWriter(),
            };
            var stabilizerJob = new StabilizeRigidBodiesJob
            {
                firstIteration    = true,
                timeScaledGravity = Time.DeltaTime * -9.81f
            };
            for (int i = 0; i < numIterations; i++)
            {
                state.Dependency = Physics.ForEachPair(in pairStream, in solveProcessor).ScheduleParallel(state.Dependency);
                stabilizerJob.ScheduleParallel();
                solveProcessor.firstIteration = false;
                solveProcessor.lastIteration  = i + 2 == numIterations;
                stabilizerJob.firstIteration  = false;
            }

            new IntegrateRigidBodiesJob { deltaTime = Time.DeltaTime }.ScheduleParallel();

            latiosWorld.UpdateCollectionComponentDependency<PhysicsPairStream>(latiosWorld.worldBlackboardEntity, state.Dependency, false);
            latiosWorld.worldBlackboardEntity.SetCollectionComponentAndDisposeOld(new PhysicsPairStream
            {
                pairStream = new PairStream(BuildCollisionLayerConfig.defaultSettings, Allocator.Persistent)
            });
        }

        struct SolveBodiesProcessor : IForEachPairProcessor
        {
            [ReadOnly] public ComponentLookup<WorldTransform> worldTransformLookup;
            [ReadOnly] public ComponentLookup<Collider>       colliderLookup;
            [ReadOnly] public EntityStorageInfoLookup         entityStorageLookup;

            public PhysicsComponentLookup<RigidBody> rigidBodyLookup;
            public float                             invNumSolverIterations;
            public float                             deltaTime;
            public bool                              firstIteration;
            public bool                              lastIteration;

            public InstantiateCommandBuffer<WorldTransform>.ParallelWriter icb;

            public void Execute(ref PairStream.Pair pair)
            {
                if (!rigidBodyLookup.HasComponent(pair.entityA))
                {
                    UnityEngine.Debug.Log("Pair A was corrupted");
                    //UnityEngine.Debug.Log(
                    //    $"A {pair.entityA.ToFixedString()}, B {pair.entityB.ToFixedString()} aIsRW: {pair.aIsRW}, bIsRW: {pair.bIsRW}, streamIndex: {pair.streamIndex}, userUshort: {pair.userUShort}, userByte: {pair.userByte}");
                    //var archetype = entityStorageLookup[pair.entityA].Chunk.Archetype.ToString();
                    //UnityEngine.Debug.Log(archetype);
                    return;
                }
                ref var rigidBodyA = ref rigidBodyLookup.GetRW(pair.entityA).ValueRW;

                if (pair.userByte == 0)
                {
                    ref var streamData = ref pair.GetRef<ContactStreamData>();
                    if (pair.bIsRW)
                    {
                        if (!rigidBodyLookup.HasComponent(pair.entityB))
                        {
                            UnityEngine.Debug.Log("Pair B was corrupted");
                            return;
                        }
                        ref var rigidBodyB       = ref rigidBodyLookup.GetRW(pair.entityB).ValueRW;
                        var     initialVelocityA = rigidBodyA.velocity;
                        var     initialVelocityB = rigidBodyB.velocity;
                        var     hit              = UnitySim.SolveJacobian(ref rigidBodyA.velocity,
                                                                      in rigidBodyA.mass,
                                                                      in rigidBodyA.motionStabilizer,
                                                                      ref rigidBodyB.velocity,
                                                                      in rigidBodyB.mass,
                                                                      in rigidBodyB.motionStabilizer,
                                                                      streamData.contactParameters.AsSpan(),
                                                                      streamData.contactImpulses.AsSpan(),
                                                                      in streamData.bodyParameters,
                                                                      false,
                                                                      invNumSolverIterations,
                                                                      out var impulses);
                        streamData.hit |= hit && impulses.combinedContactPointsImpulse > 0.2f;
                        if (math.length(rigidBodyA.velocity.linear) + math.length(rigidBodyB.velocity.linear) >
                            3f * (math.length(initialVelocityA.linear) + math.length(initialVelocityB.linear)) + 3f)
                        {
                            var colliderA  = colliderLookup[pair.entityA];
                            var transformA = worldTransformLookup[pair.entityA];
                            var colliderB  = colliderLookup[pair.entityB];
                            var transformB = worldTransformLookup[pair.entityB];
                            //var text       = PhysicsDebug.LogDistanceBetween(colliderA, transformA.worldTransform, colliderB, transformB.worldTransform, 5f);
                            //var fixedText  = new FixedString4096Bytes(text.AsReadOnly());
                            //UnityEngine.Debug.Log($"{fixedText}");
                        }
                        if (firstIteration)
                        {
                            if (UnitySim.IsStabilizerSignificantBody(rigidBodyA.mass.inverseMass, rigidBodyB.mass.inverseMass))
                                rigidBodyA.numOtherSignificantBodiesInContact++;
                            if (UnitySim.IsStabilizerSignificantBody(rigidBodyB.mass.inverseMass, rigidBodyA.mass.inverseMass))
                                rigidBodyB.numOtherSignificantBodiesInContact++;
                        }
                    }
                    else
                    {
                        if (streamData.contactParameters.length < 0 || streamData.contactParameters.length > 32 || streamData.contactImpulses.length < 0 ||
                            streamData.contactImpulses.length > 32)
                        {
                            UnityEngine.Debug.Log("streamData was corrupted");
                            //UnityEngine.Debug.Log(
                            //    $"A {pair.entityA.ToFixedString()}, B {pair.entityB.ToFixedString()} aIsRW: {pair.aIsRW}, bIsRW: {pair.bIsRW}, streamIndex: {pair.streamIndex}, userUshort: {pair.userUShort}, userByte: {pair.userByte}");
                            //var archetype = entityStorageLookup[pair.entityA].Chunk.Archetype.ToString();
                            //UnityEngine.Debug.Log(archetype);
                            return;
                        }
                        UnitySim.Velocity environmentVelocity = default;
                        var               initialVelocityA    = rigidBodyA.velocity;
                        var               hit                 = UnitySim.SolveJacobian(ref rigidBodyA.velocity,
                                                                         in rigidBodyA.mass,
                                                                         in rigidBodyA.motionStabilizer,
                                                                         ref environmentVelocity,
                                                                         default,
                                                                         UnitySim.MotionStabilizer.kDefault,
                                                                         streamData.contactParameters.AsSpan(),
                                                                         streamData.contactImpulses.AsSpan(),
                                                                         in streamData.bodyParameters,
                                                                         false,
                                                                         invNumSolverIterations,
                                                                         out var impulses);
                        streamData.hit |= hit && impulses.combinedContactPointsImpulse > 0.2f;
                        if (math.length(rigidBodyA.velocity.linear) > 3f * math.length(initialVelocityA.linear) + 3f)
                        {
                            var colliderA  = colliderLookup[pair.entityA];
                            var transformA = worldTransformLookup[pair.entityA];
                            var colliderB  = colliderLookup[pair.entityB];
                            var transformB = worldTransformLookup[pair.entityB];
                            //var text       = PhysicsDebug.LogDistanceBetween(colliderA, transformA.worldTransform, colliderB, transformB.worldTransform, 5f);
                            //var fixedText  = new FixedString4096Bytes(text.AsReadOnly());
                            //UnityEngine.Debug.Log($"{fixedText}");
                        }
                        if (firstIteration)
                        {
                            if (UnitySim.IsStabilizerSignificantBody(rigidBodyA.mass.inverseMass, 0f))
                                rigidBodyA.numOtherSignificantBodiesInContact++;
                        }
                    }

                    if (streamData.hit && lastIteration && rigidBodyA.bumpSound.entity != Entity.Null)
                    {
                        icb.Add(rigidBodyA.bumpSound, new WorldTransform { worldTransform = new TransformQvvs(streamData.hitPoint, quaternion.identity) }, pair.streamIndex);
                    }
                }
                else
                {
                    ref var           rotationConstraintData = ref pair.GetRef<UnitySim.Rotation3DConstraintJacobianParameters>();
                    UnitySim.Velocity fakeVelocity           = default;
                    UnitySim.SolveJacobian(ref rigidBodyA.velocity, rigidBodyA.mass, ref fakeVelocity, default, rotationConstraintData, deltaTime, 1f / deltaTime);
                }
            }
        }

        [BurstCompile]
        partial struct StabilizeRigidBodiesJob : IJobEntity
        {
            public bool  firstIteration;
            public float timeScaledGravity;

            public void Execute(ref RigidBody rigidBody)
            {
                UnitySim.UpdateStabilizationAfterSolverIteration(ref rigidBody.motionStabilizer,
                                                                 ref rigidBody.velocity,
                                                                 rigidBody.mass.inverseMass,
                                                                 rigidBody.angularExpansion,
                                                                 rigidBody.numOtherSignificantBodiesInContact,
                                                                 new float3(0f, timeScaledGravity, 0f),
                                                                 new float3(0f, -1f, 0f),
                                                                 UnitySim.kDefaultVelocityClippingFactor,
                                                                 UnitySim.kDefaultInertialScalingFactor,
                                                                 firstIteration);
            }
        }

        [BurstCompile]
        partial struct IntegrateRigidBodiesJob : IJobEntity
        {
            public float deltaTime;

            public void Execute(TransformAspect transform, ref RigidBody rigidBody)
            {
                var previousInertialPose = rigidBody.inertialPoseWorldTransform;
                if (!math.all(math.isfinite(rigidBody.velocity.linear)))
                    rigidBody.velocity.linear = float3.zero;
                if (!math.all(math.isfinite(rigidBody.velocity.angular)))
                    rigidBody.velocity.angular = float3.zero;
                UnitySim.Integrate(ref rigidBody.inertialPoseWorldTransform, ref rigidBody.velocity, rigidBody.linearDamping, rigidBody.angularDamping, deltaTime);
                transform.worldTransform = UnitySim.ApplyInertialPoseWorldTransformDeltaToWorldTransform(transform.worldTransform,
                                                                                                         in previousInertialPose,
                                                                                                         in rigidBody.inertialPoseWorldTransform);
            }
        }
    }
}

