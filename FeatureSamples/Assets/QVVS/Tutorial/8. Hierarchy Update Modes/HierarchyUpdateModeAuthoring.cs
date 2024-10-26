using Latios.Transforms;
using Latios.Transforms.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.QvvsSamples.Tutorial.Authoring
{
    public class HierarchyUpdateModeAuthoring : MonoBehaviour
    {
        public bool3 keepWorldPositions                        = false;
        public bool  keepWorldForwardDirection                 = false;
        public bool  keepWorldUpDirection                      = false;
        public bool  prioritizeUpDirectionOverForwardDirection = false;
        public bool  keepWorldScale                            = false;
    }

    public class HierarchyUpdateModeAuthoringBaker : Baker<HierarchyUpdateModeAuthoring>
    {
        public override void Bake(HierarchyUpdateModeAuthoring authoring)
        {
            if (!math.any(authoring.keepWorldPositions) &&
                !math.any(new bool4(authoring.keepWorldForwardDirection, authoring.keepWorldUpDirection, authoring.prioritizeUpDirectionOverForwardDirection,
                                    authoring.keepWorldScale)))
                return;

            var entity = GetEntity(TransformUsageFlags.Dynamic);
            var flags  = HierarchyUpdateMode.Flags.Normal;
            if (authoring.keepWorldPositions.x)
                flags |= HierarchyUpdateMode.Flags.WorldX;
            if (authoring.keepWorldPositions.y)
                flags |= HierarchyUpdateMode.Flags.WorldY;
            if (authoring.keepWorldPositions.z)
                flags |= HierarchyUpdateMode.Flags.WorldZ;
            if (authoring.keepWorldForwardDirection)
                flags |= HierarchyUpdateMode.Flags.WorldForward;
            if (authoring.keepWorldUpDirection)
                flags |= HierarchyUpdateMode.Flags.WorldUp;
            if (authoring.prioritizeUpDirectionOverForwardDirection)
                flags |= HierarchyUpdateMode.Flags.StrictUp;
            if (authoring.keepWorldScale)
                flags |= HierarchyUpdateMode.Flags.WorldScale;
            this.AddHiearchyModeFlags(entity, flags);
        }
    }
}

