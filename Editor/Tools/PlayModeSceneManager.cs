namespace SimpleGameDev.Editor
{
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class PlayModeSceneManager
{
    private const string START_SCENE_KEY = "PlayModeStartScene";
    private const string PREVIOUS_SCENE_KEY = "PlayModePreviousScene";

    // Opt-in: save dirty scenes without the "save?" modal before switching to the start scene.
    // A modal stalls automated drivers (editor bridges, MCP servers). Any one of these turns it on:
    // EditorPrefs (per machine, the Hub toggle), SessionState (until the editor closes, for a driver
    // to set at runtime) or the SGD_AUTOSAVE_ON_PLAY=1 environment variable (set before launch).
    internal const string AUTO_SAVE_KEY = "SGD.AutoSaveOnPlay";
    private const string AUTO_SAVE_ENV = "SGD_AUTOSAVE_ON_PLAY";

    internal static bool AutoSaveOnPlayPref => EditorPrefs.GetBool(AUTO_SAVE_KEY, false);
    internal static void SetAutoSaveOnPlayPref(bool enabled) => EditorPrefs.SetBool(AUTO_SAVE_KEY, enabled);

    internal static bool IsAutoSaveOnPlay
    {
        get
        {
            if (AutoSaveOnPlayPref || SessionState.GetBool(AUTO_SAVE_KEY, false)) return true;
            string env = System.Environment.GetEnvironmentVariable(AUTO_SAVE_ENV);
            return env == "1" || string.Equals(env, "true", System.StringComparison.OrdinalIgnoreCase);
        }
    }

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
                if (!string.IsNullOrEmpty(startScene) && SaveModifiedScenes())
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

    // True when it is safe to leave the open scenes. Without the opt-in this is Unity's prompt.
    // With it, dirty scenes are saved silently; an untitled dirty scene cannot be saved without a
    // file dialog, so the switch is skipped instead (play continues in the current scene).
    private static bool SaveModifiedScenes()
    {
        if (!IsAutoSaveOnPlay) return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            Scene scene = EditorSceneManager.GetSceneAt(i);
            if (!scene.isDirty) continue;
            if (string.IsNullOrEmpty(scene.path))
            {
                Debug.LogWarning("PlayModeSceneManager: an untitled scene has unsaved changes, so the start scene was not opened (auto-save on play is on and cannot save it without a dialog).");
                return false;
            }
            if (!EditorSceneManager.SaveScene(scene)) return false;
        }
        return true;
    }

    internal static void SetStartScene()
    {
        string scenePath = EditorSceneManager.GetActiveScene().path;
        EditorPrefs.SetString(START_SCENE_KEY, scenePath);
        DebugLogger.Instance.Log($"Start scene set to: {scenePath}");
    }
}
}
