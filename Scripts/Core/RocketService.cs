using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Grid.Items;
using Grid.Items.Obstacles;
using Grid.Items.Rockets;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Service responsible for all rocket-related operations
    /// </summary>
    public class RocketService : MonoBehaviour, IRocketService
    {
        #region Inspector Variables
        [SerializeField] private float rocketZPosition = -3f;
        [SerializeField] private float moveTime = 0.04f;
        [SerializeField] private int extraMovesAfterExit = 10;
        #endregion

        #region Private Variables
        private GridManager gridManager;
        private ObstacleService obstacleService; // ObstacleService referansı eklendi
        private HashSet<int> columnsWithRocketEffect = new HashSet<int>();
        private WaitForSeconds cachedMoveWait;
        private int activeRocketParts = 0;
        private UIManager cachedUIManager;
        
        // Fallback için yedek obstacle sayaçları
        private Dictionary<GridItemType, int> fallbackObstacleCounts = new Dictionary<GridItemType, int>()
        {
            { GridItemType.Box, 0 },
            { GridItemType.Stone, 0 },
            { GridItemType.Vase, 0 }
        };
        #endregion

        #region Initialization
        private void Awake()
        {
            Debug.Log("RocketService Awake starting...");
            
            // Get reference to GridManager
            gridManager = GetComponent<GridManager>();
            if (gridManager == null)
            {
                Debug.LogWarning("RocketService: GridManager not found on same GameObject, searching scene...");
                gridManager = FindFirstObjectByType<GridManager>();
            }
            
            if (gridManager == null)
            {
                Debug.LogError("RocketService couldn't find GridManager reference!");
            }
            
            // ObstacleService referansını al
            obstacleService = GetComponent<ObstacleService>();
            if (obstacleService == null)
            {
                Debug.LogWarning("RocketService: ObstacleService not found on same GameObject, searching scene...");
                obstacleService = FindFirstObjectByType<ObstacleService>();
            }
            
            if (obstacleService == null)
            {
                Debug.LogError("RocketService couldn't find ObstacleService reference!");
            }
            cachedMoveWait = new WaitForSeconds(moveTime);
            
            
            cachedUIManager = FindFirstObjectByType<UIManager>();
            if (cachedUIManager == null)
            {
                Debug.LogWarning("RocketService: UIManager not found during initialization");
            } 
            Debug.Log($"RocketService Awake completed. Found GridManager: {gridManager != null}, ObstacleService: {obstacleService != null}");
        }
        
        private void Start()
        {
            // Start metodu da bir kez daha referans almayı dene
            if (obstacleService == null)
            {
                Debug.LogWarning("RocketService: Trying to find ObstacleService again in Start...");
                obstacleService = FindFirstObjectByType<ObstacleService>();
                
                if (obstacleService != null)
                {
                    Debug.Log("RocketService: ObstacleService found in Start method.");
                }
                else
                {
                    Debug.LogError("RocketService: ObstacleService still not found in Start!");
                }
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Process a click on a rocket item
        /// </summary>
        public void ProcessRocketClick(RocketItem rocket)
        {
            // Check for adjacent rockets for combo
            List<BaseGridItem> neighborRockets = CheckAdjacentRockets(rocket.GridX, rocket.GridY);

            // Handle rocket combo
            if (neighborRockets.Count > 0)
            {
                // Add clicked rocket to front of list
                neighborRockets.Insert(0, rocket);
                ProcessRocketCombo(rocket, neighborRockets);
                return;
            }

            // Standard rocket explosion
            CreateAndLaunchRocketParts(
                rocket.ItemType == GridItemType.HorizontalRocket,
                rocket.GridX,
                rocket.GridY
            );
            gridManager.SetItemAt(rocket.GridX, rocket.GridY, null);
            rocket.DestroyItem();
        }

        /// <summary>
        /// Create and launch rocket projectiles
        /// </summary>
        public void CreateAndLaunchRocketParts(bool isHorizontal, int startX, int startY)
        {
            activeRocketParts += 2;
            Vector3 startPosition = gridManager.CalculateWorldPosition(startX, startY);
            
            if (isHorizontal)
            {
                // Create left and right projectiles
                GameObject leftPart = CreateRocketProjectile(
                    gridManager.ItemFactory.GetLeftProjectilePrefab(),
                    startPosition,
                    rocketZPosition);

                GameObject rightPart = CreateRocketProjectile(
                    gridManager.ItemFactory.GetRightProjectilePrefab(),
                    startPosition,
                    rocketZPosition);

                // Launch projectiles
                StartCoroutine(MoveRocketProjectile(leftPart, new Vector2Int(-1, 0), startX, startY));
                StartCoroutine(MoveRocketProjectile(rightPart, new Vector2Int(1, 0), startX, startY));
            }
            else
            {
                // Create up and down projectiles
                GameObject downPart = CreateRocketProjectile(
                    gridManager.ItemFactory.GetDownProjectilePrefab(),
                    startPosition,
                    rocketZPosition);

                GameObject upPart = CreateRocketProjectile(
                    gridManager.ItemFactory.GetUpProjectilePrefab(),
                    startPosition,
                    rocketZPosition);
                    
                // Mark column as having rocket effect
                AddColumnWithRocketEffect(startX);
                
                // Launch projectiles
                StartCoroutine(MoveRocketProjectile(downPart, new Vector2Int(0, -1), startX, startY));
                StartCoroutine(MoveRocketProjectile(upPart, new Vector2Int(0, 1), startX, startY));
            }
        }

        /// <summary>
        /// Create a rocket with animation from matched cubes
        /// </summary>
        public IEnumerator CreateRocketWithAnimation(List<BaseGridItem> matches, int rocketX, int rocketY)
        {
            BaseGridItem clickedItem = matches[0];
            
            // Create rocket
            GridItemType rocketType = matches.Count % 2 == 0 ? 
                GridItemType.HorizontalRocket : GridItemType.VerticalRocket;
                
            RocketItem rocket = gridManager.ItemFactory.CreateGridItem(rocketType, rocketX, rocketY, gridManager.GridContainer) as RocketItem;
            
            if (rocket == null)
            {
                yield break;
            }

            // Position rocket
            Vector3 rocketPosition = gridManager.CalculateWorldPosition(rocketX, rocketY);
            rocketPosition.z = rocketZPosition;
            rocket.transform.position = rocketPosition;
            rocket.transform.localScale = Vector3.zero;
            gridManager.SetItemAt(rocketX, rocketY, rocket);
            
            // Disable and animate matched cubes
            foreach (BaseGridItem cube in matches)
            {
                cube.DisableItem();
                if (clickedItem == cube) continue;
                gridManager.SetItemAt(cube.GridX, cube.GridY, null);
                cube.transform.DOMove(clickedItem.transform.position, 0.2f)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() => { cube.DestroyItem(); });
            }

            // Wait for cubes to move
            yield return new WaitForSeconds(0.2f);
            
            // Scale up rocket with bounce effect
            rocket.transform.DOScale(Vector3.one, 0.2f)
                .SetEase(Ease.OutBack);
                
            // Destroy clicked item
            clickedItem.DestroyItem();
            
            // Small delay before applying gravity
            yield return new WaitForSeconds(0.1f);

            // Apply gravity
            StartCoroutine(gridManager.ApplyGravity());
        }

        /// <summary>
        /// Add a column to the list of columns with rocket effects
        /// </summary>
        public void AddColumnWithRocketEffect(int columnX)
        {
            columnsWithRocketEffect.Add(columnX);
        }

        /// <summary>
        /// Clear the list of columns with rocket effects
        /// </summary>
        public void ClearRocketEffectColumns()
        {
            columnsWithRocketEffect.Clear();
        }

        /// <summary>
        /// Check if a column has a rocket effect
        /// </summary>
        public bool HasRocketEffectInColumn(int columnX)
        {
            return columnsWithRocketEffect.Contains(columnX);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Check for rockets in adjacent cells
        /// </summary>
        private List<BaseGridItem> CheckAdjacentRockets(int x, int y)
        {
            List<BaseGridItem> adjacentRockets = new List<BaseGridItem>();

            // Check in 4 directions
            CheckForRocket(x, y + 1, adjacentRockets); // Up
            CheckForRocket(x + 1, y, adjacentRockets); // Right
            CheckForRocket(x, y - 1, adjacentRockets); // Down
            CheckForRocket(x - 1, y, adjacentRockets); // Left

            return adjacentRockets;
        }

        /// <summary>
        /// Check if there's a rocket at coordinates
        /// </summary>
        private void CheckForRocket(int x, int y, List<BaseGridItem> rockets)
        {
            // Check grid bounds
            if (x < 0 || x >= gridManager.GridWidth || y < 0 || y >= gridManager.GridHeight)
                return;

            // Check for rocket
            BaseGridItem item = gridManager.GetItemAt(x, y);
            if (item != null && item is RocketItem)
            {
                rockets.Add(item);
            }
        }

        /// <summary>
        /// Process a rocket combo (multiple rockets exploding together)
        /// </summary>
        private void ProcessRocketCombo(RocketItem clickedRocket, List<BaseGridItem> rockets)
        {
            // Decrement moves
            DecrementMoves();
            
            int centerX = clickedRocket.GridX;
            int centerY = clickedRocket.GridY;

            // Keep clicked rocket in grid, remove others
            foreach (BaseGridItem item in rockets)
            {
                item.DisableItem();
                if (item != clickedRocket && item is RocketItem)
                {
                    gridManager.SetItemAt(item.GridX, item.GridY, null);
                    item.DisableItem();
                }
            }

            // Start combo animation
            StartCoroutine(PlayRocketComboAnimation(clickedRocket, rockets));
        }

        /// <summary>
        /// Play animation for rocket combo
        /// </summary>
        private IEnumerator PlayRocketComboAnimation(RocketItem clickedRocket, List<BaseGridItem> rockets)
        {
            // Pre-movement delay
            yield return new WaitForSeconds(0.1f);

            // Move all rockets to center
            foreach (BaseGridItem item in rockets)
            {
                if (item != clickedRocket && item is RocketItem rocket)
                {
                    rocket.transform.DOMove(clickedRocket.transform.position, 0.3f)
                        .SetEase(Ease.InBack);
                }
            }

            // Wait for movement to complete
            yield return new WaitForSeconds(0.3f);

            // Destroy other rockets
            foreach (BaseGridItem item in rockets)
            {
                if (item != clickedRocket && item is RocketItem)
                {
                    item.DestroyItem();
                }
            }

            // Scale up center rocket
            clickedRocket.transform.position += new Vector3(0, 0, -1f);
            clickedRocket.transform.DOScale(6f, 0.5f).SetEase(Ease.OutCubic);

            // Add shake effect
            clickedRocket.transform.DOShakePosition(0.5f, 0.1f, 30, 45, false, false);

            // Short delay
            yield return new WaitForSeconds(0.3f);

            // Start 3x3 explosion
            StartCoroutine(CreateExplosion(clickedRocket));
        }
        
        /// <summary>
        /// Create explosion effect for rocket combo
        /// </summary>
        private IEnumerator CreateExplosion(RocketItem sourceRocket)
        {
            int centerX = sourceRocket.GridX;
            int centerY = sourceRocket.GridY;

            // Remove center rocket from grid
            gridManager.SetItemAt(centerX, centerY, null);
            sourceRocket.DestroyItem();

            // Increase active rocket parts counter (12 directions)
            activeRocketParts += 12;

            Vector3 centerPos = gridManager.CalculateWorldPosition(centerX, centerY);

            // Create all rocket projectiles and launch them
            LaunchCenterRowProjectiles(centerPos, centerX, centerY);
            LaunchCenterColumnProjectiles(centerPos, centerX, centerY);
            LaunchTopRowProjectiles(centerX, centerY);
            LaunchBottomRowProjectiles(centerX, centerY);
            LaunchLeftColumnProjectiles(centerX, centerY);
            LaunchRightColumnProjectiles(centerX, centerY);

            yield break;
        }

        private void LaunchCenterRowProjectiles(Vector3 centerPos, int centerX, int centerY)
        {
            // Create left and right projectiles for center row
            GameObject centerRightPart = CreateRocketProjectile(
                gridManager.ItemFactory.GetRightProjectilePrefab(),
                centerPos,
                rocketZPosition);

            GameObject centerLeftPart = CreateRocketProjectile(
                gridManager.ItemFactory.GetLeftProjectilePrefab(),
                centerPos,
                rocketZPosition);

            // Launch projectiles
            StartCoroutine(MoveRocketProjectile(centerRightPart, new Vector2Int(1, 0), centerX, centerY));
            StartCoroutine(MoveRocketProjectile(centerLeftPart, new Vector2Int(-1, 0), centerX, centerY));
        }

        private void LaunchCenterColumnProjectiles(Vector3 centerPos, int centerX, int centerY)
        {
            // Create up and down projectiles for center column
            GameObject centerUpPart = CreateRocketProjectile(
                gridManager.ItemFactory.GetUpProjectilePrefab(),
                centerPos,
                rocketZPosition);

            GameObject centerDownPart = CreateRocketProjectile(
                gridManager.ItemFactory.GetDownProjectilePrefab(),
                centerPos,
                rocketZPosition);

            // Launch projectiles
            StartCoroutine(MoveRocketProjectile(centerUpPart, new Vector2Int(0, 1), centerX, centerY));
            StartCoroutine(MoveRocketProjectile(centerDownPart, new Vector2Int(0, -1), centerX, centerY));
        }

        private void LaunchTopRowProjectiles(int centerX, int centerY)
        {
            if (centerY + 1 < gridManager.GridHeight)
            {
                Vector3 topRowPos = gridManager.CalculateWorldPosition(centerX, centerY + 1);
                topRowPos.z = rocketZPosition;
                
                GameObject topLeftPart = CreateRocketProjectile(
                    gridManager.ItemFactory.GetLeftProjectilePrefab(),
                    topRowPos,
                    rocketZPosition);

                GameObject topRightPart = CreateRocketProjectile(
                    gridManager.ItemFactory.GetRightProjectilePrefab(),
                    topRowPos,
                    rocketZPosition);
                    
                StartCoroutine(MoveRocketProjectile(topLeftPart, new Vector2Int(-1, 0), centerX, centerY + 1));
                StartCoroutine(MoveRocketProjectile(topRightPart, new Vector2Int(1, 0), centerX, centerY + 1));
            }
            else
            {
                activeRocketParts -= 2;
            }
        }

        private void LaunchBottomRowProjectiles(int centerX, int centerY)
        {
            if (centerY - 1 >= 0)
            {
                Vector3 bottomRowPos = gridManager.CalculateWorldPosition(centerX, centerY - 1);
                bottomRowPos.z = rocketZPosition;
                
                GameObject bottomLeftPart = CreateRocketProjectile(
                    gridManager.ItemFactory.GetLeftProjectilePrefab(),
                    bottomRowPos,
                    rocketZPosition);

                GameObject bottomRightPart = CreateRocketProjectile(
                    gridManager.ItemFactory.GetRightProjectilePrefab(),
                    bottomRowPos,
                    rocketZPosition);
                    
                StartCoroutine(MoveRocketProjectile(bottomLeftPart, new Vector2Int(-1, 0), centerX, centerY - 1));
                StartCoroutine(MoveRocketProjectile(bottomRightPart, new Vector2Int(1, 0), centerX, centerY - 1));
            }
            else
            {
                activeRocketParts -= 2;
            }
        }

        private void LaunchLeftColumnProjectiles(int centerX, int centerY)
        {
            if (centerX - 1 >= 0)
            {
                Vector3 leftColPos = gridManager.CalculateWorldPosition(centerX - 1, centerY);
                leftColPos.z = rocketZPosition;
                
                GameObject leftColUpPart = CreateRocketProjectile(
                    gridManager.ItemFactory.GetUpProjectilePrefab(),
                    leftColPos,
                    rocketZPosition);

                GameObject leftColDownPart = CreateRocketProjectile(
                    gridManager.ItemFactory.GetDownProjectilePrefab(),
                    leftColPos,
                    rocketZPosition);
                    
                StartCoroutine(MoveRocketProjectile(leftColUpPart, new Vector2Int(0, 1), centerX - 1, centerY));
                StartCoroutine(MoveRocketProjectile(leftColDownPart, new Vector2Int(0, -1), centerX - 1, centerY));
            }
            else
            {
                activeRocketParts -= 2;
            }
        }

        private void LaunchRightColumnProjectiles(int centerX, int centerY)
        {
            if (centerX + 1 < gridManager.GridWidth)
            {
                Vector3 rightColPos = gridManager.CalculateWorldPosition(centerX + 1, centerY);
                rightColPos.z = rocketZPosition;
                
                GameObject rightColUpPart = CreateRocketProjectile(
                    gridManager.ItemFactory.GetUpProjectilePrefab(),
                    rightColPos,
                    rocketZPosition);

                GameObject rightColDownPart = CreateRocketProjectile(
                    gridManager.ItemFactory.GetDownProjectilePrefab(),
                    rightColPos,
                    rocketZPosition);
                    
                StartCoroutine(MoveRocketProjectile(rightColUpPart, new Vector2Int(0, 1), centerX + 1, centerY));
                StartCoroutine(MoveRocketProjectile(rightColDownPart, new Vector2Int(0, -1), centerX + 1, centerY));
            }
            else
            {
                activeRocketParts -= 2;
            }
        }
        
        /// <summary>
        /// Create a rocket projectile GameObject
        /// </summary>
        private GameObject CreateRocketProjectile(GameObject prefab, Vector3 position, float zPosition)
        {
            Vector3 adjustedPosition = position;
            adjustedPosition.z = zPosition;

            GameObject projectile = Instantiate(prefab, adjustedPosition, Quaternion.identity);
            projectile.transform.SetParent(gridManager.GridContainer);

            return projectile;
        }
        
        /// <summary>
        /// Move a rocket projectile and handle grid interactions
        /// </summary>
        private IEnumerator MoveRocketProjectile(GameObject rocketPart, Vector2Int direction, int currentX, int currentY)
        {
            bool continueMoving = true;
            float rocketZPosition = rocketPart.transform.position.z;
            bool exitedGrid = false;
            int remainingExtraMoves = extraMovesAfterExit;

            while (continueMoving)
            {
                // Calculate next position
                int nextX = currentX + direction.x;
                int nextY = currentY + direction.y;

                // Check grid bounds
                bool isOutOfGrid = nextX < 0 || nextX >= gridManager.GridWidth || nextY < 0 || nextY >= gridManager.GridHeight;

                // Track grid exit
                if (isOutOfGrid && !exitedGrid)
                {
                    exitedGrid = true;
                }

                // Handle movement after exiting grid
                if (exitedGrid)
                {
                    if (remainingExtraMoves <= 0)
                    {
                        continueMoving = false;
                        break;
                    }

                    remainingExtraMoves--;
                }

                // Calculate target position
                Vector3 targetPosition = gridManager.CalculateWorldPosition(nextX, nextY);
                targetPosition.z = rocketZPosition;

                // Handle grid item interaction
                if (!isOutOfGrid)
                {
                    ProcessRocketImpact(nextX, nextY);
                }

                // Move the projectile
                rocketPart.transform.DOMove(targetPosition, moveTime).SetEase(Ease.Linear);

                // Wait for movement to complete
                yield return cachedMoveWait;

                // Update position
                currentX = nextX;
                currentY = nextY;
            }

            // Reduce active parts count
            activeRocketParts--;
            
            // Check if all rocket parts have completed
            if (activeRocketParts <= 0)
            {
                ClearRocketEffectColumns();
                gridManager.CheckGameState();
                StartCoroutine(gridManager.ApplyGravity());
            }

            // Destroy rocket part with animation
            rocketPart.transform.DOScale(0f, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => { Destroy(rocketPart); });
        }

        /// <summary>
        /// Process impact of a rocket projectile at a grid position
        /// </summary>
        private void ProcessRocketImpact(int x, int y)
        {
            BaseGridItem targetItem = gridManager.GetItemAt(x, y);
            
            if (targetItem == null)
                return;
                
            if (targetItem is ObstacleItem obstacle)
            {
                // ObstacleService kullanarak engele hasar ver
                if (obstacleService != null)
                {
                    // ObstacleService üzerinden işlem yap, fromRocket=true
                    obstacleService.DamageObstacle(x, y, true);
                }
                else
                {
                    // ObstacleService bulunamazsa direct işle
                    GridItemType obstacleType = obstacle.ItemType;
                    bool destroyed = obstacle.TakeDamage(1);

                    if (destroyed)
                    {
                        gridManager.SetItemAt(x, y, null);
                        DecrementObstacleFallback(obstacleType);
                    }
                }
            }
            else if (targetItem is RocketItem rocket)
            {
                gridManager.SetItemAt(x, y, null);
                bool isHorizontal = rocket.ItemType == GridItemType.HorizontalRocket;
                rocket.DestroyItem();
                CreateAndLaunchRocketParts(isHorizontal, x, y);
            }
            else
            {
                // Regular item - destroy it
                gridManager.SetItemAt(x, y, null);
                
                targetItem.transform.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        targetItem.transform.DOScale(0f, 0.1f).SetEase(Ease.InQuad)
                            .OnComplete(() => { targetItem.DestroyItem(); });
                    });
            }
        }
        #endregion

        #region Helper Methods for GridManager Interaction
        /// <summary>
        /// Helper method to decrement moves through GridManager
        /// </summary>
        private void DecrementMoves()
        {
            GameManager.Instance.OnMoveMade(--gridManager.RemainingMoves);
        }
        
        
        /// <summary>
        /// Fallback method for decrementing obstacle count when ObstacleService is not available
        /// </summary>
        private void DecrementObstacleFallback(GridItemType obstacleType)
        {
            Debug.LogWarning("RocketService: Using fallback obstacle counting because ObstacleService is null");
            
            if (fallbackObstacleCounts.ContainsKey(obstacleType))
            {
                fallbackObstacleCounts[obstacleType]--;
                
                // Notify GameManager
                GameManager.Instance.OnObstacleDestroyed(obstacleType, fallbackObstacleCounts[obstacleType]);
                
                // Update UI
                cachedUIManager.UpdateObstacleCount(obstacleType, fallbackObstacleCounts[obstacleType]);
            }
        }
        #endregion
    }
}