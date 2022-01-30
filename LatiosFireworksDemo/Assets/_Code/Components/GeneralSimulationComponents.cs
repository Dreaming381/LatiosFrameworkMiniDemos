using System;
using Latios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Dragons
{
    public struct TimeToLive : IComponentData
    {
        public float timeToLive;
    }

    public struct TimeToLiveInitializer : IComponentData
    {
        public float2 minMaxTimeToLive;
    }

    public struct PreviousTranslation : IComponentData
    {
        public float3 previousTranslation;
    }
}

