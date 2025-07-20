using System;
using Core;
using UnityEngine;

namespace Grid.Items.Rockets
{
    /// <summary>
    /// Base class for all rocket-type grid items
    /// </summary>
    public abstract class RocketItem : BaseGridItem
    {
        #region Enums
        /// <summary>
        /// Defines the direction of a rocket
        /// </summary>
        public enum RocketDirection 
        { 
            Horizontal, 
            Vertical 
        }
        #endregion
        
        #region Protected Variables
        /// <summary>
        /// The direction of this rocket
        /// </summary>
        protected RocketDirection direction;
        #endregion
        
        #region Properties
        /// <summary>
        /// Gets the direction of this rocket
        /// </summary>
        public RocketDirection Direction => direction;
        #endregion
        
        #region Public Methods
        public override void Initialize(GridItemType type, int x, int y, GridManager manager)
        {
            base.Initialize(type, x, y, manager);
            
            // Set direction based on type
            SetDirectionFromType(type);
        }
        
        private void SetDirectionFromType(GridItemType type)
        {
            if (type == GridItemType.HorizontalRocket)
            {
                direction = RocketDirection.Horizontal;
            }
            else if (type == GridItemType.VerticalRocket)
            {
                direction = RocketDirection.Vertical;
            }
        }
        #endregion
    }
}