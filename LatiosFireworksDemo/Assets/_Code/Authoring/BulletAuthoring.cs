using Unity.Entities;
using UnityEngine;

namespace Dragons
{
    [DisallowMultipleComponent]
    public class BulletAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public float radius;
        public float mass;
        public float lifetime;
        public int   damage;
        public float referenceSpeed;
        public float warpedSpeed;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new TimeToLive { timeToLive = lifetime });
            dstManager.AddComponent<PreviousTranslation>(entity);

            dstManager.AddComponentData(entity, new Damage { damage = damage });
            dstManager.AddComponent<Firer>(entity);
            dstManager.AddComponentData(entity, new SphericalBallisticsStats
            {
                radius             = radius,
                inverseMass        = 1f / mass,
                timeWarpMultiplier = warpedSpeed / referenceSpeed
            });
            dstManager.AddComponent<SphericalBallisticsState>(      entity);
            dstManager.AddComponent<DieOnCollideWithEnvironmentTag>(entity);
        }
    }
}

