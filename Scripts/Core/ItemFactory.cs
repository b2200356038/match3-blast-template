using System.Collections.Generic;
using Grid.Items;
using UnityEngine;
using Data;

namespace Core 
{
    /// <summary>
    /// Factory responsible for creating grid item instances based on their type
    /// Follows Factory pattern to centralize object creation logic
    /// </summary>
    public class ItemFactory : MonoBehaviour 
    {
        #region Serialized Fields
        [Header("Cube Prefabs")]
        [SerializeField] private GameObject redCubePrefab;
        [SerializeField] private GameObject greenCubePrefab;
        [SerializeField] private GameObject blueCubePrefab;
        [SerializeField] private GameObject yellowCubePrefab;
        private Dictionary<int, GameObject> test;
        
        [Header("Rocket Prefabs")]
        [SerializeField] private GameObject horizontalRocketPrefab;
        [SerializeField] private GameObject verticalRocketPrefab;
        
        [Header("Obstacle Prefabs")]
        [SerializeField] private GameObject boxPrefab;
        [SerializeField] private GameObject stonePrefab;
        [SerializeField] private GameObject vasePrefab;
        
        [Header("Rocket Pieces")]
        [SerializeField] private GameObject leftProjectilePrefab;
        [SerializeField] private GameObject rightProjectilePrefab;
        [SerializeField] private GameObject upProjectilePrefab;
        [SerializeField] private GameObject downProjectilePrefab;
        
        [SerializeField]private GridManager gridManager;
        #endregion

        #region Public Methods
        /// <summary>
        /// Creates a grid item of the specified type at the given coordinates
        /// </summary>
        public BaseGridItem CreateGridItem(GridItemType type, int x, int y, Transform parent) 
        {
            GameObject prefab = GetPrefabForType(type);
            
            if (prefab == null) 
            {
                Debug.LogError($"Prefab not found for type: {type}");
                return null;
            }
            
            GameObject itemObject = Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);
            itemObject.name = type.ToString();
            
            BaseGridItem gridItem = itemObject.GetComponent<BaseGridItem>();
            
            if (gridItem != null) 
            {
                gridItem.Initialize(type, x, y, gridManager);
            }
            else
            {
                Debug.LogError($"BaseGridItem component not found on: {type}");
            }
            
            return gridItem;
        }
        
        private void Awake()
        {
            if (gridManager == null)
            {
                Debug.LogError("ItemFactory: GridManager component not found!");
            }
        }
        
        /// <summary>
        /// Creates a random cube item at the specified grid coordinates
        /// </summary>
        public BaseGridItem CreateRandomCube(int x, int y, Transform parent) 
        {
            GridItemType randomType = GetRandomCubeType();
            return CreateGridItem(randomType, x, y, parent);
        }
        
        /// <summary>
        /// Gets the left projectile prefab for rocket explosions
        /// </summary>
        public GameObject GetLeftProjectilePrefab()
        {
            return leftProjectilePrefab;
        }
        
        /// <summary>
        /// Gets the right projectile prefab for rocket explosions
        /// </summary>
        public GameObject GetRightProjectilePrefab()
        {
            return rightProjectilePrefab;
        }
        
        /// <summary>
        /// Gets the upward projectile prefab for rocket explosions
        /// </summary>
        public GameObject GetUpProjectilePrefab()
        {
            return upProjectilePrefab;
        }
        
        /// <summary>
        /// Gets the downward projectile prefab for rocket explosions
        /// </summary>
        public GameObject GetDownProjectilePrefab()
        {
            return downProjectilePrefab;
        }

        /// <summary>
        /// Checks if the given type is a cube type
        /// </summary>
        public static bool IsCubeType(GridItemType type)
        {
            return type == GridItemType.RedCube || 
                   type == GridItemType.GreenCube || 
                   type == GridItemType.BlueCube || 
                   type == GridItemType.YellowCube;
        }
        
        /// <summary>
        /// Checks if the given type is a rocket type
        /// </summary>
        public static bool IsRocketType(GridItemType type)
        {
            return type == GridItemType.HorizontalRocket || 
                   type == GridItemType.VerticalRocket;
        }
        
        /// <summary>
        /// Checks if the given type is an obstacle type
        /// </summary>
        public static bool IsObstacleType(GridItemType type)
        {
            return type == GridItemType.Box || 
                   type == GridItemType.Stone || 
                   type == GridItemType.Vase;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Returns the appropriate prefab for the given grid item type
        /// </summary>
        private GameObject GetPrefabForType(GridItemType type)
        {
            switch(type)
            {
                // Cubes
                case GridItemType.RedCube:
                    return redCubePrefab;
                case GridItemType.GreenCube:
                    return greenCubePrefab;
                case GridItemType.BlueCube:
                    return blueCubePrefab;
                case GridItemType.YellowCube:
                    return yellowCubePrefab;
                
                // Rockets
                case GridItemType.HorizontalRocket:
                    return horizontalRocketPrefab;
                case GridItemType.VerticalRocket:
                    return verticalRocketPrefab;
                
                // Obstacles
                case GridItemType.Box:
                    return boxPrefab;
                case GridItemType.Stone:
                    return stonePrefab;
                case GridItemType.Vase:
                    return vasePrefab;
                
                // Unknown type
                default:
                    Debug.LogWarning($"Unexpected grid item type: {type}");
                    return null;
            }
        }
        
        /// <summary>
        /// Selects a random cube type
        /// </summary>
        private GridItemType GetRandomCubeType()
        {
            int random = Random.Range(0, 4);
            switch (random)
            {
                case 0: return GridItemType.RedCube;
                case 1: return GridItemType.GreenCube;
                case 2: return GridItemType.BlueCube;
                default: return GridItemType.YellowCube;
            }
        }
        #endregion
    }
}