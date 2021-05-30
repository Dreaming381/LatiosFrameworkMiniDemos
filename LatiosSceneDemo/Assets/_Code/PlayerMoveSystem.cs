using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class PlayerMoveSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ref Translation translation, in Player player) =>
            {
                float2 move        = default;
                move.x             = UnityEngine.Input.GetAxis("Horizontal");
                move.y             = UnityEngine.Input.GetAxis("Vertical");
                move               = move * player.moveSpeed * Time.DeltaTime;
                translation.Value += new float3(move.x, 0f, move.y);
            }).WithoutBurst().Run();
        }
    }
}

