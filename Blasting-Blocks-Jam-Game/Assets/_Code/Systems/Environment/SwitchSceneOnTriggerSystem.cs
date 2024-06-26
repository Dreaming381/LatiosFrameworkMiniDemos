using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Post-Jam Notes:
// We build the collision layer for a single query because it was easier code to write,
// not because it had any meaningful performance advantage.

using static Unity.Entities.SystemAPI;

namespace BB
{
    [BurstCompile]
    public partial struct SwitchSceneOnTriggerSystem : ISystem
    {
        LatiosWorldUnmanaged           latiosWorld;
        EntityQuery                    m_triggerQuery;
        BuildCollisionLayerTypeHandles m_handles;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld    = state.GetLatiosWorldUnmanaged();
            m_triggerQuery = state.Fluent().With<GoToSceneOnPlayerEnter>(true).PatchQueryForBuildingCollisionLayer().Build();
            m_handles      = new BuildCollisionLayerTypeHandles(ref state);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = latiosWorld.syncPoint.CreateEntityCommandBuffer();
            m_handles.Update(ref state);
            state.Dependency = Physics.BuildCollisionLayer(m_triggerQuery, m_handles).ScheduleParallel(out var triggerLayer, state.WorldUpdateAllocator, state.Dependency);
            new Job
            {
                ecb           = ecb,
                triggerLookup = GetComponentLookup<GoToSceneOnPlayerEnter>(true),
                triggerLayer  = triggerLayer
            }.Schedule();
        }

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            [ReadOnly] public CollisionLayer                          triggerLayer;
            [ReadOnly] public ComponentLookup<GoToSceneOnPlayerEnter> triggerLookup;
            public EntityCommandBuffer                                ecb;

            public void Execute(in WorldTransform transform, in FirstPersonControllerStats stats)
            {
                var capsule = new CapsuleCollider(new float3(0f, stats.capsuleRadius, 0f), new float3(0f, stats.capsuleHeight - stats.capsuleRadius, 0f), stats.capsuleRadius);
                if (Physics.DistanceBetween(capsule, transform.worldTransform, triggerLayer, 0f, out _, out var info))
                {
                    ecb.AddComponent(info.entity, new RequestLoadScene { newScene = triggerLookup[info.entity].scene });
                }
            }
        }
    }
}

