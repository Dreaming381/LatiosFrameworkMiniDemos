using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.PsyshockSamples.CollisionSimple
{
    public class CollidingCharacterAuthoring : MonoBehaviour
    {
        public float mass;
    }

    public class CollidingCharacterAuthoringBaker : Baker<CollidingCharacterAuthoring>
    {
        public override void Bake(CollidingCharacterAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CharacterPhysicsStats
            {
                mass = authoring.mass,
            });
            AddComponent<CharacterPhysicsState>(entity);
        }
    }
}

