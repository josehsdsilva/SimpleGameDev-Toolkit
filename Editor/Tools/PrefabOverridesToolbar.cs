namespace SimpleGameDev.Editor
{
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

[InitializeOnLoad]
internal static class PrefabOverridesToolbar
{
    const string ENABLED_KEY = "SimpleGameDev_PrefabOverridesIndicatorEnabled";
    const double REFRESH_THROTTLE_SECONDS = 5.0;

    static int cachedCount = -1;
    static bool dirty = true;
    static double nextAllowedRefresh;

    internal static bool IsEnabled => EditorPrefs.GetBool(ENABLED_KEY, true);

    static PrefabOverridesToolbar()
    {
        EditorSceneManager.sceneOpened += (_, __) => MarkDirty();
        EditorSceneManager.sceneSaved += _ => MarkDirty();
        EditorSceneManager.sceneClosed += _ => MarkDirty();
        PrefabUtility.prefabInstanceUpdated += _ => MarkDirty();
        EditorApplication.update += OnEditorUpdate;
    }

    internal static void SetEnabled(bool enabled)
    {
        EditorPrefs.SetBool(ENABLED_KEY, enabled);
        MarkDirty();
    }

    internal static void MarkDirty()
    {
        dirty = true;
    }

    static void OnEditorUpdate()
    {
        if (!dirty || !IsEnabled)
        {
            return;
        }
        double now = EditorApplication.timeSinceStartup;
        if (now < nextAllowedRefresh)
        {
            return;
        }
        nextAllowedRefresh = now + REFRESH_THROTTLE_SECONDS;
        dirty = false;
        cachedCount = PrefabOverridesScanner.CountOverrides();
    }

    [MainToolbarElement("PrefabOverrides/Indicator", defaultDockPosition = MainToolbarDockPosition.Middle)]
    public static MainToolbarElement IndicatorLabel()
    {
        if (!IsEnabled)
        {
            return new MainToolbarLabel(new MainToolbarContent("", ""));
        }

        string text;
        string tooltip;
        if (cachedCount < 0)
        {
            text = "\u25CB prefabs";
            tooltip = "Prefab overrides: scanning...";
        }
        else if (cachedCount == 0)
        {
            text = "\u2713 prefabs";
            tooltip = "No prefab overrides in active scene";
        }
        else
        {
            text = $"\u26A0 {cachedCount} overrides";
            tooltip = $"{cachedCount} prefab override(s) in active scene.\nOpen Tools/Simple Game Dev/Prefab Overrides Scanner to review.";
        }

        return new MainToolbarLabel(new MainToolbarContent(text, tooltip));
    }
}

}
