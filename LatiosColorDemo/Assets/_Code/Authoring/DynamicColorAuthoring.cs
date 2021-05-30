using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons
{
    [DisallowMultipleComponent]
    public class DynamicColorAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        public GameObject hitSoundPrefab;

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(hitSoundPrefab);
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new HitSound { prefab = conversionSystem.TryGetPrimaryEntity(hitSoundPrefab) });

            dstManager.AddComponent<TranslationalVelocity>( entity);
            dstManager.AddComponent<AngularVelocity>(       entity);
            dstManager.AddComponent<DynamicColor>(          entity);

            foreach(var mr in GetComponentsInChildren<MeshRenderer>())
            {
                dstManager.AddComponent<DynamicColor>(conversionSystem.GetPrimaryEntity(mr));
            }
        }
    }
}

