using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public class SpawnFireworksStarsOnDeathSystem : SubSystem
    {
        Rng m_rng;

        public override void OnNewScene() => m_rng = new Rng("SpawnFireworksStarsOnDeathSystem");

        protected override void OnUpdate()
        {
            var icb = latiosWorld.syncPoint.CreateInstantiateCommandBuffer<Translation, SphericalBallisticsState>().AsParallelWriter();
            var rng = m_rng.Shuffle();

            Entities.ForEach((int entityInQueryIndex, in DynamicBuffer<FireworksStarSpawner> starsToSpawn, in Translation translation, in TimeToLive timeToLive) =>
            {
                if (timeToLive.timeToLive <= 0f)
                {
                    var random = rng.GetSequence(entityInQueryIndex);
                    for (int i = 0; i < starsToSpawn.Length; i++)
                    {
                        var    star                    = starsToSpawn[i];
                        float  sinDeviation            = random.NextFloat(0f, star.maxSinAngleExitVelocityDeviation);
                        float2 deviationVector         = random.NextFloat2Direction() * sinDeviation;
                        float3 deviationForwardVector  = new float3(deviationVector.x, math.sqrt(1f - sinDeviation * sinDeviation), deviationVector.y);
                        float3 rotatedVelocity         = math.rotate(quaternion.LookRotation(deviationForwardVector, math.up()), star.exitVelocity);
                        rotatedVelocity               *= random.NextFloat(star.minMaxVelocityMultiplier.x, star.minMaxVelocityMultiplier.y);
                        var state                      = new SphericalBallisticsState { unwarpedVelocity = rotatedVelocity };
                        icb.Add(star.starToSpawn, translation, state, entityInQueryIndex);
                    }
                }
            }).ScheduleParallel();
        }
    }
}

