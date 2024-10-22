using Latios;
using Latios.Transforms;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Dragons.QvvsSamples.Tutorial
{
    struct ScaleAndStretchAnimation : IComponentData
    {
        public float rootInitialScale;
        public float rootFinalScale;
        public float rootScaleTimeStart;
        public float rootScaleTimeEnd;

        public float stemInitialScaleY;
        public float stemFinalScaleY;
        public float stemScaleTimeStart;
        public float stemScaleTimeEnd;

        public float topInitialScaleX;
        public float topFinalScaleX;
        public float topScaleTimeStart;
        public float topScaleTimeEnd;

        public EntityWith<WorldTransform> stemEntity;
        public EntityWith<WorldTransform> topEntity;
    }
}

