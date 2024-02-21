using Latios;
using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;

using static Unity.Entities.SystemAPI;

[UpdateBefore(typeof(SpawnSystem))]
[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
[BurstCompile]
public partial struct MovingTriggerSystem : ISystem
{
    float extent;

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var spawner in Query<Spawner>())
        {
            extent = math.max(extent, spawner.spacing * math.ceil(math.sqrt(spawner.count)));
        }

        new Job { extent = extent, dt = Time.DeltaTime }.ScheduleParallel();
    }

    [BurstCompile]
    partial struct Job : IJobEntity
    {
        public float extent;
        public float dt;

        public void Execute(Entity entity, TransformAspect transform, ref MovingTriggerState state, in MovingTriggerStats stats)
        {
            if (state.currentVelocity.Equals(float2.zero))
            {
                var random            = Random.CreateFromIndex((uint)entity.Index);
                state.currentVelocity = random.NextFloat2Direction() * random.NextFloat(stats.minSpeed, stats.maxSpeed);
            }

            var position           = transform.worldPosition.xz;
            position              += state.currentVelocity * dt;
            state.currentVelocity  = math.select(state.currentVelocity, math.abs(state.currentVelocity), position < -extent);
            state.currentVelocity  = math.select(state.currentVelocity, -math.abs(state.currentVelocity), position > extent);

            transform.worldPosition = new float3(position.x, transform.worldPosition.y, position.y);
        }
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
[BurstCompile]
public partial struct TriggerSystem : ISystem
{
    EntityQuery m_staticQuery;
    EntityQuery m_movingQuery;

    BuildCollisionLayerTypeHandles m_handles;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        m_staticQuery = state.Fluent().With<URPMaterialPropertyBaseColor>(false).PatchQueryForBuildingCollisionLayer().Build();
        m_movingQuery = state.Fluent().With<MovingTriggerState>(true).PatchQueryForBuildingCollisionLayer().Build();
        m_handles     = new BuildCollisionLayerTypeHandles(ref state);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_handles.Update(ref state);

        var staticCount = m_staticQuery.CalculateEntityCount();
        var extents     = 1.5f * math.ceil(math.sqrt(staticCount));
        var settings    = new CollisionLayerSettings
        {
            worldSubdivisionsPerAxis = new int3(3, 1, 3),
            worldAabb                = new Aabb(-extents, extents)
        };

        var jhA = Physics.BuildCollisionLayer(m_staticQuery, m_handles)  /*.WithSettings(settings)*/.ScheduleParallel(out var staticLayer,
                                                                                                                      state.WorldUpdateAllocator,
                                                                                                                      state.Dependency);
        var jhB = Physics.BuildCollisionLayer(m_movingQuery, m_handles)  /*.WithSettings(settings)*/.ScheduleParallel(out var movingLayer,
                                                                                                                      state.WorldUpdateAllocator,
                                                                                                                      state.Dependency);

        var jhC = new SetInitialColorJob().ScheduleParallel(state.Dependency);

        var processor = new Processor { colorLookup = GetComponentLookup<URPMaterialPropertyBaseColor>(false) };
        state.Dependency                            = Physics.FindPairs(staticLayer, movingLayer, processor).ScheduleParallel(JobHandle.CombineDependencies(jhA, jhB, jhC));
    }

    [BurstCompile]
    partial struct SetInitialColorJob : IJobEntity
    {
        public void Execute(ref URPMaterialPropertyBaseColor color)
        {
            color.Value = new float4(0f, 1f, 0f, 1f);
        }
    }

    // Assume A is static and B is dynamic
    struct Processor : IFindPairsProcessor
    {
        public PhysicsComponentLookup<URPMaterialPropertyBaseColor> colorLookup;

        public void Execute(in FindPairsResult result)
        {
            if (Physics.DistanceBetween(result.colliderA, result.transformA, result.colliderB, result.transformB, 0f, out _))
            {
                colorLookup[result.entityA] = new URPMaterialPropertyBaseColor { Value = new float4(1f, 0f, 0f, 1f) };
            }
        }
    }
}

