using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons
{
    [DisallowMultipleComponent]
    public class SpawnerAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        public GameObject prefab;
        public int        spawnCount = 100;
        public Color      color;
        public float2     minMaxLinearVelocity   = new float2(1f, 10f);
        public float2     minMaxUniformScale     = new float2(0.5f, 10f);
        public float      minAreaNonUniformScale = 1f;
        public uint       seed                   = 23568926;

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(prefab);
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (color == default)
            {
                color = prefab.GetComponentInChildren<MeshRenderer>().sharedMaterial.GetColor("_Color");
            }

            dstManager.AddComponentData(entity, new Spawner
            {
                prefab                 = conversionSystem.GetPrimaryEntity(prefab),
                spawnCount             = spawnCount,
                color                  = new float4(color.r, color.g, color.b, 1),
                minMaxLinearVelocity   = minMaxLinearVelocity,
                minMaxUniformScale     = minMaxUniformScale,
                minAreaNonUniformScale = minAreaNonUniformScale,
                seed                   = seed
            });
        }
    }
}

