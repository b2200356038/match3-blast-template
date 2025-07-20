using System.Collections.Generic;
using Grid.Items;
using Grid.Items.Obstacles;
using UnityEngine;

namespace Core 
{
    /// <summary>
    /// Handles finding matches of similar items in the grid
    /// Uses depth-first search to find connected matching items
    /// </summary>
    public class GridMatcher 
    {
        #region Private Variables
        private GridManager gridManager;
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize the grid matcher
        /// </summary>
        /// <param name="gridManager">Reference to the grid manager</param>
        public GridMatcher(GridManager gridManager) 
        {
            this.gridManager = gridManager;
        }
        #endregion

        #region Match Finding Methods
        /// <summary>
        /// Find all matching neighbors connected to the item at the given coordinates
        /// </summary>
        /// <param name="x">X coordinate in grid</param>
        /// <param name="y">Y coordinate in grid</param>
        /// <returns>List of matching connected items</returns>
        public List<BaseGridItem> FindMatchingNeighbors(int x, int y) 
        {
            List<BaseGridItem> matches = new List<BaseGridItem>();
            BaseGridItem startItem = gridManager.GetItemAt(x, y);
            
            // Skip if no item or item is an obstacle
            if (startItem == null || startItem is ObstacleItem) 
            {
                return matches;
            }
            
            // Create a visited array to track cells we've already checked
            bool[,] visited = new bool[gridManager.GridWidth, gridManager.GridHeight];
            
            // Use depth-first search to find all matches
            FindMatchesDFS(x, y, startItem.ItemType, matches, visited);
            
            return matches;
        }
        
        /// <summary>
        /// Recursive depth-first search to find all connected matching items
        /// </summary>
        /// <param name="x">Current X coordinate</param>
        /// <param name="y">Current Y coordinate</param>
        /// <param name="targetType">Item type to match</param>
        /// <param name="matches">List of matching items found so far</param>
        /// <param name="visited">Array tracking visited grid cells</param>
        private void FindMatchesDFS(int x, int y, GridItemType targetType, 
                                 List<BaseGridItem> matches, bool[,] visited) 
        {
            // Check if position is out of bounds
            if (x < 0 || x >= gridManager.GridWidth || y < 0 || y >= gridManager.GridHeight) 
            {
                return;
            }
            
            // Skip if already visited
            if (visited[x, y]) 
            {
                return;
            }
            
            // Get item at this position
            BaseGridItem item = gridManager.GetItemAt(x, y);
            
            // Skip if no item, wrong type, or item is moving
            if (item == null || item.ItemType != targetType || item.IsMoving) 
            {
                return;
            }
            
            // Mark as visited and add to matches
            visited[x, y] = true;
            matches.Add(item);
            
            // Continue search in all four directions
            FindMatchesDFS(x, y + 1, targetType, matches, visited); // Up
            FindMatchesDFS(x + 1, y, targetType, matches, visited); // Right
            FindMatchesDFS(x, y - 1, targetType, matches, visited); // Down
            FindMatchesDFS(x - 1, y, targetType, matches, visited); // Left
        }
        #endregion
    }
}