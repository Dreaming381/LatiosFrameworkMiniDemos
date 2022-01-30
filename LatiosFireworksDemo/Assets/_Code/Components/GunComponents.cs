using System;
using Latios;
using Latios.Psyshock;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Dragons
{
    public struct GunStats : IComponentData
    {
        public EntityWith<LocalToWorld> gunBarrelReference;
        public EntityWith<Prefab>       bulletPrefab;
        public EntityWith<Prefab>       effectPrefab;
        public float                    shotEffectLifetime;
        public float                    shotCooldown;
        public float                    reloadTime;
        public float                    shotExitSpeed;
        public int                      magazineCapacity;
    }

    public struct GunState : IComponentData
    {
        public float magazineCount;
        public bool  shooting;
        public float shotCooldown;
        public bool  reloading;
        public float timeToReload;
    }
}

