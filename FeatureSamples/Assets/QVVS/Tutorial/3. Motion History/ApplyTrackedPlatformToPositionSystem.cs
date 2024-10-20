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
    public partial struct ApplyTrackedPlatformToPositionSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach ((var transform, var platformTracker) in Query<TransformAspect, PlatformTracker>())
            {
                var newPosition = GetComponent<WorldTransform>(platformTracker.platform).position;
                var oldPosition = GetComponent<PreviousTransform>(platformTracker.platform).position;
                transform.TranslateWorld(newPosition - oldPosition);
                transform.worldPosition = math.clamp(transform.worldPosition, newPosition - platformTracker.extents, newPosition + platformTracker.extents);
            }
        }
    }
}

