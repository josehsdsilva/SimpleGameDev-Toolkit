namespace SimpleGameDev.Editor
{
using System.IO;
using UnityEditor;

[InitializeOnLoad]
internal static class AutoRefreshWatcher
{
    private const string ENABLED_KEY = "SimpleGameDev_AutoRefreshEnabled";

    private static FileSystemWatcher _watcher;
    private static bool _needsRefresh;
    private static string scriptsPath = Path.GetFullPath("Assets");

    internal static bool IsEnabled => EditorPrefs.GetBool(ENABLED_KEY, true);

    static AutoRefreshWatcher()
    {
        StopWatcher();
        AssemblyReloadEvents.beforeAssemblyReload += StopWatcher;

        if (IsEnabled)
        {
            StartWatcher();
        }
    }

    internal static void SetEnabled(bool enabled)
    {
        EditorPrefs.SetBool(ENABLED_KEY, enabled);

        if (enabled)
        {
            StartWatcher();
        }
        else
        {
            StopWatcher();
        }
    }

    private static void StartWatcher()
    {
        if (_watcher != null) return;

        try
        {
            _watcher = new FileSystemWatcher(scriptsPath, "*.cs");
            _watcher.IncludeSubdirectories = true;
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName;
            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileChanged;
            _watcher.Renamed += OnFileRenamed;
            _watcher.EnableRaisingEvents = true;

            EditorApplication.update += OnEditorUpdate;
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"AutoRefreshWatcher failed to start: {e.Message}");
            _watcher = null;
        }
    }

    private static void StopWatcher()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            _watcher = null;
        }

        EditorApplication.update -= OnEditorUpdate;
        _needsRefresh = false;
    }

    private static void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _needsRefresh = true;
    }

    private static void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        _needsRefresh = true;
    }

    private static void OnEditorUpdate()
    {
        if (_needsRefresh)
        {
            _needsRefresh = false;
            AssetDatabase.Refresh();
        }
    }
}

}
