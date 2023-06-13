using System.Collections;
using System.Collections.Generic;
using Latios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Dragons.PsyshockSamples.CollisionSimple
{
    struct Settings : IComponentData
    {
        public float elasticity;
        public int   substeps;
        public int   iterations;
        public bool  useFindPairs;
    }

    struct WallTag : IComponentData { }

    struct CharacterPhysicsStats : IComponentData
    {
        public float mass;
    }

    struct CharacterPhysicsState : IComponentData
    {
        public float2 velocity;
    }

    struct PlayerStats : IComponentData
    {
        public float acceleration;
        public float deceleration;
        public float maxSpeed;
    }

    struct Spawner : IComponentData
    {
        public EntityWith<Prefab> characterPhysicsPrefab;
        public int                count;
        public float              minDensity;
        public float              maxDensity;
        public float              minScale;
        public float              maxScale;
        public float2             minPosition;
        public float2             maxPosition;
        public float              minSpeed;
        public float              maxSpeed;
    }
}

