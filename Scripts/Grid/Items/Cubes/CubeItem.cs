using UnityEngine;
using DG.Tweening;

namespace Grid.Items.Cubes
{
    /// <summary>
    /// Base class for all cube-type grid items
    /// </summary>
    public abstract class CubeItem : BaseGridItem 
    {
        #region Inspector Variables
        [SerializeField] private SpriteRenderer hintSpriteRenderer;
        
        [Header("Hint Effect Settings")]
        [SerializeField] private float fadeDuration = 0.1f;
        #endregion
        
        #region Private Variables
        private bool isShowingHint = false;
        #endregion
        
        #region Public Methods
        /// <summary>
        /// Shows or hides the rocket hint effect on this cube
        /// </summary>
        /// <param name="show">Whether to show or hide the hint</param>
        public void ShowHint(bool show) 
        {
            // Skip if already in requested state or no hint renderer
            if (isShowingHint == show || hintSpriteRenderer == null) return;
            
            isShowingHint = show;
            
            if (show) 
            { 
                // Fade in hint sprite
                hintSpriteRenderer.DOFade(1f, fadeDuration)
                    .SetEase(Ease.InQuad);
            } 
            else 
            {
                // Fade out hint sprite
                hintSpriteRenderer.DOFade(0f, fadeDuration)
                    .SetEase(Ease.OutQuad);
            }
        }

        /// <summary>
        /// Gets the color of this cube
        /// </summary>
        /// <returns>The cube color enum value</returns>
        public abstract CubeColor GetCubeColor();
        
        /// <summary>
        /// Disable item and kill any running animations
        /// </summary>
        public override void DisableItem() 
        {
            // Kill any running animations
            if (spriteRenderer != null) 
            {
                spriteRenderer.DOKill();
            }
            
            if (hintSpriteRenderer != null) 
            {
                hintSpriteRenderer.DOKill();
            }
            
            base.DisableItem();
        }
        
        /// <summary>
        /// Set whether this item is moving
        /// </summary>
        public override void SetIsMoving(bool value) 
        {
            base.SetIsMoving(value);
            
            // Hide hint when moving
            if (value) 
            {
                ShowHint(false);
            }
        }
        #endregion
        
        #region Enums
        /// <summary>
        /// Enumeration of cube colors
        /// </summary>
        public enum CubeColor 
        {
            Red,
            Green,
            Blue,
            Yellow
        }
        #endregion
    }
}