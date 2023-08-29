using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.QvvsSamples.Tutorial.Authoring
{
    public class SpinnerAuthoring : MonoBehaviour
    {
        public float3 axis             = new float3(0f, 1f, 0f);
        public float  degreesPerSecond = 135f;
    }

    public class SpinnerAuthoringBaker : Baker<SpinnerAuthoring>
    {
        public override void Bake(SpinnerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Spinner
            {
                axis             = authoring.axis,
                radiansPerSecond = math.radians(authoring.degreesPerSecond),
            });
        }
    }
}

