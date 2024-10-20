using Latios;
using Latios.Transforms;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Dragons.QvvsSamples.Tutorial
{
    struct PlatformMotion : IComponentData
    {
        public float magnitude;
        public float frequency;
    }

    struct PlatformTracker : IComponentData
    {
        public EntityWith<PreviousTransform> platform;
        public float                         extents;
    }
}

