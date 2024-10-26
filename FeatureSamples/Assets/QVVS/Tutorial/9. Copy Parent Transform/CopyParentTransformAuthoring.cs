using Latios.Transforms.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.QvvsSamples.Tutorial.Authoring
{
    public class CopyParentTransformAuthoring : MonoBehaviour
    {
    }

    public class CopyParentTransformAuthoringBaker : Baker<CopyParentTransformAuthoring>
    {
        [BakingType] struct CopyParentRequestTag : IRequestCopyParentTransform { }

        public override void Bake(CopyParentTransformAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent<CopyParentRequestTag>(entity);
        }
    }
}

