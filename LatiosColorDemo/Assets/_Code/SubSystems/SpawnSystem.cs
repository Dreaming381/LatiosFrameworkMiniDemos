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
    public class SpawnSystem : SubSystem
    {
        EntityQuery m_query;

        protected override void OnUpdate()
        {
            var icb  = latiosWorld.syncPoint.CreateInstantiateCommandBuffer<Translation, Rotation, NonUniformScale, Trigger, Collider>();
            var ecb  = latiosWorld.syncPoint.CreateEntityCommandBuffer();
            var icbp = icb.AsParallelWriter();
            var ecbp = ecb.AsParallelWriter();

            var halfExtents = sceneBlackboardEntity.GetComponentData<WorldBounds>().halfExtents;

            Entities.WithStoreEntityQueryInField(ref m_query).ForEach((int entityInQueryIndex, in Spawner spawner) =>
            {
                Random random = new Random(spawner.seed);

                if (HasComponent<DynamicColor>(spawner.prefab))
                {
                    for (int i = 0; i < spawner.spawnCount; i++)
                    {
                        Translation trans = new Translation { Value = random.NextFloat3(-halfExtents, halfExtents) };

                        TranslationalVelocity linear = new TranslationalVelocity
                        {
                            velocity = random.NextFloat3Direction() * random.NextFloat(spawner.minMaxLinearVelocity.x, spawner.minMaxLinearVelocity.y)
                        };

                        Rotation rot = new Rotation { Value = random.NextQuaternionRotation() };

                        AngularVelocity angular = new AngularVelocity { velocity = random.NextQuaternionRotation() };

                        DynamicColor color = new DynamicColor { color = spawner.color };

                        var collider = GetComponent<Collider>(spawner.prefab);

                        NonUniformScale scale = default;

                        if (collider.type == ColliderType.Box)
                        {
                            collider =
                                Physics.ScaleCollider(collider, new PhysicsScale(random.NextFloat3(0f, math.max(spawner.minMaxUniformScale.y, spawner.minAreaNonUniformScale))));

                            BoxCollider box = collider;
                            while (math.max(math.max(box.halfSize.x * box.halfSize.y, box.halfSize.x * box.halfSize.z),
                                            box.halfSize.y * box.halfSize.z) < spawner.minAreaNonUniformScale)
                            {
                                box.halfSize = random.NextFloat3(0f, math.max(spawner.minMaxUniformScale.y, spawner.minAreaNonUniformScale));
                            }
                            scale.Value = box.halfSize * 2f;
                            collider    = box;
                        }
                        else
                        {
                            scale.Value = random.NextFloat(spawner.minMaxUniformScale.x, spawner.minMaxUniformScale.y);

                            collider = Physics.ScaleCollider(collider, new PhysicsScale
                            {
                                scale = scale.Value,
                                state = PhysicsScale.State.Uniform
                            });
                        }

                        // Todo: This should be an ICB, but ICB only supports up to 5 components due to limited ComponentTypes constructors.
                        var entity = ecbp.Instantiate(entityInQueryIndex, spawner.prefab);
                        ecbp.AddComponent(entityInQueryIndex, entity, trans);
                        ecbp.AddComponent(entityInQueryIndex, entity, rot);
                        ecbp.AddComponent(entityInQueryIndex, entity, linear);
                        ecbp.AddComponent(entityInQueryIndex, entity, angular);
                        ecbp.AddComponent(entityInQueryIndex, entity, color);
                        ecbp.AddComponent(entityInQueryIndex, entity, scale);
                        ecbp.AddComponent(entityInQueryIndex, entity, collider);
                    }
                }
                else
                {
                    for (int i = 0; i < spawner.spawnCount; i++)
                    {
                        Translation trans = new Translation { Value = random.NextFloat3(-halfExtents, halfExtents) };

                        Rotation rot = new Rotation { Value = random.NextQuaternionRotation() };

                        Trigger trigger = new Trigger { colorDeltas = (spawner.color.xyz - 0.5f) / 10f };

                        var collider = GetComponent<Collider>(spawner.prefab);

                        NonUniformScale scale = default;

                        if (collider.type == ColliderType.Box)
                        {
                            collider =
                                Physics.ScaleCollider(collider, new PhysicsScale(random.NextFloat3(0f, math.max(spawner.minMaxUniformScale.y, spawner.minAreaNonUniformScale))));

                            BoxCollider box = collider;
                            while (math.max(math.max(box.halfSize.x * box.halfSize.y, box.halfSize.x * box.halfSize.z),
                                            box.halfSize.y * box.halfSize.z) < spawner.minAreaNonUniformScale)
                            {
                                box.halfSize = random.NextFloat3(0f, math.max(spawner.minMaxUniformScale.y, spawner.minAreaNonUniformScale));
                            }
                            scale.Value = box.halfSize * 2f;
                            collider    = box;
                        }
                        else
                        {
                            scale.Value = random.NextFloat(spawner.minMaxUniformScale.x, spawner.minMaxUniformScale.y);

                            collider = Physics.ScaleCollider(collider, new PhysicsScale(scale.Value));
                        }

                        icbp.Add(spawner.prefab, trans, rot, scale, trigger, collider, entityInQueryIndex);
                    }
                }
            }).ScheduleParallel();

            ecb.DestroyEntity(m_query);
        }
    }
}

