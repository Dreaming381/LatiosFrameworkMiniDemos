using Latios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Dragons.QvvsSamples.Tutorial
{
    struct ChildSpawner : IComponentData
    {
        public EntityWith<Prefab> spawnable;
        public float3             localOffset;
    }
}

