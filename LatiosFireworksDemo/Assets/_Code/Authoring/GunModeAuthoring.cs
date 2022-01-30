using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.Authoring
{
    [DisallowMultipleComponent]
    public class GunModeAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public GunMode.Mode mode;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new GunMode { mode = mode });
        }
    }
}

