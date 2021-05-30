using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons
{
    [DisallowMultipleComponent]
    public class CharacterAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new GridPosition
            {
                position = (int2)(((float3)transform.position).xz + 0.5f)
            });
            dstManager.AddComponent<CharacterActionDirection>(entity);
            dstManager.AddComponent<CharacterWantsToAttack>(  entity);
            dstManager.AddComponent<CharacterWantsToMove>(    entity);
            dstManager.AddComponent<Followed>(                entity);
        }
    }
}

