using Latios;
using Latios.Systems;
using Unity.Entities;
using Unity.Transforms;

namespace Dragons
{
    [UpdateInGroup(typeof(LatiosWorldSyncGroup))]
    public class InitializationPostCommandBuffersRootSuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddSystem<InitializeTimeToLiveSystem>();
            GetOrCreateAndAddSystem<SkySystem>();
        }
    }

    [UpdateBefore(typeof(TransformSystemGroup))]
    public class PreTransformSimulationRootSuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddSystem<BuildEnvironmentCollisionLayerSystem>();
            GetOrCreateAndAddSystem<UpdatePreviousPositionsSystem>();

            // directly manipulates transforms (excluding via command buffer)
            GetOrCreateAndAddSystem<PlayerGameplayReadInputSystem>();
            GetOrCreateAndAddSystem<AimSystem>();
            GetOrCreateAndAddSystem<LocomotionSystem>();
            GetOrCreateAndAddSystem<MoveBallisticsSystem>();
        }
    }

    [UpdateAfter(typeof(TransformSystemGroup))]
    public class PostTransformSimulationRootSuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            // state management

            GetOrCreateAndAddSystem<KillOnCollideWithEnvironmentSystem>();
            GetOrCreateAndAddSystem<GunModeTriggerSwitchSystem>();
            GetOrCreateAndAddSystem<GunCooldownSystem>();
            GetOrCreateAndAddSystem<GunActionSystem>();
            GetOrCreateAndAddSystem<SpawnFireworksStarsOnDeathSystem>();
            GetOrCreateAndAddSystem<TimeToLiveUpdateAndKillSystem>();

            GetOrCreateAndAddSystem<CameraManagerCameraSyncHackSystem>();
        }
    }
}

