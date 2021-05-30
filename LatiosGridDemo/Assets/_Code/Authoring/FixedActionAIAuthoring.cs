using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons
{
    [DisallowMultipleComponent]
    public class FixedActionAIAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public enum ActionType
        {
            Move,
            Attack
        }

        public ActionType actionType;

        public int2 direction;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new AiFixedAction
            {
                move      = actionType == ActionType.Move,
                attack    = actionType == ActionType.Attack,
                direction = math.clamp(direction, -1, 1)
            });
        }
    }
}

