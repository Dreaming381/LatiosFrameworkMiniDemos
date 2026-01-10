using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;


namespace Testing
{
    public struct Body
    {
        public float3 pos;
        public float3 vel;
        public float3 acc;
        public float mass;

        public Body(float3 pos, float3 vel, float mass)
        {
            this.pos = pos;
            this.vel = vel;
            this.acc = float3.zero;
            this.mass = mass;
        }
    };
}
