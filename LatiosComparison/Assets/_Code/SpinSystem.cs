using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

[UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]  // Ensure this happens before any transform system.
[BurstCompile]
public partial struct SpinSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        new Job { dt = Time.DeltaTime }.ScheduleParallel();
    }

    [BurstCompile]
    partial struct Job : IJobEntity
    {
        public float dt;

        public void Execute(TransformAspect transform, in Spinner spinner)
        {
            transform.RotateWorld(quaternion.EulerXYZ(0f, spinner.rads * dt, 0f));
        }
    }
}

