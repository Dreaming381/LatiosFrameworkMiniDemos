using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Post-Jam Notes:
// This is the system that builds the rigid body CollisionLayer and resets all rigid body
// properties for the simulation step. It is mostly copied from Free Parking. But is has one
// notable change where gravity is disabled if the rigid body is also a character controller.
// That check is no longer necessary, but the code was left in.
// Also, this code has residue of a test to see if the player could ride on moving blocks.

using static Unity.Entities.SystemAPI;

namespace BB
{
    [BurstCompile]
    public partial struct BuildBodiesLayerSystem : ISystem, ISystemNewScene
    {
        LatiosWorldUnmanaged latiosWorld;
        EntityQuery          m_query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
            m_query     = state.Fluent().With<RigidBody>(true).PatchQueryForBuildingCollisionLayer().Build();
        }

        public void OnNewScene(ref SystemState state)
        {
            latiosWorld.sceneBlackboardEntity.AddOrSetCollectionComponentAndDisposeOld<RigidBodyCollisionLayer>(default);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rigidBodyCount         = m_query.CalculateEntityCountWithoutFiltering();
            var rigidBodyColliderArray = CollectionHelper.CreateNativeArray<ColliderBody>(rigidBodyCount, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
            var rigidBodyAabbArray     = CollectionHelper.CreateNativeArray<Aabb>(rigidBodyCount, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);

            new BuildRigidBodiesJob
            {
                timeScaledGravity = Time.DeltaTime * -9.81f,
                deltaTime         = Time.DeltaTime,
                bucketCalculator  = new CollisionLayerBucketIndexCalculator(BuildCollisionLayerConfig.defaultSettings),
                colliderArray     = rigidBodyColliderArray,
                aabbArray         = rigidBodyAabbArray,
                ccHandle          = GetComponentTypeHandle<FirstPersonControllerStats>(true),
            }.ScheduleParallel();
            state.Dependency = Physics.BuildCollisionLayer(rigidBodyColliderArray, rigidBodyAabbArray)
                               .ScheduleParallel(out var rigidBodyLayer, Allocator.Persistent, state.Dependency);
            latiosWorld.sceneBlackboardEntity.SetCollectionComponentAndDisposeOld(new RigidBodyCollisionLayer { layer = rigidBodyLayer });
        }

        [BurstCompile]
        partial struct BuildRigidBodiesJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public float timeScaledGravity;
            public float deltaTime;

            public CollisionLayerBucketIndexCalculator                        bucketCalculator;
            [ReadOnly] public ComponentTypeHandle<FirstPersonControllerStats> ccHandle;

            [NativeDisableParallelForRestriction] public NativeArray<ColliderBody> colliderArray;
            [NativeDisableParallelForRestriction] public NativeArray<Aabb>         aabbArray;

            bool isCC;

            public void Execute(Entity entity, [EntityIndexInQuery] int index, ref RigidBody rigidBody, in Collider collider, in WorldTransform transform)
            {
                if (!isCC)
                {
                    rigidBody.velocity.linear.y += timeScaledGravity;
                    //rigidBody.velocity.linear.x  = 1f; // riding test
                }

                var aabb                   = Physics.AabbFrom(in collider, in transform.worldTransform);
                rigidBody.angularExpansion = UnitySim.AngularExpansionFactorFrom(in collider);
                var motionExpansion        = new UnitySim.MotionExpansion(in rigidBody.velocity, deltaTime, rigidBody.angularExpansion);
                aabb                       = motionExpansion.ExpandAabb(aabb);
                rigidBody.motionExpansion  = motionExpansion;

                rigidBody.motionStabilizer                   = UnitySim.MotionStabilizer.kDefault;
                rigidBody.numOtherSignificantBodiesInContact = 0;
                rigidBody.bucketIndex                        = bucketCalculator.BucketIndexFrom(aabb);

                colliderArray[index] = new ColliderBody
                {
                    collider  = collider,
                    transform = transform.worldTransform,
                    entity    = entity
                };
                aabbArray[index] = aabb;

                var localCenterOfMass = UnitySim.LocalCenterOfMassFrom(in collider);
                var localInertia      = UnitySim.LocalInertiaTensorFrom(in collider, transform.stretch);
                UnitySim.ConvertToWorldMassInertia(in transform.worldTransform,
                                                   in localInertia,
                                                   localCenterOfMass,
                                                   rigidBody.mass.inverseMass,
                                                   out rigidBody.mass,
                                                   out rigidBody.inertialPoseWorldTransform);
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                isCC = chunk.Has(ref ccHandle);
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
            {
            }
        }
    }
}

