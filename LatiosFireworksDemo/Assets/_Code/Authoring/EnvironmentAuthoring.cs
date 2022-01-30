using Unity.Entities;
using UnityEngine;

namespace Dragons
{
    public class EnvironmentAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var colliderComponents = gameObject.GetComponentsInChildren<Collider>();
            if (colliderComponents != null)
            {
                foreach (var colliderComponent in colliderComponents)
                {
                    var e = conversionSystem.TryGetPrimaryEntity(colliderComponent);
                    if (e != Entity.Null)
                        dstManager.AddComponent<EnvironmentTag>(e);
                }
            }
            dstManager.AddComponent<EnvironmentTag>(entity);
        }
    }
}

