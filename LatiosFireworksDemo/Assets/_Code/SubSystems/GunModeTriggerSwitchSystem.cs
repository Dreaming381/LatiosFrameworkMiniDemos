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
    public class GunModeTriggerSwitchSystem : SubSystem
    {
        EntityQuery m_triggerQuery;
        EntityQuery m_actorQuery;

        protected override void OnCreate()
        {
            m_triggerQuery = Fluent.WithAll<GunMode>(true).PatchQueryForBuildingCollisionLayer().Build();
        }

        protected override void OnUpdate()
        {
            var bodies = new NativeArray<ColliderBody>(m_actorQuery.CalculateEntityCount(), Allocator.TempJob);

            Entities.ForEach((Entity entity, int entityInQueryIndex, in MoveState state, in MoveStats stats, in Translation trans) =>
            {
                var height = state.height;
                var pos    = trans.Value;
                var radius = stats.radius;

                bodies[entityInQueryIndex] = new ColliderBody
                {
                    entity    = entity,
                    collider  = new CapsuleCollider(new float3(0f, radius, 0f), new float3(0f, height - radius, 0f), radius),
                    transform = new RigidTransform(quaternion.identity, pos)
                };
            }).WithStoreEntityQueryInField(ref m_actorQuery).Schedule();

            Dependency = Physics.BuildCollisionLayer(bodies).ScheduleParallel(out var actorLayer, Allocator.TempJob, Dependency);
            Dependency = Physics.BuildCollisionLayer(m_triggerQuery, this).ScheduleParallel(out var triggerLayer, Allocator.TempJob, Dependency);

            var processor = new ActorVsTriggerProcessor
            {
                gunModeCdfe = GetComponentDataFromEntity<GunMode>(),
                sbe         = sceneBlackboardEntity
            };

            Dependency = Physics.FindPairs(actorLayer, triggerLayer, processor).ScheduleSingle(Dependency);
            Dependency = bodies.Dispose(Dependency);
            Dependency = actorLayer.Dispose(Dependency);
            Dependency = triggerLayer.Dispose(Dependency);
        }

        struct ActorVsTriggerProcessor : IFindPairsProcessor
        {
            public Entity                           sbe;
            public ComponentDataFromEntity<GunMode> gunModeCdfe;

            public void Execute(FindPairsResult result)
            {
                if (Physics.DistanceBetween(result.bodyA.collider, result.bodyA.transform, result.bodyB.collider, result.bodyB.transform, 0f, out _))
                {
                    gunModeCdfe[sbe] = gunModeCdfe[result.entityB];
                }
            }
        }
    }
}

