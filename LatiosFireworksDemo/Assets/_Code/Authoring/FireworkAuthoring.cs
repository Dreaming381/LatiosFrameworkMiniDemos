using System.Collections.Generic;
using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.Authoring
{
    [DisallowMultipleComponent]
    public class FireworkAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        public float  radius;
        public float2 minMaxLifetime;
        public float  mass;

        public FireworkAuthoring fireworkToSpawnOnDeath;
        public float2            minMaxBlastSpeedOnDeath;
        public float             maxRandomAngleDeviationOnBlast;

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            if (fireworkToSpawnOnDeath != null)
                referencedPrefabs.Add(fireworkToSpawnOnDeath.gameObject);
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new TimeToLiveInitializer { minMaxTimeToLive = minMaxLifetime });
            dstManager.AddComponent<TimeToLive>(         entity);
            dstManager.AddComponent<PreviousTranslation>(entity);

            dstManager.AddComponentData(entity, new SphericalBallisticsStats
            {
                radius             = radius,
                inverseMass        = 1f / mass,
                timeWarpMultiplier = 1f
            });
            dstManager.AddComponent<SphericalBallisticsState>(      entity);
            dstManager.AddComponent<DieOnCollideWithEnvironmentTag>(entity);

            if (fireworkToSpawnOnDeath != null)
            {
                float childRadius = fireworkToSpawnOnDeath.radius;
                var   stars       = new NativeList<FireworksStarSpawner>(Allocator.TempJob);
                new RingPackingJob
                {
                    stars                    = stars,
                    containerRadius          = radius,
                    maxAngleDeviationDegrees = maxRandomAngleDeviationOnBlast,
                    minMaxExitSpeed          = minMaxBlastSpeedOnDeath,
                    starPrefab               = conversionSystem.GetPrimaryEntity(fireworkToSpawnOnDeath),
                    starRadius               = childRadius
                }.Run();
                var buffer = dstManager.AddBuffer<FireworksStarSpawner>(entity);
                buffer.CopyFrom(stars);
                stars.Dispose();
            }
        }

        [BurstCompile]
        struct RingPackingJob : IJob
        {
            public NativeList<FireworksStarSpawner> stars;
            public float2                           minMaxExitSpeed;
            public float                            maxAngleDeviationDegrees;
            public float                            containerRadius;
            public float                            starRadius;
            public Entity                           starPrefab;

            public void Execute()
            {
                // Generate the base star spawner struct for reuse
                var spawnerTemplate = new FireworksStarSpawner
                {
                    starToSpawn                      = starPrefab,
                    maxSinAngleExitVelocityDeviation = math.sin(math.radians(maxAngleDeviationDegrees)),
                    minMaxVelocityMultiplier         = new float2(minMaxExitSpeed.x / minMaxExitSpeed.y, 1f)
                };

                // Compute packing radius
                // We have two star spheres and one container sphere, whose centers make an isoscelese triangle.
                // We want the angle between the two stars seen from the container center.
                float  halfSinVertical    = starRadius / (containerRadius - starRadius);
                float2 verticalTurnVector = default;
                math.sincos(2f * math.asin(halfSinVertical), out verticalTurnVector.y, out verticalTurnVector.x);

                // Place first star at bottom
                spawnerTemplate.exitVelocity = new float3(0f, -minMaxExitSpeed.y, 0f);
                stars.Add(spawnerTemplate);

                for (float2 centerToRing = verticalTurnVector;
                     centerToRing.x * (containerRadius - starRadius) >= starRadius;
                     centerToRing = LatiosMath.ComplexMul(centerToRing, verticalTurnVector))
                {
                    float  ringY          = centerToRing.x * containerRadius;
                    float  ringRadius     = centerToRing.y * containerRadius;
                    float  halfSinRing    = starRadius / (ringRadius - starRadius);
                    float  minAngle       = 2f * math.asin(halfSinRing);
                    float  steps          = math.floor(math.PI * 2f / minAngle);
                    float  stepAngle      = math.PI * 2f / steps;
                    float2 ringTurnVector = default;
                    math.sincos(stepAngle, out ringTurnVector.y, out ringTurnVector.x);
                    float2 currentPointOnScaledRing = new float2(ringRadius, 0f);

                    for (float i = 0; i < steps; i += 1f)
                    {
                        float3 velocity              = new float3(currentPointOnScaledRing.x, ringY, currentPointOnScaledRing.y);
                        spawnerTemplate.exitVelocity = math.normalize(velocity) * minMaxExitSpeed.y;
                        stars.Add(spawnerTemplate);
                        currentPointOnScaledRing = LatiosMath.ComplexMul(currentPointOnScaledRing, ringTurnVector);
                    }
                }

                // We filled up the first half, now mirror with the second
                int count = stars.Length;
                for (int i = 0; i < count; i++)
                {
                    var star             = stars[i];
                    star.exitVelocity.y *= -1f;
                    stars.Add(star);
                }
            }
        }
    }
}

