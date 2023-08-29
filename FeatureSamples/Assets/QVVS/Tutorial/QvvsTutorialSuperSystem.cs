using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Dragons.QvvsSamples.Tutorial.Systems
{
    [UpdateBefore(typeof(Latios.Transforms.Systems.TransformSuperSystem))]
    public partial class QvvsTutorialSuperSystem : RootSuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<SpinnerSystem>();
            GetOrCreateAndAddUnmanagedSystem<PlayerSystem>();
        }
    }
}

