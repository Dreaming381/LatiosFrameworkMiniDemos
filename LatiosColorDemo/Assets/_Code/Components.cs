using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Dragons
{
    public struct TranslationalVelocity : IComponentData
    {
        public float3 velocity;
    }

    public struct AngularVelocity : IComponentData
    {
        public quaternion velocity;
    }

    [MaterialProperty("_Color", MaterialPropertyFormat.Float4)]
    public struct DynamicColor : IComponentData
    {
        public float4 color;
    }

    public struct WorldBounds : IComponentData
    {
        public float3 halfExtents;
    }

    public struct Spawner : IComponentData
    {
        public Entity prefab;
        public int    spawnCount;
        public float4 color;
        public float2 minMaxLinearVelocity;
        public float2 minMaxUniformScale;
        public float  minAreaNonUniformScale;
        public uint   seed;
    }

    public struct Trigger : IComponentData
    {
        public float3 colorDeltas;
    }

    public struct HitSound : IComponentData
    {
        public Entity prefab;
    }
}

