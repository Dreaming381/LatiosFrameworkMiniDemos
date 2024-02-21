using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;

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

        public void Execute(Entity entity, ref LocalTransform transform, ref MovingTriggerState state, in MovingTriggerStats stats)
        {
            if (state.currentVelocity.Equals(float2.zero))
            {
                var random            = Random.CreateFromIndex((uint)entity.Index);
                state.currentVelocity = random.NextFloat2Direction() * random.NextFloat(stats.minSpeed, stats.maxSpeed);
            }

            var position           = transform.Position.xz;
            position              += state.currentVelocity * dt;
            state.currentVelocity  = math.select(state.currentVelocity, math.abs(state.currentVelocity), position < -extent);
            state.currentVelocity  = math.select(state.currentVelocity, -math.abs(state.currentVelocity), position > extent);

            transform.Position = new float3(position.x, transform.Position.y, position.y);
        }
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
[BurstCompile]
public partial struct TriggerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new SetInitialColorJob().ScheduleParallel(state.Dependency);
        state.Dependency = new Processor
        {
            colorLookup = GetComponentLookup<URPMaterialPropertyBaseColor>(false)
        }.Schedule(GetSingleton<SimulationSingleton>(), state.Dependency);
    }

    [BurstCompile]
    partial struct SetInitialColorJob : IJobEntity
    {
        public void Execute(ref URPMaterialPropertyBaseColor color)
        {
            color.Value = new float4(0f, 1f, 0f, 1f);
        }
    }

    [BurstCompile]
    struct Processor : ITriggerEventsJob
    {
        public ComponentLookup<URPMaterialPropertyBaseColor> colorLookup;

        public void Execute(TriggerEvent triggerEvent)
        {
            var first = colorLookup.GetRefRWOptional(triggerEvent.EntityA);
            if (first.IsValid)
                first.ValueRW.Value = new float4( 1f, 0f, 0f, 1f );
            var second              = colorLookup.GetRefRWOptional(triggerEvent.EntityB);
            if (second.IsValid)
                second.ValueRW.Value = new float4(1f, 0f, 0f, 1f);
        }
    }
}

