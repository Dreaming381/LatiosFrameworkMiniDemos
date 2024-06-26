using Latios;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

// Post-Jam Notes:
// These are the non-controller player components. Technically the character controller can be driven by an AI.
// But these were exclusively for the player.

namespace BB
{
    public struct PlayerTag : IComponentData { }

    public struct GunFirer : IComponentData
    {
        public EntityWith<FirstPersonDesiredActions> actionsEntity;
        public EntityWith<Prefab>                    rigidBodyPrefab;
        public float                                 minScale;
        public float                                 scaleUpStartTime;
        public float                                 scaleUpEndTime;
        public float                                 exitVelocity;
    }
}

