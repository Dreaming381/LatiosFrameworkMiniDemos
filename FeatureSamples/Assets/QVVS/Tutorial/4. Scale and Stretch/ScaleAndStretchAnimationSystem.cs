using Latios;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace Dragons.QvvsSamples.Tutorial.Systems
{
    [RequireMatchingQueriesForUpdate]
    [BurstCompile]
    public partial struct ScaleAndStretchAnimationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var elapsedTime = (float)Time.ElapsedTime;
            foreach ((var animation, var transform) in Query<ScaleAndStretchAnimation, TransformAspect>())
            {
                var loopTime = math.fmod(elapsedTime, animation.topScaleTimeEnd);

                float rootFactor = math.saturate(math.unlerp(animation.rootScaleTimeStart, animation.rootScaleTimeEnd, loopTime));
                float stemFactor = math.saturate(math.unlerp(animation.stemScaleTimeStart, animation.stemScaleTimeEnd, loopTime));
                float topFactor  = math.saturate(math.unlerp(animation.topScaleTimeStart, animation.topScaleTimeEnd, loopTime));

                if (loopTime < animation.rootScaleTimeStart && elapsedTime > animation.rootScaleTimeEnd)
                {
                    rootFactor = 1f;
                    stemFactor = 1f;
                    topFactor  = 1f;
                }

                var stemTransform = GetAspect<TransformAspect>(animation.stemEntity);
                var topTransform  = GetAspect<TransformAspect>(animation.topEntity);

                transform.localScale = math.lerp(animation.rootInitialScale, animation.rootFinalScale, rootFactor);

                var stemStretch       = stemTransform.stretch;
                stemStretch.y         = math.lerp(animation.stemInitialScaleY, animation.stemFinalScaleY, stemFactor);
                stemTransform.stretch = stemStretch;

                var topStretch       = topTransform.stretch;
                topStretch.x         = math.lerp(animation.topInitialScaleX, animation.topFinalScaleX, topFactor);
                topTransform.stretch = topStretch;
            }
        }
    }
}

