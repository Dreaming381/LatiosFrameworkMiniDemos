using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons
{
    [DisallowMultipleComponent]
    public class WorldBoundsAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float3 halfExtents = 100f;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new WorldBounds { halfExtents = halfExtents });
        }
    }
}

