using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Dragons
{
    public struct Player : IComponentData
    {
        public float moveSpeed;
    }

    public struct Trigger : IComponentData
    {
        public float          radius;
        public FixedString128 sceneToSwitchTo;
        public bool           isDeath;
    }

    public struct DeathCounter : IComponentData
    {
        public int deathCount;
    }
}

