using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class AiFixedActionUpdateSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithChangeFilter<AiFixedAction>().ForEach((ref CharacterActionDirection direction, ref CharacterWantsToAttack attack, ref CharacterWantsToMove move,
                                                                in AiFixedAction fixedAction) =>
            {
                direction.direction  = fixedAction.direction;
                attack.wantsToAttack = fixedAction.attack;
                move.wantsToMove     = fixedAction.move;
            }).Schedule();
        }
    }
}

