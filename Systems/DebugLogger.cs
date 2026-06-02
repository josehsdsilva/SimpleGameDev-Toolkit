namespace SimpleGameDev
{
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DebugLogger : MonoBehaviour
{
    #region Singleton
    private static DebugLogger _instance;
    private static readonly object _lock = new object();
    
    public static DebugLogger Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = FindFirstObjectByType<DebugLogger>();
                        if (_instance == null)
                        {
                            GameObject go = new GameObject("DebugLogger");
                            _instance = go.AddComponent<DebugLogger>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Load settings first
        LoadScriptStates();
        
        // Try to extract scripts, but don't fail if dataPath isn't ready
        if (scriptStates == null || scriptStates.Count == 0)
        {
            ExtractAndPopulateScripts();
        }
    }

    // Add a method to manually trigger script extraction from editor
    [ContextMenu("Refresh Scripts")]
    public void RefreshScripts()
    {
        ExtractAndPopulateScripts();
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
    #endregion

    [Serializable]
    public class ScriptState
    {
        public string scriptName;
        public bool isEnabled = true;
        [NonSerialized] public string category;

        public ScriptState(string name) { scriptName = name; }
    }

    [SerializeField] private List<ScriptState> scriptStates = new List<ScriptState>();
    [SerializeField] private bool enableAllLogsInBuild = false;
    [SerializeField] private LogLevel minimumLogLevel = LogLevel.Log;
    [SerializeField] private bool enableColorCoding = true;

    public enum LogLevel
    {
        Log = 0,
        Warning = 1,
        Error = 2
    }

    private Dictionary<string, bool> scriptSettings = new Dictionary<string, bool>();
    private static string saveFilePath;
    private Coroutine _saveCoroutine;

    // Color codes for different log levels
    private const string LOG_COLOR = "#FFFFFF";
    private const string WARNING_COLOR = "#FFAA00";
    private const string ERROR_COLOR = "#FF4444";

    private void Start()
    {
        // Ensure settings are synced on start
        UpdateScriptSettings();
    }

    public void UpdateScriptSettings()
    {
        scriptSettings.Clear();
        foreach (var script in scriptStates)
        {
            scriptSettings[script.scriptName] = script.isEnabled;
        }
    }

    private void ExtractAndPopulateScripts()
    {
        try
        {
#if !UNITY_EDITOR
            return;
#else
            string dataPath = Application.dataPath;
            if (string.IsNullOrEmpty(dataPath))
            {
                LogWarning("DebugLogger: Application.dataPath not ready, skipping script extraction");
                return;
            }

            string[] scriptFiles = Directory.GetFiles(dataPath, "*.cs", SearchOption.AllDirectories);
            Dictionary<string, string> scriptDirectories = new Dictionary<string, string>();

            foreach (string filePath in scriptFiles)
            {
                if (filePath.EndsWith("DebugLogger.cs")) continue;

                try
                {
                    string scriptContent = File.ReadAllText(filePath);
                    if (scriptContent.Contains("DebugLogger.Instance."))
                    {
                        string scriptName = Path.GetFileNameWithoutExtension(filePath);
                        string directory = Path.GetDirectoryName(filePath);
                        string relativePath = directory.Substring(dataPath.Length)
                            .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                            .Replace('\\', '/');
                        scriptDirectories[scriptName] = relativePath;
                    }
                }
                catch (Exception fileException)
                {
                    LogWarning($"DebugLogger: Could not read file {filePath}: {fileException.Message}");
                }
            }

            Dictionary<string, string> categories = ComputeCategories(scriptDirectories);

            List<ScriptState> newScriptStates = new List<ScriptState>();
            foreach (string scriptName in scriptDirectories.Keys.OrderBy(s => s))
            {
                ScriptState existingState = scriptStates.Find(s => s.scriptName == scriptName);
                ScriptState state = existingState ?? new ScriptState(scriptName);
                state.category = categories[scriptName];
                newScriptStates.Add(state);
            }

            if (newScriptStates.Any() || scriptStates.Count == 0)
            {
                scriptStates = newScriptStates;
                UpdateScriptSettings();
                ScheduleSave();

                Log($"DebugLogger: Found {scriptStates.Count} scripts using DebugLogger");
            }
#endif
        }
        catch (Exception e)
        {
            LogError($"DebugLogger: Error extracting scripts - {e.Message}");
        }
    }

    private Dictionary<string, string> ComputeCategories(Dictionary<string, string> scriptDirectories)
    {
        Dictionary<string, string> result = new Dictionary<string, string>();

        // Extract immediate parent folder for each script
        Dictionary<string, string> immediateParent = new Dictionary<string, string>();
        foreach (KeyValuePair<string, string> kvp in scriptDirectories)
        {
            string[] parts = kvp.Value.Split('/');
            string parent = parts.Length > 0 ? parts[parts.Length - 1] : "Uncategorized";
            if (string.IsNullOrEmpty(parent)) parent = "Root";
            immediateParent[kvp.Key] = parent;
        }

        // Find duplicate parent names from different paths
        Dictionary<string, HashSet<string>> parentToPaths = new Dictionary<string, HashSet<string>>();
        foreach (KeyValuePair<string, string> kvp in scriptDirectories)
        {
            string parent = immediateParent[kvp.Key];
            if (!parentToPaths.ContainsKey(parent))
                parentToPaths[parent] = new HashSet<string>();
            parentToPaths[parent].Add(kvp.Value);
        }

        HashSet<string> ambiguousParents = new HashSet<string>();
        foreach (KeyValuePair<string, HashSet<string>> kvp in parentToPaths)
        {
            if (kvp.Value.Count > 1)
                ambiguousParents.Add(kvp.Key);
        }

        // Assign final category names, disambiguating with grandparent when needed
        foreach (KeyValuePair<string, string> kvp in scriptDirectories)
        {
            string parent = immediateParent[kvp.Key];
            if (ambiguousParents.Contains(parent))
            {
                string[] parts = kvp.Value.Split('/');
                if (parts.Length >= 2)
                    result[kvp.Key] = parts[parts.Length - 2] + "/" + parent;
                else
                    result[kvp.Key] = parent;
            }
            else
            {
                result[kvp.Key] = parent;
            }
        }

        return result;
    }

    public void RefreshCategories()
    {
#if UNITY_EDITOR
        string dataPath = Application.dataPath;
        if (string.IsNullOrEmpty(dataPath)) return;

        string[] scriptFiles = Directory.GetFiles(dataPath, "*.cs", SearchOption.AllDirectories);
        Dictionary<string, string> scriptDirectories = new Dictionary<string, string>();

        foreach (string filePath in scriptFiles)
        {
            if (filePath.EndsWith("DebugLogger.cs")) continue;
            try
            {
                string scriptContent = File.ReadAllText(filePath);
                if (scriptContent.Contains("DebugLogger.Instance."))
                {
                    string scriptName = Path.GetFileNameWithoutExtension(filePath);
                    string directory = Path.GetDirectoryName(filePath);
                    string relativePath = directory.Substring(dataPath.Length)
                        .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                        .Replace('\\', '/');
                    scriptDirectories[scriptName] = relativePath;
                }
            }
            catch (Exception) { }
        }

        Dictionary<string, string> categories = ComputeCategories(scriptDirectories);

        // Rebuild scriptStates from scan, preserving toggle states for known scripts
        List<ScriptState> newScriptStates = new List<ScriptState>();
        foreach (string scriptName in scriptDirectories.Keys.OrderBy(s => s))
        {
            ScriptState existingState = scriptStates.Find(s => s.scriptName == scriptName);
            ScriptState state = existingState ?? new ScriptState(scriptName);
            state.category = categories[scriptName];
            newScriptStates.Add(state);
        }

        if (newScriptStates.Any() || scriptStates.Count == 0)
        {
            scriptStates = newScriptStates;
            UpdateScriptSettings();
        }
#endif
    }

    private void ScheduleSave()
    {
        if (_saveCoroutine != null)
        {
            StopCoroutine(_saveCoroutine);
        }
        _saveCoroutine = StartCoroutine(SaveAfterDelay());
    }

    private void ForceSave()
    {
        if (_saveCoroutine != null)
        {
            StopCoroutine(_saveCoroutine);
            _saveCoroutine = null;
        }
        SaveScriptStates();
    }

    private IEnumerator SaveAfterDelay()
    {
        yield return new WaitForSeconds(0.1f); // Reduced delay for faster saves
        SaveScriptStates();
        _saveCoroutine = null;
    }

    private void SaveScriptStates()
    {
        if (string.IsNullOrEmpty(saveFilePath)) return;
        
        try
        {
            var saveData = new SaveData
            {
                scriptStates = this.scriptStates,
                enableAllLogsInBuild = this.enableAllLogsInBuild,
                minimumLogLevel = this.minimumLogLevel,
                enableColorCoding = this.enableColorCoding
            };
            
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(saveFilePath, json);
            
#if UNITY_EDITOR
            // Ensure inspector updates
            EditorUtility.SetDirty(this);
#endif
        }
        catch (Exception e)
        {
            LogError($"DebugLogger: Failed to save settings - {e.Message}");
        }
    }

    public void LoadScriptStates()
    {
        if (string.IsNullOrEmpty(saveFilePath))
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, "DebugLoggerSettings.json");
        }
        
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data != null)
                {
                    scriptStates = data.scriptStates ?? new List<ScriptState>();
                    enableAllLogsInBuild = data.enableAllLogsInBuild;
                    minimumLogLevel = data.minimumLogLevel;
                    enableColorCoding = data.enableColorCoding;
                    UpdateScriptSettings();
                }
            }
            catch (Exception e)
            {
                LogError($"DebugLogger: Failed to load settings - {e.Message}");
                // Reset to defaults if loading fails
                scriptStates = new List<ScriptState>();
                UpdateScriptSettings();
            }
        }
    }

    [Serializable]
    private class SaveData
    {
        public List<ScriptState> scriptStates;
        public bool enableAllLogsInBuild;
        public LogLevel minimumLogLevel;
        public bool enableColorCoding = true;
    }

    private string FormatMessage(string message, string scriptName, string member, int line, string colorCode = LOG_COLOR)
    {
        string location = $"[{scriptName}.cs:{line}]";
        string method = $"{member}";
        
        if (enableColorCoding && Application.isEditor)
        {
            return $"<color={colorCode}>{location} -> {method} -> {message}</color>";
        }
        return $"{location} -> {method} -> {message}";
    }

    public void Log(string message,
        [CallerFilePath] string file = "",
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0)
    {
        string scriptName = Path.GetFileNameWithoutExtension(file);
        if (minimumLogLevel > LogLevel.Log || !ShouldLog(scriptName)) return;
        
        Debug.Log(FormatMessage(message, scriptName, member, line, LOG_COLOR));
    }

    public void LogWarning(string message,
        [CallerFilePath] string file = "",
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0)
    {
        string scriptName = Path.GetFileNameWithoutExtension(file);
        if (minimumLogLevel > LogLevel.Warning || !ShouldLog(scriptName)) return;
        
        Debug.LogWarning(FormatMessage(message, scriptName, member, line, WARNING_COLOR));
    }

    public void LogError(string message,
        [CallerFilePath] string file = "",
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0)
    {
        string scriptName = Path.GetFileNameWithoutExtension(file);
        if (minimumLogLevel > LogLevel.Error || !ShouldLog(scriptName)) return;
        
        Debug.LogError(FormatMessage(message, scriptName, member, line, ERROR_COLOR));
    }

    // Overloaded methods for formatted strings
    public void Log(string format, params object[] args) => Log(string.Format(format, args));
    public void LogWarning(string format, params object[] args) => LogWarning(string.Format(format, args));
    public void LogError(string format, params object[] args) => LogError(string.Format(format, args));

    public void LogException(Exception exception,
        [CallerFilePath] string file = "",
        [CallerMemberName] string member = "",
        [CallerLineNumber] int line = 0)
    {
        string scriptName = Path.GetFileNameWithoutExtension(file);
        if (!ShouldLog(scriptName)) return;
        
        string message = $"Exception: {exception.Message}\nStackTrace: {exception.StackTrace}";
        Debug.LogError(FormatMessage(message, scriptName, member, line, ERROR_COLOR));
    }

    private bool ShouldLog(string scriptName)
    {
        if (string.IsNullOrEmpty(scriptName)) return false;
        if (!Application.isEditor && enableAllLogsInBuild) return true;
        return scriptSettings.TryGetValue(scriptName, out bool isEnabled) && isEnabled;
    }

    public void SetScriptEnabled(string scriptName, bool enabled)
    {
        if (string.IsNullOrEmpty(scriptName)) return;
        
        var scriptState = scriptStates.Find(s => s.scriptName == scriptName);
        if (scriptState != null)
        {
            scriptState.isEnabled = enabled;
        }
        else
        {
            scriptStates.Add(new ScriptState(scriptName) { isEnabled = enabled });
        }
        
        scriptSettings[scriptName] = enabled;
        
        // Force immediate save for user interactions to prevent loss on play mode transitions
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ForceSave(); // Immediate save in editor
        }
        else
        {
            ScheduleSave(); // Delayed save during play
        }
#else
        ScheduleSave();
#endif
    }

    public bool IsScriptEnabled(string scriptName) => ShouldLog(scriptName);
    public void EnableScript(string scriptName) => SetScriptEnabled(scriptName, true);
    public void DisableScript(string scriptName) => SetScriptEnabled(scriptName, false);

    // Utility methods
    public void EnableAllScripts()
    {
        foreach (var script in scriptStates)
        {
            script.isEnabled = true;
            scriptSettings[script.scriptName] = true;
        }
        
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ForceSave(); // Immediate save for bulk operations
        }
        else
        {
            ScheduleSave();
        }
#else
        ScheduleSave();
#endif
    }

    public void DisableAllScripts()
    {
        foreach (var script in scriptStates)
        {
            script.isEnabled = false;
            scriptSettings[script.scriptName] = false;
        }
        
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ForceSave(); // Immediate save for bulk operations
        }
        else
        {
            ScheduleSave();
        }
#else
        ScheduleSave();
#endif
    }

    // Ensure settings are saved when play mode changes
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) ForceSave();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) ForceSave();
    }

    private void OnDestroy()
    {
        ForceSave();
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(DebugLogger))]
    public class DebugLoggerEditor : Editor
    {
        private static class Styles
        {
            public static readonly GUIStyle ButtonStyle = new GUIStyle(EditorStyles.miniButtonMid)
            {
                padding = new RectOffset(5, 5, 3, 3),
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageLeft,
                fixedHeight = 20,
                clipping = TextClipping.Clip
            };

            public static readonly GUIStyle HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                margin = new RectOffset(0, 0, 10, 5)
            };
        }

        private bool hasLoadedOnce = false;
        private Dictionary<string, bool> categoryFoldouts = new Dictionary<string, bool>();

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            DebugLogger debugLogger = target as DebugLogger;
            if (debugLogger != null)
            {
                if (state == PlayModeStateChange.ExitingEditMode)
                {
                    debugLogger.ForceSave();
                }
                else if (state == PlayModeStateChange.EnteredEditMode)
                {
                    hasLoadedOnce = false;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            DebugLogger debugLogger = (DebugLogger)target;

            if (!hasLoadedOnce && !Application.isPlaying)
            {
                debugLogger.LoadScriptStates();
                debugLogger.RefreshCategories();
                hasLoadedOnce = true;
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Debug Settings", Styles.HeaderStyle);
            debugLogger.enableAllLogsInBuild = EditorGUILayout.Toggle("Enable All Logs in Build", debugLogger.enableAllLogsInBuild);
            debugLogger.minimumLogLevel = (LogLevel)EditorGUILayout.EnumPopup("Minimum Log Level", debugLogger.minimumLogLevel);
            debugLogger.enableColorCoding = EditorGUILayout.Toggle("Enable Color Coding", debugLogger.enableColorCoding);

            EditorGUILayout.Space(10);

            // Refresh button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Scripts ({debugLogger.scriptStates.Count})", Styles.HeaderStyle);
            if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                debugLogger.RefreshScripts();
                hasLoadedOnce = false;
            }
            EditorGUILayout.EndHorizontal();

            if (debugLogger.scriptStates.Count == 0)
            {
                EditorGUILayout.HelpBox("No scripts using DebugLogger found. Click 'Refresh' to scan for scripts.", MessageType.Info);
            }
            else
            {
                DrawCategorizedToggleBar(debugLogger);
            }

            if (EditorGUI.EndChangeCheck())
            {
                debugLogger.UpdateScriptSettings();
                debugLogger.ForceSave();
                EditorUtility.SetDirty(debugLogger);
            }
        }

        private void DrawCategorizedToggleBar(DebugLogger debugLogger)
        {
            float availableWidth = EditorGUIUtility.currentViewWidth - 30;
            Color originalColor = GUI.backgroundColor;

            // --- Global controls row ---
            EditorGUILayout.BeginHorizontal();

            bool allEnabled = debugLogger.scriptStates.All(s => s.isEnabled);
            GUI.backgroundColor = allEnabled
                ? GetThemeColor(new Color(0.7f, 0.85f, 1f))
                : GetThemeColor(Color.gray);

            if (GUILayout.Button("All", Styles.ButtonStyle, GUILayout.Width(45)))
            {
                if (allEnabled)
                    debugLogger.DisableAllScripts();
                else
                    debugLogger.EnableAllScripts();
                EditorUtility.SetDirty(debugLogger);
            }

            GUI.backgroundColor = originalColor;

            if (GUILayout.Button("Collapse All", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                List<string> keys = new List<string>(categoryFoldouts.Keys);
                foreach (string key in keys)
                    categoryFoldouts[key] = false;
            }
            if (GUILayout.Button("Expand All", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                List<string> keys = new List<string>(categoryFoldouts.Keys);
                foreach (string key in keys)
                    categoryFoldouts[key] = true;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            // --- Group scripts by category ---
            Dictionary<string, List<ScriptState>> categories = new Dictionary<string, List<ScriptState>>();
            foreach (ScriptState state in debugLogger.scriptStates)
            {
                string cat = string.IsNullOrEmpty(state.category) ? "Uncategorized" : state.category;
                if (!categories.ContainsKey(cat))
                    categories[cat] = new List<ScriptState>();
                categories[cat].Add(state);
            }

            List<string> sortedCategories = categories.Keys.OrderBy(c => c).ToList();

            // --- Draw each category ---
            foreach (string category in sortedCategories)
            {
                List<ScriptState> scripts = categories[category];

                if (!categoryFoldouts.ContainsKey(category))
                    categoryFoldouts[category] = true;

                // Category header row
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                categoryFoldouts[category] = EditorGUILayout.Foldout(
                    categoryFoldouts[category],
                    $"{category} ({scripts.Count})",
                    true,
                    EditorStyles.foldoutHeader
                );

                // Per-category "All" toggle
                bool categoryAllEnabled = scripts.All(s => s.isEnabled);
                GUI.backgroundColor = categoryAllEnabled
                    ? GetThemeColor(new Color(0.7f, 0.85f, 1f))
                    : GetThemeColor(Color.gray);

                if (GUILayout.Button("All", EditorStyles.miniButton, GUILayout.Width(35)))
                {
                    bool newState = !categoryAllEnabled;
                    foreach (ScriptState script in scripts)
                    {
                        script.isEnabled = newState;
                        debugLogger.SetScriptEnabled(script.scriptName, newState);
                    }
                    EditorUtility.SetDirty(debugLogger);
                }

                GUI.backgroundColor = originalColor;
                EditorGUILayout.EndHorizontal();

                // Script buttons (only if expanded)
                if (categoryFoldouts[category])
                {
                    float currentWidth = 0;
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(16);
                    currentWidth += 16;

                    foreach (ScriptState script in scripts)
                    {
                        GUIContent content = new GUIContent(script.scriptName,
                            $"Toggle debug logs for {script.scriptName}");
                        float buttonWidth = GUI.skin.label.CalcSize(content).x
                            + Styles.ButtonStyle.padding.horizontal + 16;

                        if (currentWidth + buttonWidth > availableWidth)
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Space(16);
                            currentWidth = 16;
                        }

                        GUI.backgroundColor = script.isEnabled
                            ? GetThemeColor(new Color(0.7f, 0.85f, 1f))
                            : GetThemeColor(Color.gray);

                        if (GUILayout.Button(content, Styles.ButtonStyle, GUILayout.Width(buttonWidth)))
                        {
                            script.isEnabled = !script.isEnabled;
                            debugLogger.SetScriptEnabled(script.scriptName, script.isEnabled);
                            EditorUtility.SetDirty(debugLogger);
                        }

                        currentWidth += buttonWidth + 2;
                    }

                    GUI.backgroundColor = originalColor;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(3);
                }
            }

            EditorGUILayout.Space(5);
        }

        private Color GetThemeColor(Color color) =>
            EditorGUIUtility.isProSkin ? color * 1.2f : color * 0.8f;
    }
#endif
}
}
