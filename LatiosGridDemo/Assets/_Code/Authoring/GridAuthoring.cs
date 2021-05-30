using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Dragons
{
    [DisallowMultipleComponent]
    public class GridAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        public int gridWidth;

        public GameObject tilePrefab0;
        public GameObject tilePrefab1;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new GridConfiguration
            {
                cellsPerRow = gridWidth,
                rows        = gridWidth,
                prefabTile0 = conversionSystem.GetPrimaryEntity( tilePrefab0),
                prefabTile1 = conversionSystem.GetPrimaryEntity( tilePrefab1 )
            });
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(tilePrefab0);
            referencedPrefabs.Add(tilePrefab1);
        }
    }
}

