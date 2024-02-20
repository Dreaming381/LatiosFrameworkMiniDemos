using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SpawnerAuthoring : MonoBehaviour
{
    public GameObject prefab;
    public float      spacing = 2f;
    public int        count   = 1;
}

public class SpawnerAuthoringBaker : Baker<SpawnerAuthoring>
{
    public override void Bake(SpawnerAuthoring authoring)
    {
        var entity                                = GetEntity(TransformUsageFlags.None);
        AddComponent(entity, new Spawner { prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic), spacing = authoring.spacing, count = authoring.count });
    }
}

