using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class MoveCameraSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((CameraManager camera, ref Translation translation, ref Rotation rotation) =>
            {
                var x              = UnityEngine.Input.GetAxis("Horizontal");
                var z              = UnityEngine.Input.GetAxis("Vertical");
                var y              = UnityEngine.Input.GetAxis("Mouse ScrollWheel") * 10f;
                translation.Value += math.mul(rotation.Value, 10f * new float3(x, y, z));

                float2 mouseDelta  = new float2(UnityEngine.Input.GetAxis("Mouse X"), UnityEngine.Input.GetAxis("Mouse Y"));
                mouseDelta        *= 120f * camera.camera.aspect * math.radians(60f) / camera.camera.scaledPixelHeight;

                //Rotation
                var oldRotation = rotation.Value;
                var turn        = mouseDelta;
                turn.y          = -turn.y;
                float3 up       = math.mul(oldRotation, new float3(0f, 1f, 0f));
                turn.x          = math.select(turn.x, -turn.x, up.y < 0f);
                var xAxisRot    = quaternion.Euler(turn.y, 0f, 0f);
                var yAxisRot    = quaternion.Euler(0f, turn.x, 0f);
                var newRotation = math.mul(oldRotation, xAxisRot);
                newRotation     = math.mul(yAxisRot, newRotation);
                rotation.Value  = newRotation;
            }).WithoutBurst().Run();
        }
    }
}

