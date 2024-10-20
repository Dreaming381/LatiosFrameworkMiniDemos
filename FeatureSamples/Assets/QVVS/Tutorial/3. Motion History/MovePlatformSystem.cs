using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace Dragons.QvvsSamples.Tutorial.Systems
{
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct MovePlatformSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach ((var transform, var platform) in Query<TransformAspect, PlatformMotion>())
            {
                var currentPosition     = transform.worldPosition;
                currentPosition.x       = platform.magnitude * math.sin(platform.frequency * (float)Time.ElapsedTime);
                transform.worldPosition = currentPosition;
            }
        }
    }
}

