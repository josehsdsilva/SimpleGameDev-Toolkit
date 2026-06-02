using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Central hub window for all Simple Game Dev editor tools.
/// </summary>
internal class SimpleGameDevHub : EditorWindow
{
    Vector2 scrollPosition;

    [MenuItem("Tools/Simple Game Dev/Hub")]
    static void ShowWindow()
    {
        GetWindow<SimpleGameDevHub>("Simple Game Dev");
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(8);

        DrawFeaturesSection();

        DrawSeparator();

        DrawToolsSection();

        DrawSeparator();

        DrawRaycastTargetSection();

        DrawSeparator();

        DrawPlayModeSceneSection();

        EditorGUILayout.Space(8);

        EditorGUILayout.EndScrollView();
    }

    void DrawFeaturesSection()
    {
        EditorGUILayout.LabelField("Features", EditorStyles.boldLabel);

        EditorGUI.indentLevel++;

        DrawToggle("Auto Refresh Watcher", AutoRefreshWatcher.IsEnabled, AutoRefreshWatcher.SetEnabled);
        DrawToggle("Git Branch Display", GitBranchToolbar.IsEnabled, GitBranchToolbar.SetEnabled);

        EditorGUI.indentLevel--;
    }

    void DrawToolsSection()
    {
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

        EditorGUI.indentLevel++;

        DrawToolButton("Favorites Tab", FavoritesTab.ShowWindow);
        DrawToolButton("PlayerPrefs Editor", PlayerPrefsEditor.ShowWindow);
        DrawToolButton("Color Palette Manager", ColorPaletteWindow.ShowWindow);

        EditorGUI.indentLevel--;
    }

    void DrawRaycastTargetSection()
    {
        EditorGUILayout.LabelField("Raycast Target (Selection)", EditorStyles.boldLabel);

        bool hasSelection = Selection.gameObjects.Length > 0;

        EditorGUI.indentLevel++;

        EditorGUI.BeginDisabledGroup(!hasSelection);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Enable (True)"))
        {
            RaycastTargetTool.SetTrue();
        }
        if (GUILayout.Button("Disable (False)"))
        {
            RaycastTargetTool.SetFalse();
        }
        if (GUILayout.Button("Disable (except Buttons)"))
        {
            RaycastTargetTool.SetFalseExceptButtons();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();

        if (!hasSelection)
        {
            EditorGUILayout.HelpBox("Select one or more GameObjects in the Hierarchy.", MessageType.Info);
        }

        EditorGUI.indentLevel--;
    }

    void DrawPlayModeSceneSection()
    {
        EditorGUILayout.LabelField("Play Mode Start Scene", EditorStyles.boldLabel);

        EditorGUI.indentLevel++;

        string startScene = EditorPrefs.GetString("PlayModeStartScene", "");
        string displayScene = string.IsNullOrEmpty(startScene) ? "(none)" : startScene;

        EditorGUILayout.LabelField("Current:", displayScene, EditorStyles.wordWrappedLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Current Scene"))
        {
            PlayModeSceneManager.SetStartScene();
            Repaint();
        }
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(startScene));
        if (GUILayout.Button("Clear"))
        {
            EditorPrefs.DeleteKey("PlayModeStartScene");
            Repaint();
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }

    void DrawToggle(string label, bool currentValue, System.Action<bool> setter)
    {
        bool newValue = EditorGUILayout.Toggle(label, currentValue);
        if (newValue != currentValue)
        {
            setter(newValue);
        }
    }

    void DrawToolButton(string label, System.Action openAction)
    {
        if (GUILayout.Button(label))
        {
            openAction();
        }
    }

    void DrawSeparator()
    {
        EditorGUILayout.Space(4);
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        EditorGUILayout.Space(4);
    }
}
