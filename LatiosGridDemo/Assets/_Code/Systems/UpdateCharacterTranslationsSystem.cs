using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class UpdateCharacterTranslationsSystem : SubSystem
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ref Translation trans, in GridPosition position) =>
            {
                trans.Value = new float3(position.position.x, trans.Value.y, position.position.y);
            }).ScheduleParallel();
        }
    }
}

