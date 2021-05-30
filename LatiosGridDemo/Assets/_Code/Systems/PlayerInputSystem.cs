using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class PlayerInputSystem : SubSystem
    {
        protected override void OnCreate()
        {
            worldBlackboardEntity.AddComponentIfMissing<ShouldExecuteTurn>();
        }

        protected override void OnUpdate()
        {
            UiBridge ui = null;

            Entities.ForEach((UiBridge uiBridge) =>
            {
                ui = uiBridge;
            }).WithoutBurst().Run();

            if (ui.receivedAction)
            {
                Entities.WithAll<PlayerTag>().ForEach((ref CharacterActionDirection direction, ref CharacterWantsToAttack attack, ref CharacterWantsToMove move) =>
                {
                    direction.direction  = ui.direction;
                    attack.wantsToAttack = ui.attack;
                    move.wantsToMove     = ui.move;
                }).WithoutBurst().Run();

                ui.direction      = 0;
                ui.attack         = false;
                ui.move           = false;
                ui.receivedAction = false;

                worldBlackboardEntity.SetComponentData(new ShouldExecuteTurn { shouldExecuteTurn = true });
            }
            else
            {
                worldBlackboardEntity.SetComponentData(new ShouldExecuteTurn { shouldExecuteTurn = false });
            }
        }
    }
}

