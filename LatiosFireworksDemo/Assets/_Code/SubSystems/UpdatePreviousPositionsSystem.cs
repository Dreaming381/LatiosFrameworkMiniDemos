using Latios;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class UpdatePreviousPositionsSystem : SubSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ref PreviousTranslation previous, in Translation current) =>
            {
                previous.previousTranslation = current.Value;
            }).ScheduleParallel();
        }
    }
}

