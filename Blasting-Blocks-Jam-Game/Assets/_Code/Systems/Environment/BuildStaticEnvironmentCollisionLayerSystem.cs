using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Post-Jam Notes:
// This is a fairly standard way to build the static CollisionLayer.
// It is rebuilt every frame because blocks can freeze.

namespace BB
{
    [BurstCompile]
    public partial struct BuildStaticEnvironmentCollisionLayerSystem : ISystem, ISystemNewScene
    {
        LatiosWorldUnmanaged latiosWorld;

        BuildCollisionLayerTypeHandles m_handles;
        EntityQuery                    m_query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
            m_handles   = new BuildCollisionLayerTypeHandles(ref state);
            m_query     = state.Fluent().With<StaticEnvironmentTag>(true).PatchQueryForBuildingCollisionLayer().Build();
        }

        public void OnNewScene(ref SystemState state)
        {
            latiosWorld.sceneBlackboardEntity.AddOrSetCollectionComponentAndDisposeOld<StaticEnvironmentCollisionLayer>(default);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            m_handles.Update(ref state);
            state.Dependency =
                Physics.BuildCollisionLayer(m_query, m_handles).ScheduleParallel(out var layer, Allocator.Persistent, state.Dependency);
            latiosWorld.sceneBlackboardEntity.SetCollectionComponentAndDisposeOld(new StaticEnvironmentCollisionLayer { layer = layer });
        }
    }
}

