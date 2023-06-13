using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Dragons.PsyshockSamples.CollisionSimple
{
    // We use fixed step to keep the simulation time step predictable to more consistently evaluate stability.
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class CollisionSimpleSuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<RandomizedSpawnerSystem>();
            GetOrCreateAndAddUnmanagedSystem<PlayerMoveSystem>();
            GetOrCreateAndAddUnmanagedSystem<BruteForceCollisionSystem>();
            GetOrCreateAndAddUnmanagedSystem<FindPairsCollisionSystem>();
        }

        public override bool ShouldUpdateSystem()
        {
            return worldBlackboardEntity.GetComponentData<CurrentScene>().current == "CollisionSimple";
        }
    }
}

