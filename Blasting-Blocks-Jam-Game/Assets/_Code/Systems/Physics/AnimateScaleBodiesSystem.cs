using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Post-Jam Notes:
// This is the job that animates the scale of the bodies.
// It continues to run on bodies that have already finished the scale animation
// to save development time.

namespace BB
{
    [BurstCompile]
    public partial struct AnimateScaleBodiesSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new Job { deltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            public float deltaTime;

            public void Execute(TransformAspect transform, ref AnimatedScale scale)
            {
                if (scale.currentTime > scale.scaleUpStartTime && scale.currentTime < scale.scaleUpEndTime)
                    transform.localScale = math.remap(scale.scaleUpStartTime, scale.scaleUpEndTime, scale.minScale, scale.targetScale, scale.currentTime);
                else if (scale.currentTime >= scale.scaleUpEndTime)
                    transform.localScale = scale.targetScale;
                else
                    transform.localScale  = scale.minScale;
                scale.currentTime        += deltaTime;
            }
        }
    }
}

