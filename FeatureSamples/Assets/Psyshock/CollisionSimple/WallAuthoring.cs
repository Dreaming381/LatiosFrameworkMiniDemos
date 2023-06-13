using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.PsyshockSamples.CollisionSimple
{
    public class WallAuthoring : MonoBehaviour
    {
    }

    public class WallAuthoringBaker : Baker<WallAuthoring>
    {
        public override void Bake(WallAuthoring authoring)
        {
            AddComponent<WallTag>(GetEntity(TransformUsageFlags.Renderable));
        }
    }
}

