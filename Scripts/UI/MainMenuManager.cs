using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Core;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private Button levelButton;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private string levelSceneName = "LevelScene";
    [SerializeField] private string finishedText = "Finished";
    [SerializeField] private ParticleSystem buttonClickEffect;
    
    private const string LEVEL_PREF_KEY = "CurrentLevel";
    private int currentLevel;
    private int maxLevels = 10; // We have 10 levels as mentioned in the requirements
    
    private void Start()
    {
        LoadSavedLevel();
        UpdateLevelButtonText();
        
        // Add click listener to the level button
        if (levelButton != null)
        {
            levelButton.onClick.AddListener(OnLevelButtonClicked);
        }
        else
        {
            Debug.LogError("Level button not assigned in MainMenuManager!");
        }
    }
    
    private void LoadSavedLevel()
    {
        currentLevel = PlayerPrefs.GetInt(LEVEL_PREF_KEY, 1);
    }
    
    private void UpdateLevelButtonText()
    {
        if (levelText != null)
        {
            // Check if all levels are finished
            if (currentLevel > maxLevels)
            {
                levelText.text = finishedText;
            }
            else
            {
                levelText.text = "Level " + currentLevel;
            }
        }
        else
        {
            Debug.LogError("Level text not assigned in MainMenuManager!");
        }
    }
    
    private void OnLevelButtonClicked()
    {
        // If all levels are finished, we don't load anything
        if (currentLevel > maxLevels)
        {
            Debug.Log("All levels are finished!");
            return;
        }
        StartCoroutine(LoadLevelWithDelay(0.2f));
    }
    
    private IEnumerator LoadLevelWithDelay(float delay)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);
        
        // Load the level scene
        SceneManager.LoadScene(levelSceneName);
    }
}