using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class TriggerAuthoring : MonoBehaviour
{
    public bool  isMoving;
    public float minSpeed = 5f;
    public float maxSpeed = 25f;
}

public class TriggerAuthoringBaker : Baker<TriggerAuthoring>
{
    public override void Bake(TriggerAuthoring authoring)
    {
        if (authoring.isMoving)
        {
            var entity                                                                 = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(                    entity, new MovingTriggerStats { minSpeed = authoring.minSpeed, maxSpeed = authoring.maxSpeed });
            AddComponent<MovingTriggerState>(entity);
        }
        else
        {
            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent<URPMaterialPropertyBaseColor>(entity);
        }
    }
}

