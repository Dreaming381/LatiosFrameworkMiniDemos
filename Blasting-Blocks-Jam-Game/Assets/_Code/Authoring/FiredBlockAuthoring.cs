using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

// Post-Jam Notes:
// This does the baking for the blocks changing colors and playing sounds upon freezing.

namespace BB
{
    public class FiredBlockAuthoring : MonoBehaviour
    {
        public Color dynamicColor       = Color.red;
        public Color staticColor        = Color.blue;
        public float colorAnimationTime = 0.3f;
        public float maxFreezeTime      = 10f;

        public GameObject freezeSoundPrefab;
    }

    public class FiredBlockAuthoringBaker : Baker<FiredBlockAuthoring>
    {
        public override void Bake(FiredBlockAuthoring authoring)
        {
            var entity                                                    = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new URPMaterialPropertyBaseColor { Value = (Vector4)authoring.dynamicColor });
            AddComponent(entity, new FiredBlock
            {
                colorAnimationDuration  = authoring.colorAnimationTime,
                colorAnimationTime      = 0f,
                dynamicColor            = (Vector4)authoring.dynamicColor,
                staticColor             = (Vector4)authoring.staticColor,
                lifetime                = 0f,
                maxTimeBeforeHardFreeze = authoring.maxFreezeTime,
                freezeSound             = GetEntity(authoring.freezeSoundPrefab, TransformUsageFlags.None),
            });
        }
    }
}

