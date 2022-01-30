using Latios;
using Unity.Entities;
using Unity.Mathematics;

namespace Dragons
{
    public class GunCooldownSystem : SubSystem
    {
        protected override void OnUpdate()
        {
            float dt = Time.DeltaTime;

            Entities.ForEach((ref GunState state, in GunStats gunStats) => {
                if (state.reloading)
                {
                    state.timeToReload -= dt;
                    if (state.timeToReload <= 0f)
                    {
                        state.reloading     = false;
                        state.magazineCount = gunStats.magazineCapacity;
                    }
                }
                else if (state.shooting)
                {
                    state.shotCooldown -= dt;
                    if (state.shotCooldown <= 0f)
                    {
                        state.shooting = false;
                    }
                }
            }).ScheduleParallel();
        }
    }
}

