using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Post-Jam Notes:
// Vertical aim is treated separate from horizontal aim, and exists on a child entity.
// The gun is parented to this entity.

namespace BB
{
    public class FirstPersonVerticalAimAuthoring : MonoBehaviour
    {
        public float minAngle = -80f;
        public float maxAngle = 80f;

        private void OnValidate()
        {
            minAngle = math.max(minAngle, -89.9f);
            maxAngle = math.min(maxAngle, 89.9f);
        }
    }

    public class FirstPersonVerticalAimAuthoringBaker : Baker<FirstPersonVerticalAimAuthoring>
    {
        public override void Bake(FirstPersonVerticalAimAuthoring authoring)
        {
            var controller = GetComponentInParent<FirstPersonControllerAuthoring>();
            if (controller == null)
                return;

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new FirstPersonVerticalAimStats
            {
                actionsEntity = GetEntity(controller, TransformUsageFlags.None),
                minSinLimit   = math.sin(math.radians(authoring.minAngle)),
                maxSinLimit   = math.sin(math.radians(authoring.maxAngle)),
            });
        }
    }
}

