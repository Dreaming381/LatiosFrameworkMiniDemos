using System;
using Latios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Dragons
{
    public struct PlayerInputStats : IComponentData
    {
        // These are biases relative to each other. Everything can be scaled in the simulation.
        public float2 mouseDeltaFactors;
    }

    public struct PlayerTag : IComponentData { }
}

