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
    public partial struct ChildSpawnerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!UnityEngine.Input.GetButtonDown("Fire1"))
                return;

            var icb = new InstantiateCommandBuffer<Parent, LocalTransform>(state.WorldUpdateAllocator);
            foreach ((var spawner, var entity) in Query<ChildSpawner>().WithAbsent<Child>().WithEntityAccess())
            {
                // Start with the base WorldTransform from the prefab to preserve scale and stretch
                var prefabWorldTransform = GetComponent<WorldTransform>(spawner.spawnable);
                var newTransform         = new LocalTransform {
                    localTransform       = new TransformQvs(prefabWorldTransform.position, prefabWorldTransform.rotation, prefabWorldTransform.scale)
                };
                newTransform.localTransform.position           += spawner.localOffset;
                icb.Add(spawner.spawnable, new Parent { parent  = entity}, newTransform);
            }
            icb.Playback(state.EntityManager);
        }
    }
}

