using Latios;
using Latios.Psyshock;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class KillOnCollideWithEnvironmentSystem : SubSystem
    {
        EntityQuery m_query;

        protected override void OnUpdate()
        {
            var bodies = new NativeArray<ColliderBody>(m_query.CalculateEntityCount(), Allocator.TempJob);
            var aabbs  = new NativeArray<Aabb>(bodies.Length, Allocator.TempJob);
            Entities.WithAll<DieOnCollideWithEnvironmentTag, TimeToLive>().WithStoreEntityQueryInField(ref m_query)
            .ForEach((Entity entity, int entityInQueryIndex, in PreviousTranslation previousTranslation, in Translation translation, in Rotation rotation, in Collider collider) =>
            {
                var startTransform         = new RigidTransform(rotation.Value, previousTranslation.previousTranslation);
                var endTransfrom           = new RigidTransform(rotation.Value, translation.Value);
                var startAabb              = Physics.AabbFrom(collider, startTransform);
                var endAabb                = Physics.AabbFrom(collider, endTransfrom);
                bodies[entityInQueryIndex] = new ColliderBody
                {
                    collider  = collider,
                    entity    = entity,
                    transform = startTransform
                };
                aabbs[entityInQueryIndex] = Physics.CombineAabb(startAabb, endAabb);
            }).ScheduleParallel();

            Dependency           = Physics.BuildCollisionLayer(bodies, aabbs).ScheduleParallel(out var bulletLayer, Allocator.TempJob, Dependency);
            Dependency           = bodies.Dispose(Dependency);
            Dependency           = aabbs.Dispose(Dependency);
            var environmentLayer = sceneBlackboardEntity.GetCollectionComponent<EnvironmentCollisionLayer>(true).layer;

            var processor = new DestroyBulletsThatHitWallsProcessor
            {
                timeToLiveCdfe  = GetComponentDataFromEntity<TimeToLive>(),
                translationCdfe = GetComponentDataFromEntity<Translation>(true)
            };

            Dependency = Physics.FindPairs(bulletLayer, environmentLayer, processor).ScheduleParallel(Dependency);
            Dependency = bulletLayer.Dispose(Dependency);
        }

        // Assumes a is entity to kill and b is environment
        struct DestroyBulletsThatHitWallsProcessor : IFindPairsProcessor
        {
            public PhysicsComponentDataFromEntity<TimeToLive>      timeToLiveCdfe;
            [ReadOnly] public ComponentDataFromEntity<Translation> translationCdfe;

            public void Execute(FindPairsResult result)
            {
                var transA = translationCdfe[result.entityA];
                if (Physics.ColliderCast(result.bodyA.collider, result.bodyA.transform, transA.Value, result.bodyB.collider, result.bodyB.transform, out _))
                {
                    timeToLiveCdfe[result.entityA] = new TimeToLive { timeToLive = 0f };
                }
            }
        }
    }
}

