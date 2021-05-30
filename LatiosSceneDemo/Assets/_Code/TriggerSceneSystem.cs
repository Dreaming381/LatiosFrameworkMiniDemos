using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class TriggerSceneSystem : SubSystem
    {
        protected override void OnUpdate()
        {
            NativeReference<float3> playerPosition = new NativeReference<float3>(Allocator.TempJob);
            var                     ecb            = latiosWorld.syncPoint.CreateEntityCommandBuffer();

            Entities.WithAll<Player>().ForEach((in Translation translation) =>
            {
                playerPosition.Value = translation.Value;
            }).Schedule();

            Entity wbe = worldBlackboardEntity;

            Entities.ForEach((Entity entity, in Translation translation, in Trigger trigger) =>
            {
                if (math.distance(translation.Value, playerPosition.Value) < trigger.radius)
                {
                    ecb.AddComponent(entity, new RequestLoadScene { newScene = trigger.sceneToSwitchTo });
                    if (trigger.isDeath)
                    {
                        var deathCount = GetComponent<DeathCounter>(wbe);
                        deathCount.deathCount++;
                        SetComponent(wbe, deathCount);
                    }
                }
            }).WithDisposeOnCompletion(playerPosition).Schedule();
        }
    }
}

