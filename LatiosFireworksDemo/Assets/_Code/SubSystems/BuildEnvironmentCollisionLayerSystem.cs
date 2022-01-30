using Latios;
using Latios.Psyshock;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class BuildEnvironmentCollisionLayerSystem : SubSystem
    {
        EntityQuery m_query;

        protected override void OnCreate()
        {
            m_query = Fluent.WithAll<EnvironmentTag>(true).PatchQueryForBuildingCollisionLayer().Build();
        }

        public override bool ShouldUpdateSystem()
        {
            return !sceneBlackboardEntity.HasCollectionComponent<EnvironmentCollisionLayer>();
        }

        protected override void OnUpdate()
        {
            Dependency = Physics.BuildCollisionLayer(m_query, this).ScheduleParallel(out var layer, Allocator.Persistent, Dependency);

            sceneBlackboardEntity.AddCollectionComponent(new EnvironmentCollisionLayer { layer = layer });
        }
    }
}

