using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]  // Ensure this happens before any transform system.
[BurstCompile]
public partial struct SpawnSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var spawner in Query<Spawner>())
        {
            var   dim         = (int)math.ceil(math.sqrt(spawner.count));
            float startOffset = (dim - 1) * spawner.spacing / 2;
            var   entities    = state.EntityManager.Instantiate(spawner.prefab, spawner.count, Allocator.Temp);
            for (int i = 0; i < spawner.count; i++)
            {
                float2 position = new int2(i / dim, i % dim);
                float2 posXZ    = startOffset - position * spawner.spacing;
                float3 posXYZ   = new float3(posXZ.x, 0f, posXZ.y);

                GetComponentRW<LocalTransform>(entities[i]).ValueRW.Position = posXYZ;
            }
        }

        state.EntityManager.DestroyEntity(QueryBuilder().WithAll<Spawner>().Build());
    }
}

