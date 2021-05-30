using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons
{
    [DisallowMultipleComponent]
    public class TriggerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<Trigger>(entity);
        }
    }
}

