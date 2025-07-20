using UnityEngine;
using UnityEditor;

public class LevelEditorMenu : EditorWindow
{
    private int levelNumber = 1;
    private const string LEVEL_PREF_KEY = "CurrentLevel";
    private const int MAX_LEVELS = 10;

    [MenuItem("Dream Games/Set Last Played Level")]
    public static void ShowWindow()
    {
        GetWindow<LevelEditorMenu>("Set Level");
    }

    private void OnGUI()
    {
        GUILayout.Label("Set Last Played Level", EditorStyles.boldLabel);

        // Get current level from PlayerPrefs
        int currentLevel = PlayerPrefs.GetInt(LEVEL_PREF_KEY, 1);
        GUILayout.Label($"Current Level: {currentLevel}", EditorStyles.label);

        // Level number input field
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("New Level Number:", GUILayout.Width(150));
        levelNumber = EditorGUILayout.IntSlider(levelNumber, 1, MAX_LEVELS + 1);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("Note: Setting to level " + (MAX_LEVELS + 1) + " will show 'Finished' text", EditorStyles.miniLabel);
        GUILayout.Space(10);

        // Set button
        if (GUILayout.Button("Set Level"))
        {
            PlayerPrefs.SetInt(LEVEL_PREF_KEY, levelNumber);
            PlayerPrefs.Save();
            Debug.Log($"Last played level set to {levelNumber}");
        }

        // Reset button
        if (GUILayout.Button("Reset to Level 1"))
        {
            PlayerPrefs.SetInt(LEVEL_PREF_KEY, 1);
            PlayerPrefs.Save();
            levelNumber = 1;
            Debug.Log("Last played level reset to 1");
        }
    }
}