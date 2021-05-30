using Latios;
using Latios.Myri;
using Latios.Psyshock;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class CollisionSystem : SubSystem
    {
        EntityQuery m_colliderQuery;
        EntityQuery m_triggerQuery;

        protected override void OnCreate()
        {
            m_colliderQuery =
                Fluent.WithAll<TranslationalVelocity>(false).WithAll<DynamicColor>(false).WithAll<AngularVelocity>(true).PatchQueryForBuildingCollisionLayer().Build();
            m_triggerQuery = Fluent.WithAll<Trigger>(true).PatchQueryForBuildingCollisionLayer().Build();
        }

        protected override void OnUpdate()
        {
            var icbNoParent   = latiosWorld.syncPoint.CreateInstantiateCommandBuffer<Translation, AudioSourceOneShot>();
            var icbWithParent = latiosWorld.syncPoint.CreateInstantiateCommandBuffer<Parent, AudioSourceOneShot>();
            icbWithParent.AddComponentTag<LocalToParent>();

            Dependency = Physics.BuildCollisionLayer(m_colliderQuery, this).ScheduleParallel(out var colliderLayer, Allocator.TempJob, Dependency);
            Dependency = Physics.BuildCollisionLayer(m_triggerQuery, this).ScheduleParallel(out var triggerLayer, Allocator.TempJob, Dependency);

            var collisionsProcessor = new CollisionsProcessor
            {
                velCdfe         = GetComponentDataFromEntity<TranslationalVelocity>(),
                colorCdfe       = GetComponentDataFromEntity<DynamicColor>(),
                angVelCdfe      = GetComponentDataFromEntity<AngularVelocity>(),
                hitSoundCdfe    = GetComponentDataFromEntity<HitSound>(),
                audioSourceCdfe = GetComponentDataFromEntity<AudioSourceOneShot>(),
                icbNoParent     = icbNoParent.AsParallelWriter(),
                icbWithParent   = icbWithParent.AsParallelWriter()
            };

            var triggerProcessor = new TriggerProcessor
            {
                colorCdfe   = GetComponentDataFromEntity<DynamicColor>(),
                triggerCdfe = GetComponentDataFromEntity<Trigger>()
            };

            Dependency = Physics.FindPairs(colliderLayer, collisionsProcessor).ScheduleParallel(Dependency);
            Dependency = Physics.FindPairs(colliderLayer, triggerLayer, triggerProcessor).ScheduleParallel(Dependency);

            Dependency = colliderLayer.Dispose(Dependency);
            Dependency = triggerLayer.Dispose(Dependency);
        }

        struct CollisionsProcessor : IFindPairsProcessor
        {
            public PhysicsComponentDataFromEntity<DynamicColor>           colorCdfe;
            public PhysicsComponentDataFromEntity<TranslationalVelocity>  velCdfe;
            [ReadOnly] public ComponentDataFromEntity<AngularVelocity>    angVelCdfe;
            [ReadOnly] public ComponentDataFromEntity<HitSound>           hitSoundCdfe;
            [ReadOnly] public ComponentDataFromEntity<AudioSourceOneShot> audioSourceCdfe;

            public InstantiateCommandBuffer<Translation, AudioSourceOneShot>.ParallelWriter icbNoParent;
            public InstantiateCommandBuffer<Parent, AudioSourceOneShot>.ParallelWriter      icbWithParent;

            public void Execute(FindPairsResult result)
            {
                if (Physics.DistanceBetween(result.bodyA.collider, result.bodyA.transform, result.bodyB.collider, result.bodyB.transform, 0f, out var distanceResult))
                {
                    float3 separationVector = distanceResult.hitpointB - distanceResult.hitpointA;

                    var velA = velCdfe[result.entityA];
                    var velB = velCdfe[result.entityB];

                    var angVelA = angVelCdfe[result.entityA];
                    var angVelB = angVelCdfe[result.entityB];
                    var colorA  = colorCdfe[result.entityA];
                    var colorB  = colorCdfe[result.entityB];

                    float colorFactor =
                        math.saturate(math.dot(math.normalizesafe(velA.velocity, 0f), -distanceResult.normalB) + math.dot(math.normalizesafe(velB.velocity,
                                                                                                                                             0f), -distanceResult.normalA));
                    colorFactor += math.saturate(-math.forward(angVelA.velocity).z - math.forward(angVelB.velocity).z);
                    colorFactor  = math.saturate(colorFactor) * 0.02f;

                    var addA = colorFactor * colorB.color;
                    var addB = colorFactor * colorA.color;

                    colorA.color = math.saturate(colorA.color + addA);
                    colorB.color = math.saturate(colorB.color + addB);

                    colorCdfe[result.entityA] = colorA;
                    colorCdfe[result.entityB] = colorB;

                    // This collision resolver produces janky results. A better solution would be a fully elastic solver.
                    // Such a solver is planned for Psyshock.
                    if (math.dot(velA.velocity, separationVector) > 0f)
                    {
                        var backup               = velA.velocity;
                        velA.velocity           -= 2f * math.projectsafe(velA.velocity, math.normalizesafe(separationVector, 0f), 0f);
                        velA.velocity            = math.normalizesafe(velA.velocity, math.normalizesafe(separationVector, math.forward())) * math.length(backup);
                        velCdfe[result.entityA]  = velA;
                    }
                    if (math.dot(velB.velocity, -separationVector) > 0f)
                    {
                        var backup               = velB.velocity;
                        velB.velocity           += 2f * math.projectsafe(velB.velocity, math.normalizesafe(separationVector, 0f), 0f);
                        velB.velocity            = math.normalizesafe(velB.velocity, math.normalizesafe(separationVector, math.forward())) * math.length(backup);
                        velCdfe[result.entityB]  = velB;
                    }

                    if (hitSoundCdfe.HasComponent(result.entityA))
                    {
                        var hitSoundPrefab       = hitSoundCdfe[result.entityA].prefab;
                        var hitSoundContact      = audioSourceCdfe[hitSoundPrefab];
                        var hitSoundCore         = hitSoundContact;
                        hitSoundContact.volume  *= 0.5f;
                        hitSoundCore.volume     *= 0.25f;
                        var   aabb               = Physics.CalculateAabb(result.bodyA.collider, RigidTransform.identity);
                        float scale              = math.cmax(aabb.max - aabb.min);
                        hitSoundCore.innerRange *= scale;
                        hitSoundCore.outerRange *= scale;
                        var translation          = new Translation { Value = distanceResult.hitpointA };
                        var parent               = new Parent { Value = result.entityA };
                        icbNoParent.Add(hitSoundPrefab, translation, hitSoundContact, result.jobIndex);
                        icbWithParent.Add(hitSoundPrefab, parent, hitSoundCore, result.jobIndex);
                    }

                    if (hitSoundCdfe.HasComponent(result.entityB))
                    {
                        var hitSoundPrefab       = hitSoundCdfe[result.entityB].prefab;
                        var hitSoundContact      = audioSourceCdfe[hitSoundPrefab];
                        var hitSoundCore         = hitSoundContact;
                        hitSoundContact.volume  *= 0.5f;
                        hitSoundCore.volume     *= 0.25f;
                        var   aabb               = Physics.CalculateAabb(result.bodyB.collider, RigidTransform.identity);
                        float scale              = math.cmax(aabb.max - aabb.min);
                        hitSoundCore.innerRange *= scale;
                        hitSoundCore.outerRange *= scale;
                        var translation          = new Translation { Value = distanceResult.hitpointB };
                        var parent               = new Parent { Value = result.entityB };
                        icbNoParent.Add(hitSoundPrefab, translation, hitSoundContact, result.jobIndex);
                        icbWithParent.Add(hitSoundPrefab, parent, hitSoundCore, result.jobIndex);
                    }
                }
            }
        }

        // Assumes collider is A and trigger is B
        struct TriggerProcessor : IFindPairsProcessor
        {
            public PhysicsComponentDataFromEntity<DynamicColor> colorCdfe;
            [ReadOnly] public ComponentDataFromEntity<Trigger>  triggerCdfe;

            public void Execute(FindPairsResult result)
            {
                if (Physics.DistanceBetween(result.bodyA.collider, result.bodyA.transform, result.bodyB.collider, result.bodyB.transform, 0f, out _))
                {
                    var color                  = colorCdfe[result.entityA];
                    color.color.xyz           += triggerCdfe[result.entityB].colorDeltas;
                    colorCdfe[result.entityA]  = color;
                }
            }
        }
    }
}

