using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SpinAuthoring : MonoBehaviour
{
    public float degreesPerSecond = 120f;
}

public class SpinAuthoringBaker : Baker<SpinAuthoring>
{
    public override void Bake(SpinAuthoring authoring)
    {
        var entity                              = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Spinner { rads = math.radians(authoring.degreesPerSecond) });
    }
}

