using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.QvvsSamples.Tutorial.Authoring
{
    public class ScaleAndStretchAnimationAuthoring : MonoBehaviour
    {
        public float rootInitialScale  = 0.1f;
        public float rootScaleDuration = 0.75f;
        public float rootScaleDelay    = 0.75f;

        public float stemInitialStretchY = 0.5f;
        public float stemFinalStretchY   = 5f;
        public float stemScaleDuration   = 0.75f;
        public float stemScaleDelay      = 0.75f;

        public float topInitialStetchX = 1f;
        public float topFinalStretchX  = 7f;
        public float topScaleDuration  = 0.75f;
        public float topScaleDelay     = 0.75f;

        public GameObject stem;
        public GameObject top;
    }

    public class ScaleAndStretchAnimationAuthoringBaker : Baker<ScaleAndStretchAnimationAuthoring>
    {
        public override void Bake(ScaleAndStretchAnimationAuthoring authoring)
        {
            if (authoring.stem == null || authoring.top == null)
                return;

            var rootTimeStart = authoring.rootScaleDelay;
            var rootTimeEnd   = rootTimeStart + authoring.rootScaleDuration;
            var stemTimeStart = rootTimeEnd + authoring.stemScaleDelay;
            var stemTimeEnd   = stemTimeStart + authoring.stemScaleDuration;
            var topTimeStart  = stemTimeEnd + authoring.topScaleDelay;
            var topTimeEnd    = topTimeStart + authoring.topScaleDuration;

            var rootTransform = GetComponent<Transform>();

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ScaleAndStretchAnimation
            {
                rootInitialScale   = authoring.rootInitialScale,
                rootFinalScale     = rootTransform.localScale.x,
                rootScaleTimeStart = rootTimeStart,
                rootScaleTimeEnd   = rootTimeEnd,
                stemEntity         = GetEntity(authoring.stem, TransformUsageFlags.Dynamic),
                stemInitialScaleY  = authoring.stemInitialStretchY,
                stemFinalScaleY    = authoring.stemFinalStretchY,
                stemScaleTimeStart = stemTimeStart,
                stemScaleTimeEnd   = stemTimeEnd,
                topEntity          = GetEntity(authoring.top, TransformUsageFlags.Dynamic),
                topInitialScaleX   = authoring.topInitialStetchX,
                topFinalScaleX     = authoring.topFinalStretchX,
                topScaleTimeStart  = topTimeStart,
                topScaleTimeEnd    = topTimeEnd,
            });
        }
    }
}

