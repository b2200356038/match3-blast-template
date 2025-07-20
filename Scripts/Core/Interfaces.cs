using System.Collections;
using System.Collections.Generic;
using Grid.Items;
using Grid.Items.Rockets;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Interface for rocket-related operations in the game
    /// </summary>
    public interface IRocketService
    {
        /// <summary>
        /// Process a click on a rocket item
        /// </summary>
        void ProcessRocketClick(RocketItem rocket);
        
        /// <summary>
        /// Create and launch rocket projectiles
        /// </summary>
        void CreateAndLaunchRocketParts(bool isHorizontal, int startX, int startY);
        
        /// <summary>
        /// Create a rocket with animation from matched cubes
        /// </summary>
        IEnumerator CreateRocketWithAnimation(List<BaseGridItem> matches, int rocketX, int rocketY);
        
        /// <summary>
        /// Add a column to the list of columns with rocket effects
        /// </summary>
        void AddColumnWithRocketEffect(int columnX);
        
        /// <summary>
        /// Clear the list of columns with rocket effects
        /// </summary>
        void ClearRocketEffectColumns();
        
        /// <summary>
        /// Check if a column has a rocket effect
        /// </summary>
        bool HasRocketEffectInColumn(int columnX);
    }
    
    public interface IObstacleService
    {
        /// <summary>
        /// Initialize obstacle counts for tracking
        /// </summary>
        void InitializeObstacleCounts();
        
        /// <summary>
        /// Get total count of remaining obstacles
        /// </summary>
        int GetRemainingObstacles();
        
        /// <summary>
        /// Get remaining obstacle count by type
        /// </summary>
        int GetObstacleCount(GridItemType obstacleType);
        
        /// <summary>
        /// Get dictionary of all obstacle counts by type
        /// </summary>
        Dictionary<GridItemType, int> GetAllObstacleCounts();
        
        /// <summary>
        /// Increment obstacle count for a type
        /// </summary>
        void IncrementObstacle(GridItemType obstacleType);
        
        /// <summary>
        /// Decrement obstacle count for a type
        /// </summary>
        void DecrementObstacle(GridItemType obstacleType);
        
        /// <summary>
        /// Damage obstacles adjacent to given coordinates
        /// </summary>
        void DamageAdjacentObstacles(int x, int y);
        
        /// <summary>
        /// Apply damage to obstacle at coordinates
        /// </summary>
        bool DamageObstacle(int x, int y, bool fromRocket = false);
    }
    
    /// <summary>
    /// Interface for grid physics operations
    /// </summary>
    public interface IGridPhysics
    {
        /// <summary>
        /// Applies gravity to make items fall into empty spaces
        /// </summary>
        IEnumerator ApplyGravity();
        
    }
}