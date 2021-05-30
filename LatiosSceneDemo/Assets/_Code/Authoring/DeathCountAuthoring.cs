using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons
{
    [DisallowMultipleComponent]
    public class DeathCountAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public int deathCount = 0;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new DeathCounter { deathCount = deathCount });
        }
    }
}

