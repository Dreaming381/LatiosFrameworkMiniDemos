using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Dragons.Authoring
{
    [DisallowMultipleComponent]
    public class PhysicsSettingsAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float gravity = 9.81f;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new Gravity { gravity = gravity });
        }
    }
}

