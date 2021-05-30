using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons
{
    [DisallowMultipleComponent]
    public class PlayerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<PlayerTag>(entity);
        }
    }
}

