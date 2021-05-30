using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class PropagateColorSystem : SubSystem
    {
        protected override void OnUpdate()
        {
            // This implementation could cause race conditions where in a hierarchy depth greater than 2,
            // a child may either get the current frame's color or a previous frame's color.
            // The sometimes laggy propagation doesn't defeat the demo's purpose nor compromise stability,
            // so the race condition is left as is.
            var colorCdfe = GetComponentDataFromEntity<DynamicColor>();
            Entities.ForEach((ref DynamicColor color, in Parent parent) =>
            {
                color = colorCdfe[parent.Value];
            }).WithNativeDisableContainerSafetyRestriction(colorCdfe).ScheduleParallel();
        }
    }
}

