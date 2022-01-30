using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class SkySystem : SubSystem
    {
        protected override void OnUpdate()
        {
            var  mode  = sceneBlackboardEntity.GetComponentData<GunMode>().mode;
            bool isDay = mode == GunMode.Mode.Timewarp;

            Entities.ForEach((UnityEngine.Light light, SkyController skyController) =>
            {
                if (!light.gameObject.activeSelf)
                    light.gameObject.SetActive(true);

                if (light.enabled != isDay)
                {
                    light.enabled = isDay;

                    if (isDay)
                    {
                        UnityEngine.RenderSettings.skybox = skyController.day;
                    }
                    else
                    {
                        UnityEngine.RenderSettings.skybox = skyController.night;
                    }
                }

                if (UnityEngine.RenderSettings.sun == null)
                    UnityEngine.RenderSettings.sun = light;
            }).WithoutBurst().Run();
        }
    }
}

