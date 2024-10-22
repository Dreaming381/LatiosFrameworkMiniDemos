using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons.QvvsSamples.Tutorial.Authoring
{
    public class WorldSpawnerAuthoring : MonoBehaviour
    {
        public GameObject spawnable;
        public float3     spawnOffsetFromPlayer;
    }

    public class WorldSpawnerAuthoringBaker : Baker<WorldSpawnerAuthoring>
    {
        public override void Bake(WorldSpawnerAuthoring authoring)
        {
            if (authoring.spawnable == null)
                return;

            var entity = GetEntity(TransformUsageFlags.Renderable);

            AddComponent(entity, new WorldSpawner
            {
                spawnable             = GetEntity(authoring.spawnable, TransformUsageFlags.Renderable),
                spawnOffsetFromPlayer = authoring.spawnOffsetFromPlayer
            });
        }
    }
}

