using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class BuildGridSystem : SubSystem
    {
        EntityQuery m_query;

        public override bool ShouldUpdateSystem()
        {
            return !m_query.IsEmptyIgnoreFilter;
        }

        protected override void OnUpdate()
        {
            GridConfiguration config = default;

            Entities.WithStoreEntityQueryInField(ref m_query).ForEach((in GridConfiguration gridConfig) =>
            {
                config = gridConfig;
            }).Run();

            EntityManager.RemoveComponent<GridConfiguration>(m_query);

            var grid = new Grid(config.rows, config.cellsPerRow, Allocator.Persistent);
            sceneBlackboardEntity.AddCollectionComponent(grid);

            var icb = new InstantiateCommandBuffer<Translation>(Allocator.TempJob);

            new InitializeTiles
            {
                gridConfig = config,
                icb        = icb.AsParallelWriter()
            }.ScheduleParallel(config.cellsPerRow * config.rows, 32, default).Complete();

            icb.Playback(EntityManager);
            Dependency = icb.Dispose(default);

            Entities.ForEach((Entity entity, in GridPosition position) =>
            {
                grid[position.position.x, position.position.y] = entity;
            }).Schedule();
        }

        [BurstCompile]
        struct InitializeTiles : IJobFor
        {
            public GridConfiguration gridConfig;

            public InstantiateCommandBuffer<Translation>.ParallelWriter icb;

            public void Execute(int index)
            {
                int row        = index / gridConfig.cellsPerRow;
                int indexInRow = index % gridConfig.cellsPerRow;

                if (((row + indexInRow) & 0x1) == 0)
                {
                    icb.Add(gridConfig.prefabTile0, new Translation { Value = new float3(indexInRow, 0f, row) }, index);
                }
                else
                {
                    icb.Add(gridConfig.prefabTile1, new Translation { Value = new float3(indexInRow, 0f, row) }, index);
                }
            }
        }
    }
}

