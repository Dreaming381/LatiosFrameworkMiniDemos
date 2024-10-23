using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace Dragons.QvvsSamples.Tutorial.Systems
{
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct WorldSpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!UnityEngine.Input.GetButtonDown("Fire1"))
                return;

            var icb = new InstantiateCommandBuffer<WorldTransform>(state.WorldUpdateAllocator);
            foreach ((var spawner, var spawnerTransform) in Query<WorldSpawner, WorldTransform>())
            {
                // Start with the base WorldTransform from the prefab to preserve scale and stretch
                var newTransform                      = GetComponent<WorldTransform>(spawner.spawnable);
                newTransform.worldTransform.position += qvvs.TransformPoint(spawnerTransform.worldTransform, spawner.spawnOffsetFromPlayer);
                icb.Add(spawner.spawnable, newTransform);
            }
            icb.Playback(state.EntityManager);
        }
    }
}

