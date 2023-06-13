using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.PsyshockSamples.CollisionSimple
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public float acceleration;
        public float deceleration;
        public float maxSpeed;
    }

    public class PlayerAuthoringBaker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            AddComponent(GetEntity(TransformUsageFlags.Dynamic), new PlayerStats
            {
                acceleration = authoring.acceleration,
                deceleration = authoring.deceleration,
                maxSpeed     = authoring.maxSpeed,
            });
        }
    }
}

