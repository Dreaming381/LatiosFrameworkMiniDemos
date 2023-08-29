using Latios.Transforms;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace Dragons.QvvsSamples.Tutorial.Systems
{
    [BurstCompile]
    public partial struct SpinnerSystem : ISystem
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
                var spinRotation = quaternion.AxisAngle(spinner.axis, spinner.radiansPerSecond * dt);
                transform.RotateWorld(spinRotation);
            }
        }
    }
}

