using System.Collections.Generic;
using Grid.Items;
using Grid.Items.Obstacles;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Service responsible for all obstacle-related operations in the game
    /// </summary>
    public class ObstacleService : MonoBehaviour, IObstacleService
    {
        #region Private Variables

        private GridManager gridManager;
        private GameManager gameManager;
        private Dictionary<GridItemType, int> obstacleCountsByType = new Dictionary<GridItemType, int>();
        public int remainingObstacles = 0;
        private UIManager uiManager;
        private HashSet<ObstacleItem> damagedObstacles = new HashSet<ObstacleItem>();

        #endregion

        #region Initialization

        private void Awake()
        {
            // Get reference to GridManager
            gridManager = GetComponent<GridManager>();
            if (gridManager == null)
            {
                gridManager = FindFirstObjectByType<GridManager>();
            }
            if (gridManager == null)
            {
                Debug.LogError("ObstacleService couldn't find GridManager reference!");
            }

            InitializeObstacleCounts();
        }

        /// <summary>
        /// Initialize obstacle counts dictionary
        /// </summary>
        public void InitializeObstacleCounts()
        {
            obstacleCountsByType.Clear();
            obstacleCountsByType[GridItemType.Box] = 0;
            obstacleCountsByType[GridItemType.Stone] = 0;
            obstacleCountsByType[GridItemType.Vase] = 0;
            remainingObstacles = 0;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get total count of remaining obstacles
        /// </summary>
        public int GetRemainingObstacles()
        {
            return remainingObstacles;
        }

        /// <summary>
        /// Get remaining obstacle count by type
        /// </summary>
        public int GetObstacleCount(GridItemType obstacleType)
        {
            if (obstacleCountsByType.ContainsKey(obstacleType))
            {
                return obstacleCountsByType[obstacleType];
            }

            return 0;
        }

        /// <summary>
        /// Get dictionary of all obstacle counts by type
        /// </summary>
        public Dictionary<GridItemType, int> GetAllObstacleCounts()
        {
            return new Dictionary<GridItemType, int>(obstacleCountsByType);
        }

        /// <summary>
        /// Increment obstacle count for a type
        /// </summary>
        public void IncrementObstacle(GridItemType obstacleType)
        {
            if (obstacleCountsByType.ContainsKey(obstacleType))
            {
                obstacleCountsByType[obstacleType]++;
                remainingObstacles++;
            }
            else
            {
                obstacleCountsByType[obstacleType] = 1;
                remainingObstacles++;
            }
        }

        /// <summary>
        /// Decrement obstacle count for a type
        /// </summary>
        public void DecrementObstacle(GridItemType obstacleType)
        {
            if (obstacleCountsByType.ContainsKey(obstacleType) && obstacleCountsByType[obstacleType] > 0)
            {
                obstacleCountsByType[obstacleType]--;
                remainingObstacles--;

                // Notify GameManager about obstacle destruction
                GameManager.Instance.OnObstacleDestroyed(obstacleType, obstacleCountsByType[obstacleType]);

                // Update UI through UIManager
                UIManager
                    .Instance.UpdateObstacleCount(obstacleType, obstacleCountsByType[obstacleType]);

                // Check if all obstacles are cleared
                if (remainingObstacles <= 0)
                {
                    gridManager.CheckGameState();
                }
            }
        }

        /// <summary>
        /// Damage obstacles adjacent to given coordinates
        /// </summary>
        public void DamageAdjacentObstacles(int x, int y)
        {
            // Clear damaged obstacles set for new damage sequence
            // Check in 4 directions
            DamageObstacle(x, y + 1); // Up
            DamageObstacle(x + 1, y); // Right
            DamageObstacle(x, y - 1); // Down
            DamageObstacle(x - 1, y); // Left
        }

        public void ClearDamagedObstacles()
        {
            damagedObstacles.Clear();
        }

        /// <summary>
        /// Apply damage to obstacle at coordinates
        /// </summary>
        public bool DamageObstacle(int x, int y, bool fromRocket = false)
        {
            // Check grid bounds
            if (x < 0 || x >= gridManager.GridWidth || y < 0 || y >= gridManager.GridHeight)
                return false;

            // Get the item at coordinates
            BaseGridItem item = gridManager.GetItemAt(x, y);

            // Check if it's an obstacle
            if (item == null || !(item is ObstacleItem obstacle))
                return false;

            // Stone obstacles only take damage from rockets
            if (obstacle.ItemType == GridItemType.Stone && !fromRocket)
                return false;

            // Check if already damaged in this sequence
            if (damagedObstacles.Contains(obstacle) && fromRocket)
                return false;

            // Store the obstacle type
            GridItemType obstacleType = obstacle.ItemType;

            // Mark as damaged in this sequence
            if(!fromRocket)damagedObstacles.Add(obstacle);

            // Apply damage
            bool isDestroyed = obstacle.TakeDamage(1);

            // Handle destruction
            if (isDestroyed)
            {
                gridManager.SetItemAt(x, y, null);
                DecrementObstacle(obstacleType);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get all damageble obstacles for a move
        /// </summary>
        public bool HasObstaclesRemaining()
        {
            return remainingObstacles > 0;
        }

        #endregion
    }
}