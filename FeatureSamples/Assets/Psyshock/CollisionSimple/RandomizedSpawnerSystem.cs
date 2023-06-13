using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;

using static Unity.Entities.SystemAPI;

namespace Dragons.PsyshockSamples.CollisionSimple
{
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct RandomizedSpawnerSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();

            state.EntityManager.AddComponentData(state.SystemHandle, new SystemRng("RandomizedSpawnerSystem"));
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new Job
            {
                icb =
                    latiosWorld.syncPoint.CreateInstantiateCommandBuffer<WorldTransform, CharacterPhysicsStats, CharacterPhysicsState,
                                                                         URPMaterialPropertyBaseColor>().AsParallelWriter(),
                rng = state.EntityManager.GetComponentDataRW<SystemRng>(state.SystemHandle).ValueRW.Shuffle()
            }.ScheduleParallel();

            var query = QueryBuilder().WithAll<Spawner>().Build();
            latiosWorld.syncPoint.CreateEntityCommandBuffer().DestroyEntity(query);
        }

        [BurstCompile]
        partial struct Job : IJobEntity, IJobEntityChunkBeginEnd
        {
            public InstantiateCommandBuffer<WorldTransform, CharacterPhysicsStats, CharacterPhysicsState, URPMaterialPropertyBaseColor>.ParallelWriter icb;
            public SystemRng                                                                                                                           rng;

            public void Execute([ChunkIndexInQuery] int chunkIndexInQuery, in Spawner spawner)
            {
                var maxMass = spawner.maxDensity * 4f / 3f * math.PI * spawner.maxScale * spawner.maxScale * spawner.maxScale / 8f;
                for (int i = 0; i < spawner.count; i++)
                {
                    var position = rng.NextFloat2(spawner.minPosition, spawner.maxPosition);
                    var scale    = rng.NextFloat(spawner.minScale, spawner.maxScale);
                    var mass     = rng.NextFloat(spawner.minDensity, spawner.maxDensity) * 4f / 3f * math.PI * scale * scale * scale / 8f;
                    var velocity = rng.NextFloat2Direction() * rng.NextFloat(spawner.minSpeed, spawner.maxSpeed);
                    var color    = 1f - mass / maxMass;

                    icb.Add(spawner.characterPhysicsPrefab,
                            new WorldTransform { worldTransform      = new TransformQvvs(new float3(position, 0f), quaternion.identity, scale, 1f) },
                            new CharacterPhysicsStats { mass         = mass },
                            new CharacterPhysicsState { velocity     = velocity },
                            new URPMaterialPropertyBaseColor { Value = new float4(color, 0f, 0f, 1f) },
                            chunkIndexInQuery);
                }
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                rng.BeginChunk(unfilteredChunkIndex);
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
            {
            }
        }
    }
}

