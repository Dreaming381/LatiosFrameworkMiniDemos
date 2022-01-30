using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.Authoring
{
    public class PlayerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [Header("Mouse and Keyboard")]
        public float2 mouseDeltaFactors = 1f;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new PlayerInputStats
            {
                mouseDeltaFactors = mouseDeltaFactors,
            });

            dstManager.AddComponent<PlayerTag>(entity);
        }
    }
}

