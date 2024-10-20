using Latios.Transforms;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace Dragons.QvvsSamples.Tutorial.Systems
{
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct PlayerSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float horizontal = UnityEngine.Input.GetAxis("Horizontal");
            float vertical   = UnityEngine.Input.GetAxis("Vertical");

            float2 input = new float2(horizontal, vertical);
            if (input.Equals(float2.zero))
                return;

            if (math.length(input) > 1f)
                input = math.normalize(input);

            foreach ((var transform, var player) in Query<TransformAspect, Player>())
            {
                transform.worldRotation = quaternion.LookRotationSafe(new float3(input.x, 0f, input.y), math.up());
                transform.TranslateWorld(new float3(input.x, 0f, input.y) * player.speed * Time.DeltaTime);
            }
        }
    }
}

