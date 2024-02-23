using Latios;
using Latios.Kinemation;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

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

                GetAspect<TransformAspect>(entities[i]).TranslateWorld(posXYZ);
            }
        }

        state.EntityManager.DestroyEntity(QueryBuilder().WithAll<Spawner>().Build());
    }
}

// Usually, optimized skeletons are spawned either in LatiosWorldSyncGroup or at the syncPoint via command buffer.
// But for comparisons, we are spawning at the beginning of SimulationSystemGroup.
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[UpdateAfter(typeof(SpawnSystem))]
[BurstCompile]
public partial struct FixOptimizedSkeletonSpawningSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = state.GetLatiosWorldUnmanaged().syncPoint.CreateEnableCommandBuffer();
        var dcb = new DisableCommandBuffer(Allocator.Temp);

        // Using InternalSourceGen as a quick hack to detect that binding hasn't happened yet
        foreach (var entity in QueryBuilder().WithAll<OptimizedBoneTransform>().WithNone<Latios.Kinemation.InternalSourceGen.DependentSkinnedMesh>().Build().ToEntityArray(Allocator
                                                                                                                                                                           .Temp))
        {
            dcb.Add(entity);
            ecb.Add(entity);
        }
        dcb.Playback(state.EntityManager, GetBufferLookup<LinkedEntityGroup>(true));
    }
}

