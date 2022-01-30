using Latios;
using Latios.Psyshock;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class MoveBallisticsSystem : SubSystem
    {
        protected override void OnUpdate()
        {
            float  dt  = Time.DeltaTime;
            Entity sbe = sceneBlackboardEntity;
            Entities.ForEach((ref Translation trans, ref SphericalBallisticsState state, in SphericalBallisticsStats stats) =>
            {
                var gravity = GetComponent<Gravity>(sbe).gravity;

                float3 dragForce     = Physics.DragFrom(state.unwarpedVelocity, stats.radius, float3.zero, Physics.Constants.fluidViscosityOfAir, Physics.Constants.densityOfAir);
                float3 bouyancyForce = Physics.BouyancyFrom(new float3(0f, -gravity, 0f), stats.radius, Physics.Constants.densityOfAir);

                float3 acceleration     = stats.inverseMass * (dragForce + bouyancyForce);
                acceleration.y         -= gravity;
                state.unwarpedVelocity += acceleration * dt * stats.timeWarpMultiplier;
                trans.Value            += state.unwarpedVelocity * dt * stats.timeWarpMultiplier;
            }).ScheduleParallel();
        }
    }
}

