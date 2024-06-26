using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Post-Jam Notes:
// This is the authoring for the very tip of the gun where blocks are spawned from.
// This spawner is responsible for both the launch speed and the scale-up animation,
// since those two properties usually need to be edited together.

namespace BB
{
    public class FirePointAuthoring : MonoBehaviour
    {
        public RigidBodyAuthoring prefab;
        public float              initialScale     = 0.01f;
        public float              scaleUpStartTime = 0.1f;
        public float              scaleUpEndTime   = 0.5f;
        public float              exitVelocity     = 5f;
    }

    public class FirePointAuthoringBaker : Baker<FirePointAuthoring>
    {
        public override void Bake(FirePointAuthoring authoring)
        {
            var actions = GetComponentInParent<FirstPersonControllerAuthoring>();
            if (actions == null || authoring.prefab == null)
                return;

            var entity = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(entity, new GunFirer
            {
                actionsEntity    = GetEntity(actions, TransformUsageFlags.None),
                rigidBodyPrefab  = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
                minScale         = authoring.initialScale,
                scaleUpStartTime = authoring.scaleUpStartTime,
                scaleUpEndTime   = authoring.scaleUpEndTime,
                exitVelocity     = authoring.exitVelocity,
            });
        }
    }
}

