namespace SimpleGameDev.Editor
{
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal class PrefabOverridesWindow : EditorWindow
{
    List<OverrideEntry> entries = new List<OverrideEntry>();
    Dictionary<int, bool> foldouts = new Dictionary<int, bool>();
    Vector2 scroll;
    bool autoRefreshOnHierarchyChange = true;

    int pendingApplyIndex = -1;
    int pendingRevertIndex = -1;
    bool pendingApplyAll;
    bool pendingRefresh;

    internal static void ShowWindow()
    {
        PrefabOverridesWindow window = GetWindow<PrefabOverridesWindow>("Prefab Overrides");
        window.minSize = new Vector2(420, 200);
        window.Refresh();
    }

    void OnEnable()
    {
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    void OnDisable()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
    }

    void OnHierarchyChanged()
    {
        if (autoRefreshOnHierarchyChange)
        {
            Refresh();
            Repaint();
        }
    }

    void Refresh()
    {
        entries = PrefabOverridesScanner.ScanActiveScene();
    }

    void OnGUI()
    {
        DrawToolbar();
        DrawSummary();
        DrawSeparator();
        DrawEntries();
        ProcessPendingActions();
    }

    void ProcessPendingActions()
    {
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }

        if (pendingApplyAll)
        {
            pendingApplyAll = false;
            foreach (OverrideEntry entry in entries)
            {
                PrefabOverridesScanner.ApplyToInnermost(entry);
            }
            pendingRefresh = true;
        }

        if (pendingApplyIndex >= 0 && pendingApplyIndex < entries.Count)
        {
            PrefabOverridesScanner.ApplyToInnermost(entries[pendingApplyIndex]);
            pendingApplyIndex = -1;
            pendingRefresh = true;
        }

        if (pendingRevertIndex >= 0 && pendingRevertIndex < entries.Count)
        {
            OverrideEntry entry = entries[pendingRevertIndex];
            if (entry.OverriddenInstance != null)
            {
                PrefabUtility.RevertPrefabInstance(entry.OverriddenInstance, InteractionMode.UserAction);
            }
            pendingRevertIndex = -1;
            pendingRefresh = true;
        }

        if (pendingRefresh)
        {
            pendingRefresh = false;
            PrefabOverridesToolbar.MarkDirty();
            Refresh();
            Repaint();
        }
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            Refresh();
            PrefabOverridesToolbar.ForceRefresh();
        }

        autoRefreshOnHierarchyChange = GUILayout.Toggle(
            autoRefreshOnHierarchyChange,
            "Auto-refresh",
            EditorStyles.toolbarButton,
            GUILayout.Width(100));

        GUILayout.FlexibleSpace();

        GUI.enabled = entries.Count > 0;
        if (GUILayout.Button("Apply All to Correct Prefabs", EditorStyles.toolbarButton, GUILayout.Width(200)))
        {
            if (EditorUtility.DisplayDialog(
                "Apply All Overrides",
                $"Apply all {entries.Count} override group(s) to their innermost prefab assets?\n\nThis cannot be undone via the scene.",
                "Apply All",
                "Cancel"))
            {
                pendingApplyAll = true;
            }
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    void DrawSummary()
    {
        int totalModifications = 0;
        int nestedGroups = 0;
        foreach (OverrideEntry entry in entries)
        {
            totalModifications += entry.TotalCount;
            if (entry.IsNested)
            {
                nestedGroups++;
            }
        }

        EditorGUILayout.HelpBox(
            $"{entries.Count} group(s) with overrides · {totalModifications} total change(s) · {nestedGroups} require apply to nested prefab",
            entries.Count == 0 ? MessageType.Info : MessageType.Warning);
    }

    void DrawSeparator()
    {
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
    }

    void DrawEntries()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < entries.Count; i++)
        {
            DrawEntry(i, entries[i]);
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawEntry(int index, OverrideEntry entry)
    {
        int id = entry.OverriddenInstance != null ? entry.OverriddenInstance.GetInstanceID() : index;
        if (!foldouts.TryGetValue(id, out bool expanded))
        {
            expanded = entry.IsNested;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        string header = entry.OverriddenInstance != null ? entry.OverriddenInstance.name : "<missing>";
        expanded = EditorGUILayout.Foldout(expanded, $"{header}  ({entry.TotalCount})", true);
        foldouts[id] = expanded;

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
            Selection.activeGameObject = entry.OverriddenInstance;
            EditorGUIUtility.PingObject(entry.OverriddenInstance);
        }
        EditorGUILayout.EndHorizontal();

        if (expanded)
        {
            EditorGUI.indentLevel++;

            string innermostName = !string.IsNullOrEmpty(entry.InnermostPrefabPath)
                ? System.IO.Path.GetFileName(entry.InnermostPrefabPath)
                : "<unresolved>";

            Color prev = GUI.contentColor;
            if (entry.IsNested)
            {
                GUI.contentColor = new Color(1f, 0.75f, 0.3f);
            }
            EditorGUILayout.LabelField("Apply target:", innermostName);
            GUI.contentColor = prev;

            if (entry.IsNested && !string.IsNullOrEmpty(entry.OutermostPrefabPath))
            {
                EditorGUILayout.LabelField(
                    "Outermost (wrong target):",
                    System.IO.Path.GetFileName(entry.OutermostPrefabPath));
            }

            if (entry.Modifications != null && entry.Modifications.Count > 0)
            {
                EditorGUILayout.LabelField($"Modified properties ({entry.Modifications.Count}):", EditorStyles.miniBoldLabel);
                foreach (PropertyModification mod in entry.Modifications)
                {
                    string targetName = mod.target != null ? mod.target.GetType().Name : "<null>";
                    EditorGUILayout.LabelField($"  • {targetName}.{mod.propertyPath} = {mod.value}", EditorStyles.miniLabel);
                }
            }

            if (entry.AddedComponents != null && entry.AddedComponents.Count > 0)
            {
                EditorGUILayout.LabelField($"Added components: {entry.AddedComponents.Count}", EditorStyles.miniLabel);
            }
            if (entry.RemovedComponents != null && entry.RemovedComponents.Count > 0)
            {
                EditorGUILayout.LabelField($"Removed components: {entry.RemovedComponents.Count}", EditorStyles.miniLabel);
            }
            if (entry.AddedGameObjects != null && entry.AddedGameObjects.Count > 0)
            {
                EditorGUILayout.LabelField($"Added GameObjects: {entry.AddedGameObjects.Count}", EditorStyles.miniLabel);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.enabled = entry.InnermostPrefabAsset != null;
            if (GUILayout.Button("Apply to Correct Prefab", GUILayout.Width(180)))
            {
                pendingApplyIndex = index;
            }
            GUI.enabled = true;

            if (GUILayout.Button("Revert", GUILayout.Width(80)))
            {
                pendingRevertIndex = index;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

}

}
