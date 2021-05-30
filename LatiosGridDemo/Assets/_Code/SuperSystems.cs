using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public class GameplayRootSuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddSystem<PerFrameSuperSystem>();
            GetOrCreateAndAddSystem<PerTurnSuperSystem>();
        }
    }

    public class PerFrameSuperSystem : SuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddSystem<BuildGridSystem>();
            GetOrCreateAndAddSystem<PlayerInputSystem>();
        }
    }

    public class PerTurnSuperSystem : SuperSystem
    {
        public override bool ShouldUpdateSystem()
        {
            return worldBlackboardEntity.GetComponentData<ShouldExecuteTurn>().shouldExecuteTurn;
        }

        protected override void CreateSystems()
        {
            GetOrCreateAndAddSystem<AiFixedActionUpdateSystem>();

            GetOrCreateAndAddSystem<MoveSystem>();
            GetOrCreateAndAddSystem<AttackSystem>();

            GetOrCreateAndAddSystem<UpdateCharacterTranslationsSystem>();

            //GetOrCreateAndAddSystem<TestTurnSystem>();
        }
    }

    public class TestTurnSystem : SubSystem
    {
        int runCount = 0;

        protected override void OnUpdate()
        {
            UnityEngine.Debug.Log($"Test Turn: {runCount++}");
        }
    }
}

