using Latios;
using Unity.Entities;
using Unity.Jobs;

namespace Dragons
{
    public class TimeToLiveUpdateAndKillSystem : SubSystem
    {
        protected override void OnUpdate()
        {
            var   dcb = latiosWorld.syncPoint.CreateDestroyCommandBuffer().AsParallelWriter();
            float dt  = Time.DeltaTime;

            Entities.ForEach((Entity entity, int entityInQueryIndex, ref TimeToLive timeToLive) =>
            {
                if (timeToLive.timeToLive <= 0f)
                    dcb.Add(entity, entityInQueryIndex);

                timeToLive.timeToLive -= dt;
            }).ScheduleParallel();
        }
    }
}

