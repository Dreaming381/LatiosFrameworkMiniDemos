using Latios.Psyshock;
using Latios.Transforms.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// Post-Jam Notes:
// These are the parameters we used to describe the rigid body, largely copied from Free Parking.
// The combining mechanism used for friction and restitution are hardcoded into the system.
// The change from Free Parking was the addition of sound for when the rigid body collides with things.

namespace BB
{
    public class RigidBodyAuthoring : MonoBehaviour
    {
        public float mass = 1f;

        [Range(0, 1)] public float coefficientOfFriction    = 0.5f;
        [Range(0, 1)] public float coefficientOfRestitution = 0.5f;
        [Range(0, 1)] public float linearDamping            = 0.05f;
        [Range(0, 1)] public float angularDamping           = 0.05f;

        public GameObject bumpSoundPrefab;
    }

    public class RigidBodyAuthoringBaker : Baker<RigidBodyAuthoring>
    {
        public override void Bake(RigidBodyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new RigidBody
            {
                mass = new UnitySim.Mass
                {
                    inverseMass = math.rcp(authoring.mass)
                },
                coefficientOfFriction    = authoring.coefficientOfFriction,
                coefficientOfRestitution = authoring.coefficientOfRestitution,
                linearDamping            = authoring.linearDamping,
                angularDamping           = authoring.angularDamping,
                bumpSound                = GetEntity(authoring.bumpSoundPrefab, TransformUsageFlags.None),
            });
            AddComponent<PreviousTransformRequest>(entity);
        }

        [BakingType]
        struct PreviousTransformRequest : IRequestPreviousTransform { }
    }
}

