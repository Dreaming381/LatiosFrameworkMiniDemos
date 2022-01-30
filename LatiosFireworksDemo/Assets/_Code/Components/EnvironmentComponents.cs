using System;
using Latios;
using Latios.Psyshock;
using Unity.Entities;
using Unity.Jobs;

namespace Dragons
{
    public struct EnvironmentTag : IComponentData { }

    public struct EnvironmentCollisionLayerTag : IComponentData { }

    public struct EnvironmentCollisionLayer : ICollectionComponent
    {
        public CollisionLayer layer;

        public Type AssociatedComponentType => typeof(EnvironmentCollisionLayerTag);

        public JobHandle Dispose(JobHandle inputDeps) => layer.Dispose(inputDeps);
    }

    public struct GunMode : IComponentData
    {
        public enum Mode : byte
        {
            Timewarp,
            Fireworks
        }

        public Mode mode;
    }

    public class SkyController : IComponentData
    {
        public UnityEngine.Material day;
        public UnityEngine.Material night;
    }
}

