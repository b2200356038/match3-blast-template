using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core;
using Grid;
using UnityEngine.SceneManagement;

namespace Core {
    /// <summary>
    /// Manages level loading, persistence, and progression
    /// </summary>
    public class LevelManager : MonoBehaviour {
        private static LevelManager _instance;
        public static LevelManager Instance => _instance;
        
        [Header("Level Management")]
        [SerializeField] private List<LevelDataJson> levels = new List<LevelDataJson>();
        [SerializeField] private int maxLevels = 10;
        [SerializeField] private string levelsDirectory = "Levels"; // Folder in Resources
        
        [Header("Scene References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private GameManager gameManager;
        
        [Header("Scene Names")]
        [SerializeField] private string mainSceneName = "MainScene";
        [SerializeField] private string levelSceneName = "LevelScene";
        
        private int currentLevel = 1;
        private const string LEVEL_PREF_KEY = "CurrentLevel";
        private Dictionary<int, TextAsset> levelJsonFiles = new Dictionary<int, TextAsset>();
        
        private void Awake() {
            if (_instance != null && _instance != this) {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureReferences();
            LoadSavedLevel();
            LoadLevelJsonFiles();
        }
        
        private void Start() {

        }
        
        private void OnEnable() {
            // Listen for scene loading events
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        private void OnDisable() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            EnsureReferences();
            if (scene.name == levelSceneName) {
                LoadCurrentLevel();
            }
        }
        
        private void EnsureReferences() {
            // Find references if not set
            if (gridManager == null) gridManager = FindFirstObjectByType<GridManager>();
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
            if (gameManager == null) gameManager = FindFirstObjectByType<GameManager>();
        }
        
        // Load JSON files from Resources folder
        private void LoadLevelJsonFiles() { 
            TextAsset[] jsonFiles = Resources.LoadAll<TextAsset>(levelsDirectory);
            
            levels.Clear();
            levelJsonFiles.Clear();
            
            foreach (TextAsset jsonFile in jsonFiles) {
                try {
                    // Read JSON content
                    string jsonText = jsonFile.text;
                    
                    // Deserialize JSON content
                    LevelDataJson levelData = JsonUtility.FromJson<LevelDataJson>(jsonText);
                    
                    // Add to dictionary by level number
                    levelJsonFiles.Add(levelData.level_number, jsonFile);
                    
                    // Add to levels list
                    levels.Add(levelData);
                    
                    Debug.Log($"Level {levelData.level_number} loaded: {levelData.grid_width}x{levelData.grid_height}");
                }
                catch (System.Exception e) {
                    Debug.LogError($"Error loading JSON file: {jsonFile.name} - {e.Message}");
                }
            }
            
            levels.Sort((a, b) => a.level_number.CompareTo(b.level_number));
            
            maxLevels = Mathf.Max(levels.Count, maxLevels);
            
            Debug.Log($"Total {levels.Count} levels loaded. Maximum level: {maxLevels}");
        }
        
        // Load and start a specific level
        public void LoadLevel(int levelNumber) {
            if (levelNumber > maxLevels) {
                Debug.LogWarning($"Level {levelNumber} is greater than maximum level count ({maxLevels}). All levels may be completed.");
                return;
            }
            
            LevelDataJson levelData = GetLevelData(levelNumber);
            
            if (levelData == null) {
                Debug.LogError($"Data for level {levelNumber} not found!");
                return;
            }
            
            // Start level using GameManager
            if (gameManager != null) {
                gameManager.StartLevel(levelNumber);
            }
            
            Debug.Log($"Level {levelNumber} started.");
        }
        
        // Load current level
        public void LoadCurrentLevel() {
            LoadLevel(currentLevel);
        }
        
        // Return level data for the given level number
        public LevelDataJson GetLevelData(int levelNumber) {
            // First search in levels list
            LevelDataJson levelData = levels.Find(level => level.level_number == levelNumber);
            
            // If not found and level files are loaded, try loading from JSON file
            if (levelData == null && levelJsonFiles.TryGetValue(levelNumber, out TextAsset jsonFile)) {
                try {
                    string jsonText = jsonFile.text;
                    levelData = JsonUtility.FromJson<LevelDataJson>(jsonText);
                }
                catch (System.Exception e) {
                    Debug.LogError($"Error reading JSON data for level {levelNumber}: {e.Message}");
                }
            }
            
            // If still not found, return the first level (fallback)
            if (levelData == null && levels.Count > 0) {
                Debug.LogWarning($"Level {levelNumber} not found, reverting to level 1.");
                levelData = levels[0];
            }
            
            return levelData;
        }
        
        // Return current level data
        public LevelDataJson GetCurrentLevelData() {
            return GetLevelData(currentLevel);
        }
        
        // Complete level and move to next
        public void CompleteLevel() {
            currentLevel++;
            SaveCurrentLevel();
        }
        
        // Set to a specific level
        public void SetLevel(int level) {
            currentLevel = level;
            SaveCurrentLevel();
        }
        
        // Return current level number
        public int GetLevel() {
            return currentLevel;
        }
        
        // Check if all levels are completed
        public bool IsAllLevelsCompleted() {
            return currentLevel > maxLevels;
        }
        
        // Load saved level
        private void LoadSavedLevel() {
            currentLevel = PlayerPrefs.GetInt(LEVEL_PREF_KEY, 1);
        }
        
        // Save current level
        private void SaveCurrentLevel() {
            PlayerPrefs.SetInt(LEVEL_PREF_KEY, currentLevel);
            PlayerPrefs.Save();
        }
        
        // Scene transitions
        public void LoadMainScene() {
            SceneManager.LoadScene(mainSceneName);
        }
        
        public void LoadLevelScene() {
            SceneManager.LoadScene(levelSceneName);
        }
    }
}

[System.Serializable]
public class LevelDataJson
{
    public int level_number;
    public int grid_width;
    public int grid_height;
    public int move_count;
    public string[] grid;
}