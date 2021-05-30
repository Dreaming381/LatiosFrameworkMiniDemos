using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons
{
    [DisallowMultipleComponent]
    public class TriggerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float  radius = 2f;
        public string sceneToSwitchTo;
        public bool   isDeath = false;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new Trigger { radius = radius, sceneToSwitchTo = sceneToSwitchTo, isDeath = isDeath });
        }
    }
}

