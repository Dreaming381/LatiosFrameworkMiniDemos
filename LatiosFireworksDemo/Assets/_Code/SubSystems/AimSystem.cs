using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class AimSystem : SubSystem
    {
        protected override void OnUpdate()
        {
            float dt = Time.DeltaTime;
            Entities.ForEach((ref Rotation rotation, ref AimAltitude altitude, in AimStats stats, in DesiredActions actions) =>
            {
                altitude.altitude -= actions.turn.y * dt;
                altitude.altitude  = math.clamp(altitude.altitude, stats.minAltitude, stats.maxAltitude);
                var yAxisRotation  = quaternion.Euler(0f, math.clamp(actions.turn.x, -stats.turnTopSpeed, stats.turnTopSpeed) * dt, 0f);
                rotation.Value     = math.mul(yAxisRotation, rotation.Value);
            }).ScheduleParallel();

            Entities.ForEach((in AimHeadReference head, in AimAltitude altitude) =>
            {
                var rotation   = GetComponent<Rotation>(head.head);
                rotation.Value = quaternion.Euler(altitude.altitude, 0f, 0f);
                SetComponent(head.head, rotation);
            }).Schedule();
        }
    }
}

