using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Post-Jam Notes:
// The vertical mouse-look algorithm here is a little different, and was a technique I hadn't tried before the jam.
// The idea is that we start with the x and y values of the forward vector in local space that the input
// is asking us to face. We project that vector onto the yz plane and normalize it. And we compute a local-relative
// rotation which we multiply with the existing rotation. Then we check if the forward vector in world-space exceeds
// the view limits, and if so, we clamp and renormalize the vector and compute a new rotation.

using static Unity.Entities.SystemAPI;

namespace BB
{
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct FirstPersonAimVerticalSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new Job { desiredActionsLookup = GetComponentLookup<FirstPersonDesiredActions>(true) }.ScheduleParallel();
        }

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            [ReadOnly] public ComponentLookup<FirstPersonDesiredActions> desiredActionsLookup;

            public void Execute(TransformAspect transform, in FirstPersonVerticalAimStats stats)
            {
                ref readonly var actions = ref desiredActionsLookup.GetRefRO(stats.actionsEntity).ValueRO;

                var deltaY             = math.clamp(actions.lookDirectionFromForward.y, -0.9f, 0.9f);
                var deltaForward       = new float3(0f, deltaY, math.sqrt(1f - deltaY * deltaY));
                var deltaRotation      = quaternion.LookRotation(deltaForward, math.up());
                var newRotation        = math.mul(deltaRotation, transform.localRotation);
                var newForwardY        = math.forward(newRotation).y;
                var clampedNewForwardY = math.clamp(newForwardY, stats.minSinLimit, stats.maxSinLimit);
                if (newForwardY != clampedNewForwardY)
                {
                    // We hit the boundary and need to clamp the rotation.
                    var clampedForward = new float3(0f, clampedNewForwardY, math.sqrt(1f - clampedNewForwardY * clampedNewForwardY));
                    newRotation        = quaternion.LookRotation(clampedForward, math.up());
                }
                transform.localRotation = newRotation;
            }
        }
    }
}

