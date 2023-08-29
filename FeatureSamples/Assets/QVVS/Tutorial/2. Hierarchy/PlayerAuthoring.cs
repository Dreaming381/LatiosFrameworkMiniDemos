using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.QvvsSamples.Tutorial.Authoring
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public float speed = 3f;
    }

    public class PlayerAuthoringBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var entity                              = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Player { speed = authoring.speed });
        }
    }
}

