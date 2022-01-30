using System.Collections.Generic;
using Latios;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Dragons
{
    public class GunAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        public GameObject bulletPrefab;
        public GameObject gunBarrelTip;
        public GameObject shotEffectPrefab;
        public float      shotExitSpeed;
        public float      shotEffectLifetime;
        public float      rateOfFire;
        public int        magazineCapacity;
        public float      reloadTime;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new GunStats
            {
                bulletPrefab       = conversionSystem.TryGetPrimaryEntity(bulletPrefab),
                gunBarrelReference = conversionSystem.TryGetPrimaryEntity(gunBarrelTip),
                effectPrefab       = conversionSystem.TryGetPrimaryEntity(shotEffectPrefab),
                shotExitSpeed      = shotExitSpeed,
                shotEffectLifetime = shotEffectLifetime,
                shotCooldown       = 1f / rateOfFire,
                magazineCapacity   = magazineCapacity,
                reloadTime         = reloadTime
            });
            dstManager.AddComponentData(entity, new GunState
            {
                magazineCount = magazineCapacity,
                shooting      = false,
                shotCooldown  = 0f,
                reloading     = false,
                timeToReload  = 0f
            });
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(bulletPrefab);
            referencedPrefabs.Add(shotEffectPrefab.gameObject);
        }
    }
}

