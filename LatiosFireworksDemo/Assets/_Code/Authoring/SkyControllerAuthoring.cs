using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.Authoring
{
    [DisallowMultipleComponent]
    public class SkyControllerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public Material daySky;
        public Material nightSky;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentObject(entity, new SkyController { day = daySky, night = nightSky});
        }
    }
}

