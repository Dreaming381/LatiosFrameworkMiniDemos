using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons
{
    [DisallowMultipleComponent]
    public class PlayerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float moveSpeed = 5f;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new Player { moveSpeed = moveSpeed });
        }
    }
}

