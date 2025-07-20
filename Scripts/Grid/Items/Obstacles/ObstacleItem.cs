using Core;
using UnityEngine;

namespace Grid.Items.Obstacles 
{
    /// <summary>
    /// Base class for all obstacle-type grid items
    /// </summary>
    public abstract class ObstacleItem : BaseGridItem 
    {
        #region Inspector Variables
        [SerializeField] protected int maxHealth = 1;
        #endregion
        
        #region Protected Variables
        /// <summary>
        /// Current health points of the obstacle
        /// </summary>
        protected int currentHealth;
        #endregion
        
        #region Properties
        /// <summary>
        /// Current health of the obstacle
        /// </summary>
        public int CurrentHealth => currentHealth;
        
        /// <summary>
        /// Obstacles are not clickable
        /// </summary>
        public override bool IsClickable => false;
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Initialize the obstacle with a specific type and position
        /// </summary>
        /// <param name="type">The obstacle type</param>
        /// <param name="x">X coordinate in grid</param>
        /// <param name="y">Y coordinate in grid</param>
        /// <param name="manager">Reference to the grid manager</param>
        public override void Initialize(GridItemType type, int x, int y, GridManager manager)
        {
            base.Initialize(type, x, y, manager);
            currentHealth = maxHealth;
        }
        
        /// <summary>
        /// Apply damage to the obstacle
        /// </summary>
        /// <param name="amount">Amount of damage to apply</param>
        /// <returns>True if the obstacle was destroyed, false otherwise</returns>
        public virtual bool TakeDamage(int amount) 
        {
            currentHealth -= amount;
            
            if (currentHealth <= 0) 
            {
                DestroyItem();
                return true; // Destroyed
            }
            
            return false; // Still alive
        }
        #endregion
    }
}