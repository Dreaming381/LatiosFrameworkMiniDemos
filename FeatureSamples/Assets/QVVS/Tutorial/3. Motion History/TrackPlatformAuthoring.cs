using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.QvvsSamples.Tutorial.Authoring
{
    public class TrackPlatformAuthoring : MonoBehaviour
    {
        public PlatformAuthoring platform;
        public float             extents = 5f;
    }

    public class TrackPlatformAuthoringBaker : Baker<TrackPlatformAuthoring>
    {
        public override void Bake(TrackPlatformAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlatformTracker
            {
                platform = GetEntity(authoring.platform, TransformUsageFlags.Renderable),
                extents  = authoring.extents
            });
        }
    }
}

