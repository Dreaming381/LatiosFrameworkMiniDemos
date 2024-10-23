using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.QvvsSamples.Tutorial.Authoring
{
    public class ChildSpawnerAuthoring : MonoBehaviour
    {
        public GameObject spawnable;
        public float3     localOffset;
    }

    public class ChildSpawnerAuthoringBaker : Baker<ChildSpawnerAuthoring>
    {
        public override void Bake(ChildSpawnerAuthoring authoring)
        {
            if (authoring.spawnable == null)
                return;

            var entity = GetEntity(TransformUsageFlags.Renderable);

            AddComponent(entity, new ChildSpawner
            {
                spawnable   = GetEntity(authoring.spawnable, TransformUsageFlags.Renderable),
                localOffset = authoring.localOffset
            });
        }
    }
}

