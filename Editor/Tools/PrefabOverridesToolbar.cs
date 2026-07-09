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

    // Quiet period after the last change before rescanning. Dragging an inspector field fires
    // prefabInstanceUpdated every frame, so the scan must wait for the user to stop — throttling
    // to "at most once every N seconds" still pays the full scan cost mid-drag.
    const double DEBOUNCE_SECONDS = 1.5;

    // A scan slower than this makes the editor feel broken. Cross it and the indicator drops to
    // manual refresh rather than stalling the main thread on every edit.
    const double AUTO_SCAN_BUDGET_SECONDS = 0.25;

    static int cachedCount = -1;
    static bool dirty = true;
    static bool autoScanDisabled;
    static double scanNotBefore;

    internal static bool IsEnabled => EditorPrefs.GetBool(ENABLED_KEY, true);

    static PrefabOverridesToolbar()
    {
        // A different scene deserves a fresh verdict on whether it can be scanned cheaply.
        EditorSceneManager.sceneOpened += (_, __) => ForceRefresh();
        EditorSceneManager.sceneClosed += _ => ForceRefresh();
        EditorSceneManager.sceneSaved += _ => MarkDirty();
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
        scanNotBefore = EditorApplication.timeSinceStartup + DEBOUNCE_SECONDS;
    }

    /// Re-enables auto-scanning after it tripped the time budget, and rescans now.
    internal static void ForceRefresh()
    {
        autoScanDisabled = false;
        dirty = true;
        scanNotBefore = 0.0;
    }

    static void OnEditorUpdate()
    {
        if (!dirty || autoScanDisabled || !IsEnabled)
        {
            return;
        }
        // Scanning mid-import or mid-compile races the asset database, and the count is
        // meaningless while play mode mutates the scene.
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }
        if (EditorApplication.timeSinceStartup < scanNotBefore)
        {
            return;
        }

        dirty = false;
        double startedAt = EditorApplication.timeSinceStartup;
        cachedCount = PrefabOverridesScanner.CountOverrides();
        double elapsed = EditorApplication.timeSinceStartup - startedAt;

        if (elapsed > AUTO_SCAN_BUDGET_SECONDS)
        {
            autoScanDisabled = true;
            Debug.Log($"PrefabOverridesToolbar: scan took {elapsed * 1000.0:F0}ms (budget {AUTO_SCAN_BUDGET_SECONDS * 1000.0:F0}ms) — auto-refresh disabled for this scene to keep the editor responsive. Use the Prefab Overrides window to refresh manually.");
        }
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
        if (autoScanDisabled)
        {
            text = cachedCount > 0 ? $"\u26A0 {cachedCount} overrides?" : "\u25CB prefabs";
            tooltip = "Scene too heavy to scan automatically \u2014 this count may be stale.\nOpen Tools/Simple Game Dev/Prefab Overrides Scanner and hit Refresh.";
        }
        else if (cachedCount < 0)
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
