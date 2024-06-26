using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Post-Jam Notes:
// This was added very late into the jam. The "reload" concept allowed us to delete all dynamic blocks.
// This could clear out a path if the player accidentally trapped themselves. There's also a level restart,
// in which we are using the "reload the current scene" trick.

using static Unity.Entities.SystemAPI;

namespace BB
{
    [BurstCompile]
    public partial struct ResetAndRestartSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;
        EntityQuery          m_query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
            m_query     = state.Fluent().With<FiredBlock>().Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();
            bool reload            = false;
            bool restart           = false;
            var  reloadSoundEntity = Entity.Null;
            var  playerEntity      = Entity.Null;
            foreach ((var firstPersonDesiredActions, var stats, var entity) in Query<FirstPersonDesiredActions, FirstPersonControllerStats>().WithEntityAccess())
            {
                reload  |= firstPersonDesiredActions.reload;
                restart |= firstPersonDesiredActions.restart;
                if (stats.reloadSound.entity != Entity.Null)
                    reloadSoundEntity = stats.reloadSound;
                playerEntity          = entity;
            }

            if (restart)
            {
                var ecb                                                                             = latiosWorld.syncPoint.CreateEntityCommandBuffer();
                var currentScene                                                                    = latiosWorld.worldBlackboardEntity.GetComponentData<CurrentScene>();
                ecb.AddComponent(latiosWorld.sceneBlackboardEntity, new RequestLoadScene { newScene = currentScene.current });
            }

            if (reload)
            {
                var ecb = latiosWorld.syncPoint.CreateEntityCommandBuffer();
                ecb.DestroyEntity(m_query, EntityQueryCaptureMode.AtRecord);
                if (reloadSoundEntity != Entity.Null)
                {
                    var newEntity                                   = ecb.Instantiate(reloadSoundEntity);
                    ecb.AddComponent(newEntity, new Parent { parent = playerEntity });
                }
            }
        }
    }
}

