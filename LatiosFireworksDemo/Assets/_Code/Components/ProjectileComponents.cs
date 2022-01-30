using System;
using Latios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Dragons
{
    // Expected on sceneBlackboardEntity
    public struct Gravity : IComponentData
    {
        public float gravity;  // Keep positive
    }

    public struct Damage : IComponentData
    {
        public int damage;
    }

    public struct Firer : IComponentData
    {
        public Entity firerEntity;
    }

    public struct SphericalBallisticsStats : IComponentData
    {
        public float radius;
        public float inverseMass;
        public float timeWarpMultiplier;
    }

    public struct SphericalBallisticsState : IComponentData
    {
        public float3 unwarpedVelocity;
    }

    public struct DieOnCollideWithEnvironmentTag : IComponentData { }

    public struct FireworksStarSpawner : IBufferElementData
    {
        public EntityWith<Prefab> starToSpawn;
        public float3             exitVelocity;
        public float              maxSinAngleExitVelocityDeviation;
        public float2             minMaxVelocityMultiplier;
    }
}

