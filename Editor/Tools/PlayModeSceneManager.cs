using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class PlayModeSceneManager
{
    private const string START_SCENE_KEY = "PlayModeStartScene";
    private const string PREVIOUS_SCENE_KEY = "PlayModePreviousScene";

    static PlayModeSceneManager()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.ExitingEditMode:
                // Store current scene path before entering play mode
                string currentScene = EditorSceneManager.GetActiveScene().path;
                EditorPrefs.SetString(PREVIOUS_SCENE_KEY, currentScene);

                // Switch to start scene if one is set
                string startScene = EditorPrefs.GetString(START_SCENE_KEY, "");
                if (!string.IsNullOrEmpty(startScene) && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(startScene);
                }
                break;

            case PlayModeStateChange.EnteredEditMode:
                // Return to previous scene after exiting play mode
                string previousScene = EditorPrefs.GetString(PREVIOUS_SCENE_KEY, "");
                if (!string.IsNullOrEmpty(previousScene))
                {
                    EditorSceneManager.OpenScene(previousScene);
                }
                break;
        }
    }

    internal static void SetStartScene()
    {
        string scenePath = EditorSceneManager.GetActiveScene().path;
        EditorPrefs.SetString(START_SCENE_KEY, scenePath);
        DebugLogger.Instance.Log($"Start scene set to: {scenePath}");
    }
}