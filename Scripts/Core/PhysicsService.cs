using System.Collections;
using DG.Tweening;
using Grid.Items;
using Grid.Items.Obstacles;
using Grid.Items.Rockets;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Handles physics simulation for grid items, including falling and bounce effects
    /// </summary>
    public class PhysicsService: IGridPhysics
    {

        #region Private Variables
        private GridManager gridManager;
        private float cellSize;
        private float gravity = 20f;

        // Bounce animation parameters
        private float bounceScale = 1.2f;
        private float bounceDuration = 0.15f;
        private float bounceReturnDuration = 0.1f;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates a new GridPhysics instance
        /// </summary>
        /// <param name="gridManager">Reference to the grid manager</param>
        /// <param name="cellSize">Size of each grid cell</param>
        public PhysicsService(GridManager gridManager, float cellSize)
        {
            this.gridManager = gridManager;
            this.cellSize = cellSize;
        }
        #endregion

        #region Gravity Application
        /// <summary>
        /// Applies gravity to make items fall into empty spaces below them
        /// </summary>
        public IEnumerator ApplyGravity()
        {
            // Process each column
            for (int x = 0; x < gridManager.GridWidth; x++)
            {
                // Scan from top to bottom (process upper rows first)
                for (int y = gridManager.GridHeight - 1; y >= 0; y--)
                {
                    // Find empty cells
                    if (gridManager.GetItemAt(x, y) == null)
                    {
                        // Check if there's an item above that can fall
                        int aboveY = y + 1;
                        if (aboveY < gridManager.GridHeight)
                        {
                            BaseGridItem aboveItem = gridManager.GetItemAt(x, aboveY);
                            // Check if item can fall (not null, not a fixed obstacle, not already moving)
                            if (aboveItem != null &&
                                !(aboveItem is ObstacleItem && !(aboveItem is VaseItem)) &&
                                !aboveItem.IsMoving)
                            {
                                // Move the item to the empty space
                                gridManager.SetItemAt(x, y, aboveItem);
                                gridManager.SetItemAt(x, aboveY, null);
                                aboveItem.SetIsMoving(true);
                                ApplyFallingPhysics(aboveItem, x, y);
                            }
                        }
                    }
                }

                // Create new items at the top of the grid if needed
                if (gridManager.GetItemAt(x, gridManager.GridHeight - 1) == null)
                {
                    CreateItemAboveGrid(x);
                }
            }

            // Wait for falling animations to complete
            yield return null;
        }
        

        /// <summary>
        /// Creates a new random item above the grid to fall down
        /// </summary>
        private void CreateItemAboveGrid(int x, float initialVelocity = 0f)
        {
            int topY = gridManager.GridHeight - 1;
    
            // Check if rocket effect is active in this column
            if (gridManager.HasRocketEffectInColumn(x))
            {
                Debug.Log("Rocket effect active, not creating new item.");
                return;
            }
    
            // Calculate spawn position above the visible grid
            Vector3 spawnPosition = gridManager.CalculateWorldPosition(x, topY);
            spawnPosition.z = 0.1f;
            spawnPosition.y += cellSize;
            
            // Create a random cube
            BaseGridItem newItem = gridManager.ItemFactory.CreateRandomCube(x, topY, gridManager.GridContainer);
            if (newItem != null)
            {
                newItem.SetPosition(spawnPosition);
                gridManager.SetItemAt(x, topY, newItem);
                newItem.SetIsMoving(true);
                ApplyFallingPhysics(newItem, x, topY, initialVelocity);
            }
        }

        /// <summary>
        /// Applies falling physics to an item
        /// </summary>
        private void ApplyFallingPhysics(BaseGridItem item, int x, int y, float velocity = 0f)
        {
            if (item == null) return;
            
            // Calculate start and target positions
            Vector3 startPos = item.transform.position;
            Vector3 targetPos = gridManager.CalculateWorldPosition(x, y);
            
            // Adjust z-position based on item type
            if (item is RocketItem)
            {
                targetPos.z = startPos.z;
            }
            else
            {
                targetPos.z = -0.1f * y;
            }
            
            // Calculate physics parameters
            float distance = Vector3.Distance(startPos, targetPos);
            float acceleration = gravity;
            float initialVelocity = velocity;
            float delay = (initialVelocity == 0f) ? CountRowsToFixedObstacle(x, y) * 0.03f : 0f;
            float duration = SolveTime(distance, initialVelocity, acceleration);
            duration = Mathf.Max(duration, 0.05f);
            
            // Check for items two rows above to cascade the fall
            int aboveY = y + 2;
            if (aboveY < gridManager.GridHeight)
            {
                BaseGridItem aboveItem = gridManager.GetItemAt(x, aboveY);
                if (aboveItem != null &&
                    !(aboveItem is ObstacleItem && !(aboveItem is VaseItem)) &&
                    !aboveItem.IsMoving)
                {
                    // Move the item above to the newly emptied position
                    gridManager.SetItemAt(x, y+1, aboveItem);
                    gridManager.SetItemAt(x, aboveY, null);
                    aboveItem.SetIsMoving(true);
                    ApplyFallingPhysics(aboveItem, x, y+1, 0);
                }
            }

            // Create falling animation sequence
            Sequence sequence = DOTween.Sequence();
            
            // Add delay if needed
            if (initialVelocity == 0f && delay > 0f)
            {
                sequence.AppendInterval(delay);
            }

            // Add physics-based movement
            sequence.Append(
                DOTween.To(() => 0f, t =>
                        {
                            // s = v₀t + 0.5a t²
                            float currentDistance = initialVelocity * t + 0.5f * acceleration * t * t;
                            float t01 = currentDistance / distance;
                            t01 = Mathf.Clamp01(t01);
                            item.transform.position = Vector3.Lerp(startPos, targetPos, t01);
                        },
                        duration,
                        duration)
                    .SetEase(Ease.Linear)
            );

            // On completion callback
            sequence.OnComplete(() =>
            {
                // New final velocity: v = v₀ + at
                float finalVelocity = initialVelocity + acceleration * duration;
                item.transform.position = targetPos;
                CheckAndContinueFalling(item, x, y, finalVelocity);
            });
        }

        /// <summary>
        /// Checks if an item should continue falling and handles it accordingly
        /// </summary>
        private void CheckAndContinueFalling(BaseGridItem item, int x, int y, float velocity)
        {
            // Make sure item still exists
            if (item != null)
            {
                // Check if new item needs to be created at top of column
                if (gridManager.GetItemAt(x, gridManager.GridHeight - 1) == null && y == gridManager.GridHeight - 2)
                {
                    CreateItemAboveGrid(x, velocity);
                }

                // Check if there's an empty space below to continue falling
                if (y > 0 && gridManager.GetItemAt(x, y - 1) == null)
                {
                    // Continue falling to the next cell below
                    int newY = y - 1;
                    gridManager.SetItemAt(x, newY, item);
                    gridManager.SetItemAt(x, y, null);
                    ApplyFallingPhysics(item, x, newY, velocity);
                }
                else
                {
                    item.SetIsMoving(false);
                    PlayBounceAnimation(item, velocity);
                }
            }
        }
        #endregion

        #region Animation Effects
        /// <summary>
        /// Plays a bounce animation when an item lands
        /// </summary>
        /// <param name="item">The item to animate</param>
        /// <param name="velocity">The landing velocity</param>
        private void PlayBounceAnimation(BaseGridItem item, float velocity)
        {
            // Skip animation for rockets and non-vase obstacles
            if (item is RocketItem || (item is ObstacleItem && !(item is VaseItem)))
            {
                return;
            }

            // Calculate bounce intensity based on velocity
            float velocityFactor = Mathf.Clamp01(velocity / 20f);
            float currentBounceScale = 1f + (bounceScale - 1f) * velocityFactor;
            
            // Store original scale
            Vector3 originalScale = item.transform.localScale;
            Vector3 targetScale = new Vector3(
                originalScale.x * currentBounceScale,
                originalScale.y * (1f / currentBounceScale), // Compress vertically
                originalScale.z
            );
            
            // Create bounce sequence
            Sequence bounceSequence = DOTween.Sequence();
            
            // Step 1: Squash vertically and stretch horizontally
            bounceSequence.Append(item.transform.DOScale(targetScale, bounceDuration)
                .SetEase(Ease.OutQuad));
            
            // Step 2: Return to normal size with slight bounce
            bounceSequence.Append(item.transform.DOScale(originalScale, bounceReturnDuration)
                .SetEase(Ease.OutBack, 2f));
 
            
            bounceSequence.Play();
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Counts the number of rows to the nearest fixed obstacle or bottom
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns>Number of rows to obstacle</returns>
        private int CountRowsToFixedObstacle(int x, int y)
        {
            int rowCount = 0;
            for (int checkY = y - 1; checkY >= 0; checkY--)
            {
                BaseGridItem item = gridManager.GetItemAt(x, checkY);

                // Öğe null ise bitir
                if (item == null)
                {
                    break;
                }
        
                // Eğer öğe sabit bir engelse bitir - Vase hariç, çünkü o düşebilir
                if (item is ObstacleItem obstacle)
                {
                    // Vase değilse ve bir engelse (Box veya Stone) - bitir
                    if (!(obstacle is VaseItem))
                    {
                        break;
                    }
                }
                // Hareket etmeyen, engel olmayan bir öğeyse (örneğin stabil bir küp) - bitir
                else if (!item.IsMoving)
                {
                    break;
                }

                rowCount++;
            }

            return rowCount;
        }

        /// <summary>
        /// Solves for time given distance, initial velocity and acceleration
        /// Using quadratic formula to solve physics equation
        /// </summary>
        /// <param name="s">Distance</param>
        /// <param name="v0">Initial velocity</param>
        /// <param name="a">Acceleration</param>
        /// <returns>Time to travel the distance</returns>
        private float SolveTime(float s, float v0, float a)
        {
            // s = v₀t + 0.5at² → 0.5a t² + v₀t - s = 0
            float A = 0.5f * a;
            float B = v0;
            float C = -s;

            float discriminant = B * B - 4 * A * C;

            if (discriminant < 0)
                return 0f; // impossible case

            float sqrtDiscriminant = Mathf.Sqrt(discriminant);
            float t1 = (-B + sqrtDiscriminant) / (2 * A);
            float t2 = (-B - sqrtDiscriminant) / (2 * A);
            return Mathf.Max(t1, t2);
        }
        #endregion
    }
}