using Latios.Transforms.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.QvvsSamples.Tutorial.Authoring
{
    public class PlatformAuthoring : MonoBehaviour
    {
        public float magnitude = 5f;
        public float period    = 6f;
    }

    public class PlatformAuthoringBaker : Baker<PlatformAuthoring>
    {
        [BakingType] struct PreviousTransformRequest : IRequestPreviousTransform { }

        public override void Bake(PlatformAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlatformMotion
            {
                magnitude = authoring.magnitude,
                frequency = math.TAU / authoring.period
            });
            AddComponent<PreviousTransformRequest>(entity);
        }
    }
}

