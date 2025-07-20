using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;


namespace Core
{
    /// <summary>
    /// Manages all UI elements, panels, and user interface interactions in the game
    /// Follows the singleton pattern for global access
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Singleton
        private static UIManager _instance;
        public static UIManager Instance => _instance;
        #endregion

        #region Events
        // UI update events
        public event Action<int> OnMovesUpdated;
        public event Action<GridItemType, int> OnObstacleCountUpdated;
        public event Action OnAllGoalsCompleted;

        // Panel events
        public event Action OnWinPanelShown;
        public event Action OnLosePanelShown;

        // Navigation events
        public event Action OnMainMenuRequested;
        public event Action OnRetryRequested;
        #endregion

        #region Inspector References
        [Header("In-Game UI")]
        [SerializeField] private TextMeshProUGUI moveCounter;
        [SerializeField] private GoalPanel goalPanel;
        [SerializeField] private TextMeshProUGUI levelTitleText;

        [Header("Win Panel")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private Button mainMenuButton;

        [Header("Lose Panel")]
        [SerializeField] private GameObject losePanel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button closeButton;
        #endregion

        #region Unity Lifecycle
        /// <summary>
        /// Initializes the singleton instance
        /// </summary>
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        /// <summary>
        /// Sets up component references and button listeners
        /// </summary>
        private void Start()
        {
            // Initialize references
            if (goalPanel == null)
            {
                Debug.LogWarning("GoalPanel reference missing!");
                goalPanel = FindFirstObjectByType<GoalPanel>();
            }

            // Set up button listeners
            SetupButtonListeners();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Configures button click listeners for UI interactions
        /// </summary>
        private void SetupButtonListeners()
        {
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnMainMenuClicked);
            }
        }
        #endregion

        #region UI Updates
        /// <summary>
        /// Updates all UI elements with data from the current level
        /// </summary>
        public void UpdateUI(LevelDataJson levelData)
        {
            if (levelData == null) return;

            Debug.Log($"Updating UI for Level {levelData.level_number}");

            UpdateLevelTitle(levelData.level_number);
            UpdateMoveCounter(levelData.move_count);
            UpdateGoalPanel(levelData);
        }

        /// <summary>
        /// Updates the level title display
        /// </summary>
        private void UpdateLevelTitle(int levelNumber)
        {
            if (levelTitleText != null)
            {
                levelTitleText.text = $"Level {levelNumber}";
            }
        }

        /// <summary>
        /// Updates the move counter with animation
        /// </summary>
        public void UpdateMoveCounter(int moves)
        {
            if (moveCounter == null) return;

            moveCounter.text = moves.ToString();
            moveCounter.transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 1, 0.5f);

            // Trigger event
            OnMovesUpdated?.Invoke(moves);
        }

        /// <summary>
        /// Updates the goal panel with obstacle counts from level data
        /// </summary>
        private void UpdateGoalPanel(LevelDataJson levelData)
        {
            if (goalPanel == null)
            {
                return;
            }

            Dictionary<GridItemType, int> obstacles = CountObstaclesInLevelData(levelData);
            goalPanel.InitializeGoals(obstacles);
        }

        /// <summary>
        /// Updates the count of a specific obstacle type
        /// </summary>
        public void UpdateObstacleCount(GridItemType obstacleType, int remainingCount)
        {
            if (goalPanel == null) return;
            goalPanel.UpdateObstacleCount(obstacleType, remainingCount);
            
            // Trigger obstacle update event
            OnObstacleCountUpdated?.Invoke(obstacleType, remainingCount);

            if (goalPanel.AreAllGoalsCompleted())
            {
                // Trigger goal completion event
                OnAllGoalsCompleted?.Invoke();

                // Also notify GameManager for backward compatibility
                GameManager.Instance.OnAllObstaclesDestroyed();
            }
        }
        #endregion

        #region Panel Management
        /// <summary>
        /// Displays the win panel with animation
        /// </summary>
        public void ShowWinPanel()
        {
            if (winPanel == null) return;

            ShowPanelWithAnimation(winPanel);

            // Trigger event
            OnWinPanelShown?.Invoke();
        }

        /// <summary>
        /// Displays the lose panel with animation
        /// </summary>
        public void ShowLosePanel()
        {
            if (losePanel == null) return;

            ShowPanelWithAnimation(losePanel);

            // Trigger event
            OnLosePanelShown?.Invoke();
        }

        /// <summary>
        /// Shows a panel with sliding and pulsing animation
        /// </summary>
        /// <param name="panel">The panel GameObject to animate</param>
        private void ShowPanelWithAnimation(GameObject panel)
        {
            panel.SetActive(true);

            // Store the panel's final position
            Vector3 finalPosition = panel.transform.position;
            
            panel.transform.position = new Vector3(
                finalPosition.x,
                finalPosition.y - Screen.height,
                finalPosition.z
            );
    
            // Store the panel's original scale
            Vector3 originalScale = panel.transform.localScale;
    
            // Create a DOTween sequence
            Sequence sequence = DOTween.Sequence();
    
            // First animation: Panel moves up
            sequence.Append(
                panel.transform.DOMove(finalPosition, 0.6f)
                    .SetEase(Ease.OutBack)
            );
            // Start the sequence
            sequence.Play();
        }
        #endregion

        #region Button Handlers
        /// <summary>
        /// Handles the main menu button click event
        /// </summary>
        private void OnMainMenuClicked()
        {
            // Trigger event
            OnMainMenuRequested?.Invoke();

            // Call GameManager for backward compatibility
            GameManager.Instance.GoToMainMenu();
        }

        /// <summary>
        /// Handles the retry button click event
        /// </summary>
        private void OnRetryClicked()
        {
            // Trigger event
            OnRetryRequested?.Invoke();

            // Call GameManager for backward compatibility
            GameManager.Instance.RetryLevel();
            losePanel.SetActive(false);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Counts obstacles in level data by type
        /// </summary>
        /// <param name="levelData">The level data to analyze</param>
        /// <returns>Dictionary mapping obstacle types to their counts</returns>
        private Dictionary<GridItemType, int> CountObstaclesInLevelData(LevelDataJson levelData)
        {
            Dictionary<GridItemType, int> obstacles = new Dictionary<GridItemType, int>
            {
                { GridItemType.Box, 0 },
                { GridItemType.Stone, 0 },
                { GridItemType.Vase, 0 }
            };

            for (int i = 0; i < levelData.grid.Length; i++)
            {
                string itemCode = levelData.grid[i];
                GridItemType itemType = StringToGridItemType(itemCode);

                if (IsObstacleType(itemType))
                {
                    if (obstacles.ContainsKey(itemType))
                    {
                        obstacles[itemType]++;
                    }
                    else
                    {
                        obstacles[itemType] = 1;
                    }
                }
            }

            return obstacles;
        }

        /// <summary>
        /// Converts a string code from level data to a GridItemType
        /// </summary>
        /// <param name="code">The string code from level data</param>
        /// <returns>Corresponding GridItemType</returns>
        private GridItemType StringToGridItemType(string code)
        {
            switch (code.ToLower())
            {
                case "r": return GridItemType.RedCube;
                case "g": return GridItemType.GreenCube;
                case "b": return GridItemType.BlueCube;
                case "y": return GridItemType.YellowCube;
                case "vro": return GridItemType.VerticalRocket;
                case "hro": return GridItemType.HorizontalRocket;
                case "bo": return GridItemType.Box;
                case "s": return GridItemType.Stone;
                case "v": return GridItemType.Vase;
                default: return GridItemType.Empty;
            }
        }

        /// <summary>
        /// Checks if a grid item type is an obstacle
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type is an obstacle</returns>
        private bool IsObstacleType(GridItemType type)
        {
            return type == GridItemType.Box ||
                   type == GridItemType.Stone ||
                   type == GridItemType.Vase;
        }
        #endregion
    }
}