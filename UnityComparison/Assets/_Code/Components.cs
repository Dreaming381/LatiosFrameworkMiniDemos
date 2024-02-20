using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

struct Spawner : IComponentData
{
    public Entity prefab;
    public float  spacing;
    public int    count;
}

struct Spinner : IComponentData
{
    public float rads;
}

