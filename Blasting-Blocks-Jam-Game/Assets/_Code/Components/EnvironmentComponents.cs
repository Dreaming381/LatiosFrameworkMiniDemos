using Latios;
using Latios.Psyshock;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

// Post-Jam Notes:
// These are the general environment and non-player gameplay components, excluding physics.

namespace BB
{
    public struct StaticEnvironmentTag : IComponentData { }

    public partial struct StaticEnvironmentCollisionLayer : ICollectionComponent
    {
        public CollisionLayer layer;

        public JobHandle TryDispose(JobHandle inputDeps) => layer.IsCreated ? layer.Dispose(inputDeps) : inputDeps;
    }

    public struct FiredBlock : IComponentData
    {
        public float4 dynamicColor;
        public float4 staticColor;
        public float  colorAnimationTime;
        public float  colorAnimationDuration;
        public float  lifetime;
        public float  maxTimeBeforeHardFreeze;

        public EntityWith<Prefab> freezeSound;
    }

    public struct GoToSceneOnPlayerEnter : IComponentData
    {
        public FixedString128Bytes scene;
    }
}

