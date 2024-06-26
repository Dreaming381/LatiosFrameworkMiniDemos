using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Post-Jam Notes:
// This was how we specified the next level or win screen when the player reached the finish token.

namespace BB
{
    public class GoToSceneOnPlayerEnterAuthoring : MonoBehaviour
    {
        public string sceneName;
    }

    public class GoToSceneOnPlayerEnterAuthoringBaker : Baker<GoToSceneOnPlayerEnterAuthoring>
    {
        public override void Bake(GoToSceneOnPlayerEnterAuthoring authoring)
        {
            var entity                                              = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new GoToSceneOnPlayerEnter { scene = authoring.sceneName });
        }
    }
}

