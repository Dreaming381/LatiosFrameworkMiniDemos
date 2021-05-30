using Latios;
using Latios.Psyshock;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class SimulateSystem : SubSystem
    {
        protected override void OnUpdate()
        {
            float dt = Time.DeltaTime;

            var halfExtents = sceneBlackboardEntity.GetComponentData<WorldBounds>().halfExtents;

            Entities.ForEach((ref Translation trans, ref Rotation rot, ref TranslationalVelocity linear, in AngularVelocity angular) =>
            {
                trans.Value           += linear.velocity * dt;
                bool3 needsNegation    = trans.Value >= halfExtents;
                trans.Value            = math.select(trans.Value, halfExtents - (trans.Value - halfExtents), needsNegation);
                linear.velocity        = math.select(linear.velocity, -math.abs(linear.velocity), needsNegation);
                bool3 needsAbsolution  = trans.Value <= -halfExtents;
                trans.Value            = math.select(trans.Value, -2 * halfExtents - trans.Value, needsAbsolution);
                linear.velocity        = math.select(linear.velocity, math.abs(linear.velocity), needsAbsolution);

                rot.Value = math.mul(math.slerp(quaternion.identity, angular.velocity, math.saturate(dt)), rot.Value);
            }).ScheduleParallel();
        }
    }

    public class DebugDrawCollidersSystem : SubSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((in Collider collider, in Translation trans, in Rotation rot) =>
            {
                var aabb = Physics.CalculateAabb(collider, new RigidTransform { pos = trans.Value, rot = rot.Value });
                PhysicsDebug.DrawAabb(aabb, UnityEngine.Color.cyan);
            }).ScheduleParallel();
        }
    }
}

