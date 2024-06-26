using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Post-Jam Notes:
// By putting the EnvironmentCollisionAuthoring on a root GameObject,
// all child collider entities receive the StaticEnvironmentTag.
// This saves a ton of clicks in the Editor. There's also the option
// to cut off a hierarchy by using the ExcludeRecursively mode.
// And then the chain can be started again on a further descendant.

namespace BB
{
    public class EnvironmentCollisionAuthoring : MonoBehaviour
    {
        public enum Mode
        {
            IncludeRecursively,
            ExcludeRecursively
        }

        public Mode mode;
    }

    [BakeDerivedTypes]
    public class EnvironmentCollisionAuthoringBaker : Baker<Collider>
    {
        static List<Collider> s_colliderCache = new List<Collider>();

        public override void Bake(Collider authoring)
        {
            s_colliderCache.Clear();
            GetComponents(s_colliderCache);
            if (s_colliderCache[0] != authoring)
                return;

            var ancencestor = GetComponentInParent<EnvironmentCollisionAuthoring>();
            if (ancencestor == null)
                return;
            if (ancencestor.mode == EnvironmentCollisionAuthoring.Mode.IncludeRecursively)
            {
                var entity = GetEntity(TransformUsageFlags.Renderable);
                AddComponent<StaticEnvironmentTag>(entity);
            }
        }
    }
}

