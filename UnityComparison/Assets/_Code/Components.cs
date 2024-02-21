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

struct MovingTriggerStats : IComponentData
{
    public float minSpeed;
    public float maxSpeed;
}

struct MovingTriggerState : IComponentData
{
    public float2 currentVelocity;
}

