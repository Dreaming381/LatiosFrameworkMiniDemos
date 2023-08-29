using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Dragons.QvvsSamples.Tutorial
{
    struct Spinner : IComponentData
    {
        public float3 axis;
        public float  radiansPerSecond;
    }
}

