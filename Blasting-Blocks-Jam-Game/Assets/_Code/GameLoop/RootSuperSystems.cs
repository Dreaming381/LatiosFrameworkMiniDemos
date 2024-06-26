using Latios;
using Latios.Transforms;
using Latios.Transforms.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Post-Jam Notes:
// These are all the custom systems in the game. For jams I usually keep these all in one file,
// as there aren't that many of them. Everything that feeds into motion is in the first group,
// while other gameplay events are in the second.

namespace BB
{
    [UpdateBefore(typeof(TransformSuperSystem))]
    public partial class PreTransformsSuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<BuildStaticEnvironmentCollisionLayerSystem>();
            GetOrCreateAndAddManagedSystem<ReadPlayerInputSystem>();
            GetOrCreateAndAddUnmanagedSystem<AnimateScaleBodiesSystem>();
            GetOrCreateAndAddUnmanagedSystem<FirstPersonControllerSystem>();
            GetOrCreateAndAddUnmanagedSystem<FirstPersonAimVerticalSystem>();
            GetOrCreateAndAddUnmanagedSystem<BuildBodiesLayerSystem>();
            GetOrCreateAndAddUnmanagedSystem<LockRotationSystem>();
            GetOrCreateAndAddUnmanagedSystem<BodyVsBodySystem>();
            GetOrCreateAndAddUnmanagedSystem<BodyVsEnvironmentSystem>();
            GetOrCreateAndAddUnmanagedSystem<SolveBodiesSystem>();
        }
    }

    [UpdateAfter(typeof(TransformSuperSystem))]
    public partial class PostTransformSuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<ResetAndRestartSystem>();
            GetOrCreateAndAddUnmanagedSystem<SwitchSceneOnTriggerSystem>();
            GetOrCreateAndAddUnmanagedSystem<FireGunSystem>();
            GetOrCreateAndAddUnmanagedSystem<FreezeBlocksSystem>();
        }
    }
}

