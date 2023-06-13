using Latios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.PsyshockSamples.CollisionSimple
{
    public class RandomizingSpawnerAuthoring : MonoBehaviour
    {
        public GameObject characterPhysicsPrefab;
        public int        count;
        public float      minDensity;
        public float      maxDensity;
        public float      minScale;
        public float      maxScale;
        public float2     minPosition;
        public float2     maxPosition;
        public float      minSpeed;
        public float      maxSpeed;
    }

    public class RandomizingSpawnerAuthoringBaker : Baker<RandomizingSpawnerAuthoring>
    {
        public override void Bake(RandomizingSpawnerAuthoring authoring)
        {
            AddComponent(GetEntity(TransformUsageFlags.None), new Spawner
            {
                characterPhysicsPrefab = GetEntity(authoring.characterPhysicsPrefab, TransformUsageFlags.Renderable),
                count                  = authoring.count,
                minDensity             = authoring.minDensity,
                maxDensity             = authoring.maxDensity,
                minScale               = authoring.minScale,
                maxScale               = authoring.maxScale,
                minPosition            = authoring.minPosition,
                maxPosition            = authoring.maxPosition,
                minSpeed               = authoring.minSpeed,
                maxSpeed               = authoring.maxSpeed,
            });
        }
    }
}

