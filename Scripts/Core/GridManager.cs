using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Grid.Items;
using Grid.Items.Cubes;
using Grid.Items.Obstacles;
using Grid.Items.Rockets;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Manages the game grid including item placement, interactions, and grid logic
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        #region Inspector Variables

        [SerializeField] private ItemFactory itemFactory;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Transform gridContainer;
        [SerializeField] private RocketService rocketService;
        [SerializeField] private ObstacleService obstacleService;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private float extraPadding = 1f;
        [SerializeField] private SpriteRenderer maskAreaRenderer;
        [SerializeField] private float stableGridConfirmDelay = 0.1f;

        #endregion

        #region Private Variables

        // Grid data
        private BaseGridItem[,] grid;
        private int gridWidth;
        private int gridHeight;
        private int remainingMoves;

        // Utility objects
        private PhysicsService physicsService;
        private GridMatcher gridMatcher;
        
        [SerializeField] private int fallingItemsCount = 0;
        private Dictionary<int, List<BaseGridItem>> cachedMatches = new Dictionary<int, List<BaseGridItem>>();

        // Counters
        private int matchCounter;
        private Coroutine recheckCoroutine;

        #endregion

        #region Properties

        public BaseGridItem[,] Grid => grid;
        public ItemFactory ItemFactory => itemFactory;
        public Transform GridContainer => gridContainer;
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;

        public int RemainingMoves
        {
            get => remainingMoves;
            set => remainingMoves = value;
        }

        public int RemainingObstacles => obstacleService?.GetRemainingObstacles() ?? 0;

        #endregion

        #region Initialization

        private void Awake()
        {
            DOTween.SetTweensCapacity(500, 100);
            
            // Initialize helper classes
            physicsService = new PhysicsService(this, cellSize);
            gridMatcher = new GridMatcher(this);

            // Ensure RocketService is available
            if (rocketService == null)
            {
                rocketService = GetComponent<RocketService>();
            }

            // Ensure ObstacleService is available
            if (obstacleService == null)
            {
                obstacleService = GetComponent<ObstacleService>();
            }

            if (obstacleService == null)
            {
                obstacleService = gameObject.AddComponent<ObstacleService>();
            }

            if (rocketService == null)
            {
                rocketService = gameObject.AddComponent<RocketService>();
            }
        }

        /// <summary>
        /// Initializes the grid with data from the given level
        /// </summary>
        public void InitializeGrid(LevelDataJson levelData)
        {
            ClearGrid();
            // Set grid dimensions
            gridWidth = levelData.grid_width;
            gridHeight = levelData.grid_height;
            remainingMoves = levelData.move_count;
            
            grid = new BaseGridItem[gridWidth, gridHeight];
            obstacleService.InitializeObstacleCounts();
            CreateItemsFromLevelData(levelData);
            AdjustSlicedBackground();
            CheckMatches();
        }

        /// <summary>
        /// Clear the grid and remove all items
        /// </summary>
        private void ClearGrid()
        {
            if (grid == null)
                return;

            // Destroy all grid items
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    BaseGridItem item = grid[x, y];
                    if (item != null)
                    {
                        Destroy(item.gameObject);
                        grid[x, y] = null;
                    }
                }
            }
            
            if (rocketService != null)
            {
                rocketService.ClearRocketEffectColumns();
            }
            if (obstacleService != null)
            {
                obstacleService.InitializeObstacleCounts();
            }

            cachedMatches.Clear();
            matchCounter = 0;
            fallingItemsCount = 0;

            Debug.Log("Grid cleared");
        }

        /// <summary>
        /// Create grid items from the level data
        /// </summary>
        private void CreateItemsFromLevelData(LevelDataJson levelData)
        {
            Vector3 gridOffset = CalculateGridOffset();
            for (int i = 0; i < levelData.grid.Length; i++)
            {
                if (i >= gridWidth * gridHeight)
                {
                    Debug.LogWarning("Extra elements in level data, ignoring extras");
                    break;
                }
                int x = i % gridWidth;
                int y = i / gridWidth;
                string itemCode = levelData.grid[i];
                if (string.IsNullOrEmpty(itemCode))
                {
                    continue;
                }

                Vector3 worldPosition = CalculateItemPosition(x, y, gridOffset);

                if (itemCode.ToLower() == "rand")
                {
                    CreateRandomItemAt(x, y, worldPosition);
                }
                else
                {
                    GridItemType itemType = GridItemHelper.StringToGridItemType(itemCode);
                    CreateItemAt(itemType, x, y, worldPosition);
                }
            }

            Debug.Log(
                $"Grid initialized: {gridWidth}x{gridHeight}, Obstacles: {RemainingObstacles}, Moves: {remainingMoves}");
        }

        /// <summary>
        /// Calculate the grid offset from center
        /// </summary>
        private Vector3 CalculateGridOffset()
        {
            return new Vector3(
                -(gridWidth * cellSize) / 2 + cellSize / 2,
                -(gridHeight * cellSize) / 2 + cellSize / 2,
                0
            );
        }

        /// <summary>
        /// Calculate position for an item at given grid coordinates
        /// </summary>
        private Vector3 CalculateItemPosition(int x, int y, Vector3 gridOffset)
        {
            return new Vector3(
                gridOffset.x + x * cellSize,
                gridOffset.y + y * cellSize,
                -0.01f * y 
            );
        }

        /// <summary>
        /// Adjust background size to fit grid
        /// </summary>
        public void AdjustSlicedBackground()
        {
            if (backgroundRenderer == null)
            {
                Debug.LogWarning("Background renderer not assigned!");
                return;
            }
            
            float gridWorldWidth = gridWidth * cellSize;
            float gridWorldHeight = gridHeight * cellSize;
            
            backgroundRenderer.size = new Vector2(
                gridWorldWidth + extraPadding,
                gridWorldHeight + extraPadding
            );

            // Center background with grid
            Vector3 gridCenter = new Vector3(
                transform.position.x,
                transform.position.y,
                1f 
            );
            backgroundRenderer.transform.position = gridCenter;

            // Adjust mask area if it exists
            if (maskAreaRenderer != null)
            {
                maskAreaRenderer.size = new Vector2(gridWorldWidth, gridWorldHeight);
                maskAreaRenderer.transform.position = gridCenter;
            }

            // Center grid container
            if (gridContainer != null)
            {
                gridContainer.position = new Vector3(
                    transform.position.x,
                    transform.position.y,
                    0f
                );
            }
        }

        #endregion

        #region Item Creation and Placement

        /// <summary>
        /// Create a specific item type at grid coordinates
        /// </summary>
        private void CreateItemAt(GridItemType type, int x, int y, Vector3 worldPosition)
        {
            if (type == GridItemType.Empty) return;

            BaseGridItem item = itemFactory.CreateGridItem(type, x, y, gridContainer);

            if (item != null)
            {
                grid[x, y] = item;
                item.SetPosition(worldPosition);
                
                if (GridItemHelper.IsObstacle(type))
                {
                    obstacleService.IncrementObstacle(type);
                }
            }
        }

        /// <summary>
        /// Create a random cube item at grid coordinates
        /// </summary>
        private void CreateRandomItemAt(int x, int y, Vector3 worldPosition)
        {
            BaseGridItem item = itemFactory.CreateRandomCube(x, y, gridContainer);

            if (item != null)
            {
                grid[x, y] = item;
                item.SetPosition(worldPosition);
            }
        }

        #endregion

        #region Grid Operations

        /// <summary>
        /// Handle when a grid item is clicked
        /// </summary>
        public void OnGridItemClicked(BaseGridItem item)
        {
            // Check if game is over
            if (GameManager.Instance.IsGameOver)
            {
                Debug.Log("Game is over, ignoring clicks");
                return;
            }

            if (item == null) return;
            
            if (item is RocketItem rocket)
            {
                rocketService.ProcessRocketClick(rocket);
                return;
            }

            // Check for cached matches first
            List<BaseGridItem> matchesFromCache = FindMatchInCache(item);
            
            if (matchesFromCache != null && matchesFromCache.Count >= 2&& fallingItemsCount==0)
            {
                // Move clicked item to front of list
                matchesFromCache.Remove(item);
                matchesFromCache.Insert(0, item);

                ProcessMatches(matchesFromCache);
                return;
            }

            // Find matches using matcher
            List<BaseGridItem> matches = gridMatcher.FindMatchingNeighbors(item.GridX, item.GridY);
            
            if (matches.Count >= 2)
            {
                ProcessMatches(matches);
                CheckMatches();
            }
        }

        /// <summary>
        /// Find a match containing the given item in the cache
        /// </summary>
        private List<BaseGridItem> FindMatchInCache(BaseGridItem item)
        {
            foreach (var kvp in cachedMatches)
            {
                foreach (BaseGridItem matchItem in kvp.Value)
                {
                    if (matchItem == item)
                    {
                        // Check if all items in match are still valid
                        bool allValid = true;
                        foreach (BaseGridItem cacheItem in kvp.Value)
                        {
                            if (cacheItem == null || cacheItem.IsMoving)
                            {
                                allValid = false;
                                break;
                            }
                        }

                        if (allValid)
                        {
                            return kvp.Value;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Process a collection of matched items
        /// </summary>
        public void ProcessMatches(List<BaseGridItem> matches)
        {
            // Decrement moves and inform GameManager
            remainingMoves--;
            GameManager.Instance.OnMoveMade(remainingMoves);

            BaseGridItem clickedItem = matches[0];
            bool createRocket = matches.Count >= 4;

            int rocketX = clickedItem.GridX;
            int rocketY = clickedItem.GridY;
            obstacleService.ClearDamagedObstacles();

            // Process matches and damage adjacent obstacles
            foreach (BaseGridItem item in matches)
            {
                item.DisableItem();
                obstacleService.DamageAdjacentObstacles(item.GridX, item.GridY);

                // Destroy matches if not creating rocket
                if (!createRocket)
                {
                    grid[item.GridX, item.GridY] = null;
                    item.DestroyItem();
                }
            }
            
            if (createRocket)
            {
                StartCoroutine(rocketService.CreateRocketWithAnimation(matches, rocketX, rocketY));
            }
            else
            {
                StartCoroutine(physicsService.ApplyGravity());
            }
        }

        /// <summary>
        /// Check for matches after gravity has been applied
        /// </summary>
        public void CheckMatches()
        {
            Debug.Log("Checking for matches after gravity");

            cachedMatches.Clear();
            matchCounter = 0;

            // Track visited cells to avoid duplicate processing
            bool[][] visitedCells = new bool[gridWidth][];
            for (int index = 0; index < gridWidth; index++)
            {
                visitedCells[index] = new bool[gridHeight];
            }

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    // Skip already checked cells
                    if (visitedCells[x][y]) continue;

                    BaseGridItem currentItem = grid[x, y];
                    
                    if (currentItem == null || !(currentItem is CubeItem) || currentItem.IsMoving)
                    {
                        continue;
                    }

                    // Find matching neighbors
                    List<BaseGridItem> matches = gridMatcher.FindMatchingNeighbors(x, y);
                    
                    if (matches.Count >= 2)
                    {
                        foreach (BaseGridItem item in matches)
                        {
                            visitedCells[item.GridX][item.GridY] = true;
                        }

                        // Add match to cache
                        cachedMatches[matchCounter] = matches;
                        matchCounter++;
                    }
                    ShowRocketHint(matches, matches.Count >= 4);
                }
            }
        }

        /// <summary>
        /// Show or hide rocket hint on matched cubes
        /// </summary>
        private void ShowRocketHint(List<BaseGridItem> items, bool isHint)
        {
            foreach (BaseGridItem item in items)
            {
                if (item is CubeItem cubeItem)
                {
                    cubeItem.ShowHint(isHint);
                }
            }
        }

        #endregion

        #region Falling Item Management

        /// <summary>
        /// Add an item to the falling items count
        /// </summary>
        public void AddFallingItem()
        {
            fallingItemsCount++;
        }

        /// <summary>
        /// Remove an item from the falling items count
        /// </summary>
        public void RemoveFallingItem()
        {
            fallingItemsCount--;
            
            // Safety clamp - prevent negative
            if (fallingItemsCount < 0)
            {
                fallingItemsCount = 0;
                Debug.LogWarning("Falling items count went negative, resetting to 0");
            }

            // Trigger match check when all items stopped
            if (fallingItemsCount == 0)
            {
                if (recheckCoroutine != null)
                {
                    StopCoroutine(recheckCoroutine);
                }
                recheckCoroutine = StartCoroutine(DelayedRecheckAndMatch());
            }
        }

        private IEnumerator DelayedRecheckAndMatch()
        {
            yield return new WaitForSeconds(stableGridConfirmDelay);
            
            // Double check with counter
            if (fallingItemsCount == 0)
            {
                CheckMatches();
                CheckGameState();
            }
            recheckCoroutine = null;
        }

        /// <summary>
        /// Apply gravity to the grid
        /// </summary>
        public IEnumerator ApplyGravity()
        {
            return physicsService.ApplyGravity();
        }

        #endregion

        #region Game State

        /// <summary>
        /// Check if the game has been won or lost
        /// </summary>
        public void CheckGameState()
        {
            // If no obstacles remain, player wins
            if (obstacleService.GetRemainingObstacles() <= 0)
            {
                GameManager.Instance.OnAllObstaclesDestroyed();
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Calculate the world position for grid coordinates
        /// </summary>
        public Vector3 CalculateWorldPosition(int x, int y)
        {
            Vector3 gridOffset = new Vector3(
                -(gridWidth * cellSize) / 2 + cellSize / 2,
                -(gridHeight * cellSize) / 2 + cellSize / 2,
                0);
            
            return new Vector3(
                gridOffset.x + x * cellSize,
                gridOffset.y + y * cellSize,
                0);
        }

        /// <summary>
        /// Get the item at grid coordinates
        /// </summary>
        public BaseGridItem GetItemAt(int x, int y)
        {
            if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            {
                return grid[x, y];
            }

            return null;
        }

        /// <summary>
        /// Set the item at grid coordinates
        /// </summary>
        public void SetItemAt(int x, int y, BaseGridItem item)
        {
            if (x >= 0 && x < gridWidth && y >= 0 && y < gridHeight)
            {
                grid[x, y] = item;

                if (item != null)
                {
                    item.SetGridPosition(x, y);
                }
            }
        }

        /// <summary>
        /// Check if a column has a rocket effect (delegates to RocketService)
        /// </summary>
        public bool HasRocketEffectInColumn(int columnX)
        {
            if (rocketService != null)
            {
                return rocketService.HasRocketEffectInColumn(columnX);
            }

            return false;
        }
        private void OnDestroy() 
        {
            DOTween.KillAll();
        }

        private void OnApplicationPause(bool pauseStatus) 
        {
            if (pauseStatus)
            {
                DOTween.PauseAll();
            }
            else
            {
                DOTween.PlayAll();
            }
        }

        #endregion
    }
}