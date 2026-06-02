namespace SimpleGameDev.Editor
{
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), Id, "Hidden Overlays", true)]
internal class SceneViewOverlayHider : Overlay
{
    private const string Id = "simplegamedev-scene-view-hidden-overlays";
    private const string SessionInitKey = "SimpleGameDev_SceneViewHidden_Initialized";
    private const string FoldoutPrefKeyPrefix = "SimpleGameDev_SceneViewHidden_Foldout_";
    private const string OtherGroupLabel = "(other)";

    private static Dictionary<Type, GroupInfo> _cachedTypeGroups;
    private static Dictionary<string, int> _cachedOrderLookup;

    private VisualElement _list;

    private struct GroupInfo
    {
        public string Group;
        public int Order;
    }

    public override VisualElement CreatePanelContent()
    {
        VisualElement root = new VisualElement();
        root.style.minWidth = 220;
        root.style.paddingTop = 4;
        root.style.paddingBottom = 4;
        root.style.paddingLeft = 4;
        root.style.paddingRight = 4;

        _list = new VisualElement();
        root.Add(_list);

        VisualElement buttons = new VisualElement();
        buttons.style.flexDirection = FlexDirection.Row;
        buttons.style.marginTop = 4;

        Button hideAll = new Button(() => SetAll(true)) { text = "Hide All" };
        hideAll.style.flexGrow = 1;
        Button showAll = new Button(() => SetAll(false)) { text = "Show All" };
        showAll.style.flexGrow = 1;
        buttons.Add(hideAll);
        buttons.Add(showAll);
        root.Add(buttons);

        EditorApplication.hierarchyChanged += Rebuild;
        EditorSceneManager.sceneOpened += OnSceneOpenedRebuild;
        Rebuild();
        return root;
    }

    public override void OnWillBeDestroyed()
    {
        EditorApplication.hierarchyChanged -= Rebuild;
        EditorSceneManager.sceneOpened -= OnSceneOpenedRebuild;
        base.OnWillBeDestroyed();
    }

    private void OnSceneOpenedRebuild(Scene scene, OpenSceneMode mode) => Rebuild();

    private void Rebuild()
    {
        if (_list == null) return;
        _list.Clear();

        List<GroupBucket> buckets = FindAllTargetsByGroup();
        if (buckets.Count == 0)
        {
            Label hint = new Label("(no marked overlays in open scenes)");
            hint.style.color = new Color(0.7f, 0.7f, 0.7f);
            hint.style.unityFontStyleAndWeight = FontStyle.Italic;
            _list.Add(hint);
            return;
        }

        foreach (GroupBucket bucket in buckets)
        {
            string foldoutKey = FoldoutPrefKeyPrefix + bucket.Group;
            Foldout foldout = new Foldout
            {
                text = $"{bucket.Group} ({bucket.Targets.Count})",
                value = EditorPrefs.GetBool(foldoutKey, true)
            };
            foldout.RegisterValueChangedCallback(evt =>
            {
                if (evt.target == foldout) EditorPrefs.SetBool(foldoutKey, foldout.value);
            });
            foldout.style.marginTop = 2;

            Toggle groupToggle = new Toggle
            {
                tooltip = "Toggle visibility for entire group"
            };
            groupToggle.style.marginLeft = 4;
            groupToggle.style.marginRight = 0;

            VisualElement foldoutHeader = foldout.Q<Toggle>(className: "unity-foldout__toggle");
            if (foldoutHeader != null)
                foldoutHeader.Add(groupToggle);
            else
                foldout.hierarchy.Insert(0, groupToggle);

            List<GameObject> capturedTargets = bucket.Targets;
            groupToggle.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                bool anyVisible = false;
                foreach (GameObject go in capturedTargets)
                {
                    if (go != null && !SceneVisibilityManager.instance.IsHidden(go, false))
                    {
                        anyVisible = true;
                        break;
                    }
                }
                bool makeVisible = !anyVisible;
                foreach (GameObject go in capturedTargets)
                    SetHidden(go, !makeVisible);
                Rebuild();
            });

            UpdateGroupToggleState(groupToggle, bucket.Targets);

            foreach (GameObject go in bucket.Targets)
            {
                GameObject captured = go;
                Toggle toggle = new Toggle(go.name)
                {
                    value = !SceneVisibilityManager.instance.IsHidden(captured, false),
                    tooltip = "Checked = visible in scene view"
                };
                toggle.RegisterValueChangedCallback(evt => SetHidden(captured, !evt.newValue));
                foldout.Add(toggle);
            }

            _list.Add(foldout);
        }
    }

    private void SetAll(bool hidden)
    {
        foreach (GroupBucket bucket in FindAllTargetsByGroup())
            foreach (GameObject go in bucket.Targets)
                SetHidden(go, hidden);
        Rebuild();
    }

    private static void SetHidden(GameObject go, bool hidden)
    {
        if (go == null) return;
        if (hidden) SceneVisibilityManager.instance.Hide(go, true);
        else SceneVisibilityManager.instance.Show(go, true);
        SceneView.RepaintAll();
    }

    private static void UpdateGroupToggleState(Toggle toggle, List<GameObject> targets)
    {
        int visibleCount = 0;
        int totalCount = 0;
        foreach (GameObject go in targets)
        {
            if (go == null) continue;
            totalCount++;
            if (!SceneVisibilityManager.instance.IsHidden(go, false))
                visibleCount++;
        }
        bool allVisible = totalCount > 0 && visibleCount == totalCount;
        bool mixed = visibleCount > 0 && visibleCount < totalCount;
        toggle.SetValueWithoutNotify(allVisible);
        toggle.showMixedValue = mixed;
    }

    private struct GroupBucket
    {
        public string Group;
        public int Order;
        public List<GameObject> Targets;
    }

    private static List<GroupBucket> FindAllTargetsByGroup()
    {
        Dictionary<string, GroupBucket> buckets = new Dictionary<string, GroupBucket>();
        Dictionary<Type, GroupInfo> typeGroups = GetMarkedTypeGroups();
        if (typeGroups.Count == 0) return new List<GroupBucket>();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            foreach (GameObject root in scene.GetRootGameObjects())
                CollectTargets(root.transform, typeGroups, buckets);
        }

        List<GroupBucket> result = new List<GroupBucket>(buckets.Values);
        result.Sort((a, b) =>
        {
            int byOrder = a.Order.CompareTo(b.Order);
            if (byOrder != 0) return byOrder;
            return string.Compare(a.Group, b.Group, StringComparison.Ordinal);
        });
        return result;
    }

    private static void CollectTargets(Transform t, Dictionary<Type, GroupInfo> typeGroups, Dictionary<string, GroupBucket> buckets)
    {
        MonoBehaviour[] mbs = t.GetComponents<MonoBehaviour>();
        for (int i = 0; i < mbs.Length; i++)
        {
            MonoBehaviour mb = mbs[i];
            if (mb == null) continue;
            if (typeGroups.TryGetValue(mb.GetType(), out GroupInfo info))
            {
                string label = string.IsNullOrEmpty(info.Group) ? OtherGroupLabel : info.Group;
                int order = string.IsNullOrEmpty(info.Group) ? int.MaxValue : info.Order;
                if (!buckets.TryGetValue(label, out GroupBucket bucket))
                {
                    bucket = new GroupBucket
                    {
                        Group = label,
                        Order = order,
                        Targets = new List<GameObject>()
                    };
                }
                bucket.Targets.Add(t.gameObject);
                buckets[label] = bucket;
                break;
            }
        }
        for (int i = 0; i < t.childCount; i++)
            CollectTargets(t.GetChild(i), typeGroups, buckets);
    }

    private static Dictionary<Type, GroupInfo> GetMarkedTypeGroups()
    {
        if (_cachedTypeGroups != null) return _cachedTypeGroups;
        _cachedTypeGroups = new Dictionary<Type, GroupInfo>();
        Dictionary<string, int> orderLookup = GetOrderLookup();

        foreach (Type t in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
        {
            if (t.IsAbstract) continue;
            HideInSceneViewAttribute attr = t.GetCustomAttribute<HideInSceneViewAttribute>(true);
            if (attr == null) continue;

            int order = 0;
            if (!string.IsNullOrEmpty(attr.GroupName))
                orderLookup.TryGetValue(attr.GroupName, out order);

            _cachedTypeGroups[t] = new GroupInfo { Group = attr.GroupName, Order = order };
        }
        return _cachedTypeGroups;
    }

    private static Dictionary<string, int> GetOrderLookup()
    {
        if (_cachedOrderLookup != null) return _cachedOrderLookup;
        _cachedOrderLookup = new Dictionary<string, int>();

        foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types; }

            foreach (Type t in types)
            {
                if (t == null) continue;
                if (!(t.IsAbstract && t.IsSealed)) continue; // static class

                foreach (FieldInfo f in t.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (f.FieldType != typeof(OverlayGroup[])) continue;
                    OverlayGroup[] arr = f.GetValue(null) as OverlayGroup[];
                    if (arr == null) continue;
                    foreach (OverlayGroup g in arr)
                    {
                        if (g == null || string.IsNullOrEmpty(g.Name)) continue;
                        if (!_cachedOrderLookup.ContainsKey(g.Name))
                            _cachedOrderLookup[g.Name] = g.Order;
                    }
                }
            }
        }
        return _cachedOrderLookup;
    }

    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorSceneManager.sceneOpened += (_, __) => EditorApplication.delayCall += AutoHideAllOpenScenes;
        if (!SessionState.GetBool(SessionInitKey, false))
        {
            SessionState.SetBool(SessionInitKey, true);
            EditorApplication.delayCall += AutoHideAllOpenScenes;
        }
    }

    private static void AutoHideAllOpenScenes()
    {
        foreach (GroupBucket bucket in FindAllTargetsByGroup())
        {
            foreach (GameObject go in bucket.Targets)
            {
                if (go != null && !SceneVisibilityManager.instance.IsHidden(go, false))
                    SceneVisibilityManager.instance.Hide(go, true);
            }
        }
        SceneView.RepaintAll();
    }
}
#endif

}
