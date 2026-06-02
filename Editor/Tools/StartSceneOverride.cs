namespace SimpleGameDev.Editor
{
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class StartSceneOverride
{
    private const string ENABLED_KEY = "SimpleGameDev_StartSceneEnabled";
    private const string SCENE_PATH_KEY = "SimpleGameDev_StartScenePath";

    internal static bool IsEnabled => EditorPrefs.GetBool(ENABLED_KEY, false);
    internal static string ScenePath => EditorPrefs.GetString(SCENE_PATH_KEY, "");

    static StartSceneOverride()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        ApplyOverride();
    }

    internal static void SetEnabled(bool enabled)
    {
        EditorPrefs.SetBool(ENABLED_KEY, enabled);
        ApplyOverride();
    }

    internal static void SetScenePath(string path)
    {
        EditorPrefs.SetString(SCENE_PATH_KEY, path);
        ApplyOverride();
    }

    private static void ApplyOverride()
    {
        if (IsEnabled && !string.IsNullOrEmpty(ScenePath))
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
            EditorSceneManager.playModeStartScene = sceneAsset;
        }
        else
        {
            EditorSceneManager.playModeStartScene = null;
        }
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            ApplyOverride();
        }
    }

    internal static void DrawGUI()
    {
        EditorGUILayout.BeginHorizontal();

        bool enabled = EditorGUILayout.Toggle("Start Scene Override", IsEnabled);
        if (enabled != IsEnabled)
        {
            SetEnabled(enabled);
        }

        EditorGUI.BeginDisabledGroup(!IsEnabled);

        SceneAsset currentScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        SceneAsset newScene = (SceneAsset)EditorGUILayout.ObjectField(currentScene, typeof(SceneAsset), false);

        if (newScene != currentScene)
        {
            string path = newScene != null ? AssetDatabase.GetAssetPath(newScene) : "";
            SetScenePath(path);
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
    }
}

}
