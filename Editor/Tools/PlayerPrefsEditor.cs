namespace SimpleGameDev.Editor
{
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR_WIN
using Microsoft.Win32;
#endif

internal class PlayerPrefsEditor : EditorWindow
{
    enum PrefType { String, Int, Float }

    class PrefEntry
    {
        public string key;
        public PrefType type;
        public string stringValue;
        public int intValue;
        public float floatValue;
    }

    List<PrefEntry> entries = new List<PrefEntry>();
    Vector2 scrollPosition;
    string searchFilter = "";
    string newKey = "";
    PrefType newType = PrefType.String;
    string newStringValue = "";
    int newIntValue;
    float newFloatValue;
    bool showAddSection;

    public static void ShowWindow()
    {
        GetWindow<PlayerPrefsEditor>("PlayerPrefs Editor");
    }

    void OnEnable()
    {
        LoadAllPrefs();
    }

    void OnGUI()
    {
        DrawToolbar();

        if (showAddSection)
            DrawAddSection();

        DrawSearchBar();
        DrawPrefsList();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            LoadAllPrefs();
        }

        showAddSection = GUILayout.Toggle(showAddSection, "Add New", EditorStyles.toolbarButton, GUILayout.Width(65));

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField($"{GetFilteredEntries().Count} / {entries.Count} keys", EditorStyles.miniLabel, GUILayout.Width(80));

        if (GUILayout.Button("Delete All", EditorStyles.toolbarButton, GUILayout.Width(65)))
        {
            if (EditorUtility.DisplayDialog("Delete All PlayerPrefs",
                "Are you sure you want to delete ALL PlayerPrefs?\nThis cannot be undone.", "Delete All", "Cancel"))
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                LoadAllPrefs();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    void DrawAddSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Key", GUILayout.Width(30));
        newKey = EditorGUILayout.TextField(newKey);
        newType = (PrefType)EditorGUILayout.EnumPopup(newType, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Value", GUILayout.Width(30));

        switch (newType)
        {
            case PrefType.String:
                newStringValue = EditorGUILayout.TextField(newStringValue);
                break;
            case PrefType.Int:
                newIntValue = EditorGUILayout.IntField(newIntValue);
                break;
            case PrefType.Float:
                newFloatValue = EditorGUILayout.FloatField(newFloatValue);
                break;
        }

        GUI.enabled = !string.IsNullOrEmpty(newKey);
        if (GUILayout.Button("Add", GUILayout.Width(40)))
        {
            AddPref(newKey, newType);
            newKey = "";
            newStringValue = "";
            newIntValue = 0;
            newFloatValue = 0f;
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    void DrawSearchBar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);
        if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
        {
            searchFilter = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawPrefsList()
    {
        var filtered = GetFilteredEntries();

        if (filtered.Count == 0)
        {
            EditorGUILayout.HelpBox(
                entries.Count == 0
                    ? "No PlayerPrefs found. Use 'Add New' to create one."
                    : "No PlayerPrefs match the search filter.",
                MessageType.Info);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Key", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(50));
        EditorGUILayout.LabelField("Value", EditorStyles.boldLabel, GUILayout.Width(200));
        GUILayout.Space(24);
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < filtered.Count; i++)
        {
            DrawPrefEntry(filtered[i]);
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawPrefEntry(PrefEntry entry)
    {
        EditorGUILayout.BeginHorizontal();

        // Key name (read-only)
        EditorGUILayout.SelectableLabel(entry.key, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

        // Type label
        EditorGUILayout.LabelField(entry.type.ToString(), GUILayout.Width(50));

        // Editable value
        switch (entry.type)
        {
            case PrefType.String:
                string newStr = EditorGUILayout.TextField(entry.stringValue, GUILayout.Width(200));
                if (newStr != entry.stringValue)
                {
                    entry.stringValue = newStr;
                    PlayerPrefs.SetString(entry.key, newStr);
                    PlayerPrefs.Save();
                }
                break;

            case PrefType.Int:
                int newInt = EditorGUILayout.IntField(entry.intValue, GUILayout.Width(200));
                if (newInt != entry.intValue)
                {
                    entry.intValue = newInt;
                    PlayerPrefs.SetInt(entry.key, newInt);
                    PlayerPrefs.Save();
                }
                break;

            case PrefType.Float:
                float newFloat = EditorGUILayout.FloatField(entry.floatValue, GUILayout.Width(200));
                if (!Mathf.Approximately(newFloat, entry.floatValue))
                {
                    entry.floatValue = newFloat;
                    PlayerPrefs.SetFloat(entry.key, newFloat);
                    PlayerPrefs.Save();
                }
                break;
        }

        // Delete button
        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            PlayerPrefs.DeleteKey(entry.key);
            PlayerPrefs.Save();
            entries.Remove(entry);
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndHorizontal();
    }

    List<PrefEntry> GetFilteredEntries()
    {
        if (string.IsNullOrEmpty(searchFilter))
            return entries;

        string lower = searchFilter.ToLowerInvariant();
        return entries.Where(e => e.key.ToLowerInvariant().Contains(lower)).ToList();
    }

    void AddPref(string key, PrefType type)
    {
        switch (type)
        {
            case PrefType.String:
                PlayerPrefs.SetString(key, newStringValue);
                break;
            case PrefType.Int:
                PlayerPrefs.SetInt(key, newIntValue);
                break;
            case PrefType.Float:
                PlayerPrefs.SetFloat(key, newFloatValue);
                break;
        }
        PlayerPrefs.Save();
        LoadAllPrefs();
    }

    void LoadAllPrefs()
    {
        entries.Clear();

#if UNITY_EDITOR_WIN
        LoadFromWindowsRegistry();
#elif UNITY_EDITOR_OSX
        LoadFromMacOSPlist();
#else
        Debug.LogWarning("PlayerPrefs Editor: Automatic key enumeration is only supported on Windows and macOS.");
#endif

        entries = entries.OrderBy(e => e.key).ToList();
    }

#if UNITY_EDITOR_WIN
    void LoadFromWindowsRegistry()
    {
        string companyName = Application.companyName;
        string productName = Application.productName;
        string registryPath = $@"Software\Unity\UnityEditor\{companyName}\{productName}";

        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(registryPath))
        {
            if (key == null) return;

            foreach (string valueName in key.GetValueNames())
            {
                // Unity stores keys as "keyName_h{hash}" — extract original name
                string originalKey = ExtractKeyName(valueName);
                if (string.IsNullOrEmpty(originalKey)) continue;

                var entry = new PrefEntry { key = originalKey };

                RegistryValueKind kind = key.GetValueKind(valueName);

                switch (kind)
                {
                    case RegistryValueKind.DWord:
                        entry.type = PrefType.Int;
                        entry.intValue = (int)key.GetValue(valueName);
                        break;

                    case RegistryValueKind.Binary:
                        // Unity stores floats as 8-byte little-endian double
                        byte[] bytes = (byte[])key.GetValue(valueName);
                        if (bytes != null && bytes.Length == 8)
                        {
                            double d = System.BitConverter.ToDouble(bytes, 0);
                            entry.type = PrefType.Float;
                            entry.floatValue = (float)d;
                        }
                        break;

                    case RegistryValueKind.String:
                        entry.type = PrefType.String;
                        entry.stringValue = (string)key.GetValue(valueName) ?? "";
                        break;

                    default:
                        continue;
                }

                entries.Add(entry);
            }
        }
    }

    static string ExtractKeyName(string registryValueName)
    {
        // Format: "originalKey_h{hash}" where hash is a number
        int lastUnderscore = registryValueName.LastIndexOf("_h");
        if (lastUnderscore < 0) return null;

        // Verify the part after _h is numeric
        string hashPart = registryValueName.Substring(lastUnderscore + 2);
        if (hashPart.Length > 0 && hashPart.All(char.IsDigit))
        {
            return registryValueName.Substring(0, lastUnderscore);
        }

        return null;
    }
#endif

#if UNITY_EDITOR_OSX
    void LoadFromMacOSPlist()
    {
        // macOS: PlayerPrefs stored in ~/Library/Preferences/ as a plist file.
        // Unity uses different domain formats depending on version:
        //   - "unity.{companyName}.{productName}"
        //   - "{bundleIdentifier}" (e.g. "com.CompanyName.ProductName")
        string companyName = Application.companyName;
        string productName = Application.productName;

        string[] domains = new string[]
        {
            $"unity.{companyName}.{productName}",
            Application.identifier,
            $"com.{companyName}.{productName}",
        };

        foreach (string domain in domains)
        {
            if (string.IsNullOrEmpty(domain)) continue;

            string output = RunDefaults(domain);
            if (!string.IsNullOrEmpty(output))
            {
                ParsePlistXml(output);
                return;
            }
        }

        // Fallback: search ~/Library/Preferences/ for matching plist files
        string prefsDir = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal),
            "Library/Preferences");

        if (System.IO.Directory.Exists(prefsDir))
        {
            string searchPattern = $"*{productName}*.plist";
            foreach (string file in System.IO.Directory.GetFiles(prefsDir, searchPattern))
            {
                string domain = System.IO.Path.GetFileNameWithoutExtension(file);
                string output = RunDefaults(domain);
                if (!string.IsNullOrEmpty(output))
                {
                    ParsePlistXml(output);
                    return;
                }
            }
        }
    }

    string RunDefaults(string domain)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "defaults",
                Arguments = $"export \"{domain}\" -",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    return output;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"PlayerPrefs Editor: Failed to read macOS plist for domain '{domain}': {e.Message}");
        }

        return null;
    }

    static readonly System.Collections.Generic.HashSet<string> InternalPlistKeys = new System.Collections.Generic.HashSet<string>
    {
        "unity.player_session_background",
        "unity.player_sessionid",
        "unity.cloud_userid",
        "unity.player_session_elapsed",
        "unity.player_session_count",
    };

    void ParsePlistXml(string xml)
    {
        // Parse plist XML: <key>name</key> followed by a value tag inside <dict>
        var lines = xml.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (!line.StartsWith("<key>")) continue;

            string plistKey = line.Replace("<key>", "").Replace("</key>", "").Trim();

            if (InternalPlistKeys.Contains(plistKey)) continue;

            if (i + 1 >= lines.Length) continue;
            string valueLine = lines[i + 1].Trim();

            var entry = new PrefEntry { key = plistKey };

            if (valueLine.StartsWith("<string>"))
            {
                entry.type = PrefType.String;
                entry.stringValue = valueLine.Replace("<string>", "").Replace("</string>", "").Trim();
            }
            else if (valueLine.StartsWith("<integer>"))
            {
                string val = valueLine.Replace("<integer>", "").Replace("</integer>", "").Trim();
                entry.type = PrefType.Int;
                int.TryParse(val, out entry.intValue);
            }
            else if (valueLine.StartsWith("<real>"))
            {
                string val = valueLine.Replace("<real>", "").Replace("</real>", "").Trim();
                entry.type = PrefType.Float;
                float.TryParse(val, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out entry.floatValue);
            }
            else
            {
                continue;
            }

            entries.Add(entry);
        }
    }
#endif
}

}
