using Data;
using Grid;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace Core 
{
    /// <summary>
    /// Manages the overall game state, level flow, and win/lose conditions
    /// </summary>
    public class GameManager : MonoBehaviour 
    {
        #region Singleton
        private static GameManager _instance;
        
        /// <summary>
        /// Singleton instance of GameManager
        /// </summary>
        public static GameManager Instance => _instance;
        
        private void Awake() 
        {
            // Enforce singleton pattern
            if (_instance != null && _instance != this) 
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            // Find required component references
            InitializeReferences();
        }
        #endregion
        
        #region Inspector Variables
        [SerializeField] private GridManager gridManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private ParticleManager particleManager;
        
        [Header("Scene Settings")]
        [SerializeField] private string mainSceneName = "MainScene";
        [SerializeField] private string levelSceneName = "LevelScene";
        
        [Header("Celebration Effects")]
        [SerializeField] private ParticleSystem winParticles;
        [SerializeField] private float celebrationDuration = 2f;
        #endregion
        
        #region Private Variables
        private int currentLevelNumber;
        private int currentMoves;
        private bool isGameOver = false;
        #endregion
        
        #region Properties
        /// <summary>
        /// Whether the game is over (won or lost)
        /// </summary>
        public bool IsGameOver => isGameOver;
        #endregion
        
        #region Initialization
        /// <summary>
        /// Find required component references if not set
        /// </summary>
        private void InitializeReferences() 
        {
            if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
            if (levelManager == null) levelManager = FindFirstObjectByType<LevelManager>();
            if (particleManager == null) particleManager = FindFirstObjectByType<ParticleManager>();
        }
        #endregion
        
        #region Level Management
        /// <summary>
        /// Start a level with the given level number
        /// </summary>
        /// <param name="levelNumber">The level number to start</param>
        public void StartLevel(int levelNumber) 
        {
            currentLevelNumber = levelNumber;
            isGameOver = false;
            
            // Get level data
            LevelDataJson levelData = levelManager.GetLevelData(levelNumber);
            if (levelData == null) 
            {
                Debug.LogError($"Level data not found for level {levelNumber}");
                return;
            }
            
            // Initialize grid
            if (gridManager != null) 
            {
                gridManager.InitializeGrid(levelData);
                currentMoves = gridManager.RemainingMoves;
                Debug.Log($"Level {levelNumber} started with {currentMoves} moves");
            }
            
            // Update UI
            if (uiManager != null) 
            {
                uiManager.UpdateUI(levelData);
                uiManager.UpdateMoveCounter(currentMoves);
            }
        }
        
        /// <summary>
        /// Called when a move is made
        /// </summary>
        /// <param name="movesLeft">Number of moves remaining</param>
        public void OnMoveMade(int movesLeft) 
        {
            currentMoves = movesLeft;
            
            // Update UI
            if (uiManager != null) 
            {
                uiManager.UpdateMoveCounter(currentMoves);
            }
            
            // Check game over conditions
            CheckGameState();
        }
        
        /// <summary>
        /// Called when an obstacle is destroyed
        /// </summary>
        /// <param name="obstacleType">Type of obstacle destroyed</param>
        /// <param name="remainingObstacleCount">Number of this obstacle type remaining</param>
        public void OnObstacleDestroyed(GridItemType obstacleType, int remainingObstacleCount) 
        {
            // Update UI to reflect the obstacle count change
            if (uiManager != null)
            {
                uiManager.UpdateObstacleCount(obstacleType, remainingObstacleCount);
            }
        }
        
        /// <summary>
        /// Called when all obstacles are destroyed
        /// </summary>
        public void OnAllObstaclesDestroyed() 
        {
            if (!isGameOver) 
            {
                Debug.Log("All obstacles destroyed! Level complete.");
                isGameOver = true;
                WinLevel();
            }
        }
        #endregion
        
        #region Game State
        /// <summary>
        /// Check if the game has been won or lost
        /// </summary>
        private void CheckGameState() 
        {
            if (isGameOver) return;
            
            // Lose condition - out of moves
            if (currentMoves <= 0) 
            {
                isGameOver = true;
                LoseLevel();
                return;
            }
        }
        public void CheckLoseCondition()
        {
            if (currentMoves <= 0) 
            {
                LoseLevel();
            }
        }
        
        /// <summary>
        /// Handle level completion
        /// </summary>
        public void WinLevel() 
        {
            Debug.Log($"Level {currentLevelNumber} completed!");
            
            // Play celebration effects
            if (winParticles != null) 
            {
                winParticles.gameObject.SetActive(true);
                winParticles.Play();
            }
            
            // Update level progress
            if (levelManager != null) 
            {
                levelManager.CompleteLevel();
            }
            
            // Show win UI
            if (uiManager != null) 
            {
                uiManager.ShowWinPanel();
            }
            
            // Return to main menu after celebration
            StartCoroutine(ReturnToMainMenuAfterDelay(celebrationDuration));
        }
        
        /// <summary>
        /// Handle level failure
        /// </summary>
        public void LoseLevel() 
        {
            Debug.Log($"Failed Level {currentLevelNumber}");
            
            // Show lose UI
            if (uiManager != null) 
            {
                uiManager.ShowLosePanel();
            }
        }
        #endregion
        
        #region Navigation
        /// <summary>
        /// Retry the current level
        /// </summary>
        public void RetryLevel() 
        {
            StartLevel(currentLevelNumber);
        }
        
        /// <summary>
        /// Go to the main menu scene
        /// </summary>
        public void GoToMainMenu() 
        {
            SceneManager.LoadScene(mainSceneName);
        }
        
        /// <summary>
        /// Go to the next level
        /// </summary>
        public void GoToNextLevel() 
        {
            currentLevelNumber++;
            SceneManager.LoadScene(levelSceneName);
        }
        
        /// <summary>
        /// Return to main menu after a delay
        /// </summary>
        /// <param name="delay">Delay in seconds</param>
        private IEnumerator ReturnToMainMenuAfterDelay(float delay) 
        {
            yield return new WaitForSeconds(delay);
            GoToMainMenu();
        }
        #endregion
    }
}