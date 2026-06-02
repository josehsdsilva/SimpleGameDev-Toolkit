using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal struct OverrideEntry
{
    public GameObject InstanceRoot;
    public GameObject OverriddenInstance;
    public Object InnermostPrefabAsset;
    public string InnermostPrefabPath;
    public string OutermostPrefabPath;
    public List<ObjectOverride> ObjectOverrides;
    public List<PropertyModification> Modifications;
    public List<AddedComponent> AddedComponents;
    public List<RemovedComponent> RemovedComponents;
    public List<AddedGameObject> AddedGameObjects;

    public bool IsNested => InnermostPrefabPath != OutermostPrefabPath;
    public int TotalCount =>
        (ObjectOverrides?.Count ?? 0) +
        (AddedComponents?.Count ?? 0) +
        (RemovedComponents?.Count ?? 0) +
        (AddedGameObjects?.Count ?? 0);
}

internal static class PrefabOverridesScanner
{
    internal static List<OverrideEntry> ScanActiveScene()
    {
        List<OverrideEntry> result = new List<OverrideEntry>();
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return result;
        }

        HashSet<GameObject> visited = new HashSet<GameObject>();
        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            CollectFromHierarchy(root, result, visited);
        }

        return result;
    }

    internal static int CountOverrides()
    {
        List<OverrideEntry> entries = ScanActiveScene();
        int total = 0;
        foreach (OverrideEntry entry in entries)
        {
            total += entry.TotalCount;
        }
        return total;
    }

    internal static void ApplyToInnermost(OverrideEntry entry)
    {
        if (entry.InnermostPrefabAsset == null || string.IsNullOrEmpty(entry.InnermostPrefabPath))
        {
            Debug.LogError("PrefabOverridesScanner: cannot apply — no innermost prefab asset resolved");
            return;
        }

        string path = entry.InnermostPrefabPath;

        if (entry.ObjectOverrides != null)
        {
            foreach (ObjectOverride objectOverride in entry.ObjectOverrides)
            {
                if (objectOverride == null || objectOverride.instanceObject == null)
                {
                    continue;
                }
                ApplyObjectOverrideSkippingSceneRefs(objectOverride.instanceObject, path);
            }
        }

        if (entry.AddedComponents != null)
        {
            foreach (AddedComponent added in entry.AddedComponents)
            {
                added.Apply(path, InteractionMode.UserAction);
            }
        }

        if (entry.RemovedComponents != null)
        {
            foreach (RemovedComponent removed in entry.RemovedComponents)
            {
                removed.Apply(path, InteractionMode.UserAction);
            }
        }

        if (entry.AddedGameObjects != null)
        {
            foreach (AddedGameObject addedGo in entry.AddedGameObjects)
            {
                addedGo.Apply(path, InteractionMode.UserAction);
            }
        }
    }

    static void CollectFromHierarchy(GameObject root, List<OverrideEntry> result, HashSet<GameObject> visited)
    {
        if (root == null)
        {
            return;
        }

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            GameObject go = t.gameObject;
            if (!PrefabUtility.IsPartOfPrefabInstance(go))
            {
                continue;
            }

            GameObject nearestRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
            if (nearestRoot == null || visited.Contains(nearestRoot))
            {
                continue;
            }
            visited.Add(nearestRoot);

            List<ObjectOverride> objectOverrides = FilterObjectOverrides(PrefabUtility.GetObjectOverrides(nearestRoot, false));
            List<PropertyModification> mods = CollectModifications(nearestRoot);
            List<AddedComponent> addedComponents = PrefabUtility.GetAddedComponents(nearestRoot);
            List<RemovedComponent> removedComponents = PrefabUtility.GetRemovedComponents(nearestRoot);
            List<AddedGameObject> addedGameObjects = PrefabUtility.GetAddedGameObjects(nearestRoot);

            if ((objectOverrides == null || objectOverrides.Count == 0)
                && (addedComponents == null || addedComponents.Count == 0)
                && (removedComponents == null || removedComponents.Count == 0)
                && (addedGameObjects == null || addedGameObjects.Count == 0))
            {
                continue;
            }

            Object innermostAsset = ResolveInnermostAsset(nearestRoot);
            string innermostPath = innermostAsset != null ? AssetDatabase.GetAssetPath(innermostAsset) : null;

            GameObject outermostRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            string outermostPath = outermostRoot != null
                ? PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(outermostRoot)
                : null;

            OverrideEntry entry = new OverrideEntry
            {
                InstanceRoot = outermostRoot != null ? outermostRoot : nearestRoot,
                OverriddenInstance = nearestRoot,
                InnermostPrefabAsset = innermostAsset,
                InnermostPrefabPath = innermostPath,
                OutermostPrefabPath = outermostPath,
                ObjectOverrides = objectOverrides,
                Modifications = mods,
                AddedComponents = addedComponents,
                RemovedComponents = removedComponents,
                AddedGameObjects = addedGameObjects,
            };

            result.Add(entry);
        }
    }

    static List<ObjectOverride> FilterObjectOverrides(List<ObjectOverride> source)
    {
        List<ObjectOverride> result = new List<ObjectOverride>();
        if (source == null)
        {
            return result;
        }
        foreach (ObjectOverride objectOverride in source)
        {
            if (objectOverride == null || objectOverride.instanceObject == null)
            {
                continue;
            }
            if (HasApplicableOverride(objectOverride.instanceObject))
            {
                result.Add(objectOverride);
            }
        }
        return result;
    }

    static bool HasApplicableOverride(Object instanceObject)
    {
        SerializedObject so = new SerializedObject(instanceObject);
        SerializedProperty iter = so.GetIterator();
        while (iter.Next(true))
        {
            if (!iter.prefabOverride || iter.isDefaultOverride)
            {
                continue;
            }
            if (iter.propertyType == SerializedPropertyType.ObjectReference
                && iter.objectReferenceValue != null
                && !EditorUtility.IsPersistent(iter.objectReferenceValue))
            {
                continue;
            }
            return true;
        }
        return false;
    }

    static void ApplyObjectOverrideSkippingSceneRefs(Object instanceObject, string path)
    {
        List<string> propertyPaths = new List<string>();
        SerializedObject so = new SerializedObject(instanceObject);
        SerializedProperty iter = so.GetIterator();
        while (iter.Next(true))
        {
            if (!iter.prefabOverride || iter.isDefaultOverride)
            {
                continue;
            }
            if (iter.propertyType == SerializedPropertyType.ObjectReference
                && iter.objectReferenceValue != null
                && !EditorUtility.IsPersistent(iter.objectReferenceValue))
            {
                continue;
            }
            propertyPaths.Add(iter.propertyPath);
        }

        foreach (string propertyPath in propertyPaths)
        {
            SerializedObject freshSo = new SerializedObject(instanceObject);
            SerializedProperty prop = freshSo.FindProperty(propertyPath);
            if (prop == null || !prop.prefabOverride)
            {
                continue;
            }
            PrefabUtility.ApplyPropertyOverride(prop, path, InteractionMode.UserAction);
        }
    }

    internal static bool IsSceneReferenceOverride(PropertyModification mod)
    {
        if (mod == null || mod.objectReference == null)
        {
            return false;
        }
        return !EditorUtility.IsPersistent(mod.objectReference);
    }

    static List<PropertyModification> CollectModifications(GameObject nearestRoot)
    {
        PropertyModification[] raw = PrefabUtility.GetPropertyModifications(nearestRoot);
        if (raw == null)
        {
            return new List<PropertyModification>();
        }

        List<PropertyModification> filtered = new List<PropertyModification>();
        foreach (PropertyModification mod in raw)
        {
            if (mod == null || mod.target == null)
            {
                continue;
            }
            if (IsDefaultOverride(mod))
            {
                continue;
            }
            if (IsSceneReferenceOverride(mod))
            {
                continue;
            }
            filtered.Add(mod);
        }
        return filtered;
    }

    static bool IsDefaultOverride(PropertyModification mod)
    {
        string p = mod.propertyPath;
        if (string.IsNullOrEmpty(p))
        {
            return false;
        }
        // Unity treats root-transform position/rotation and name as default overrides.
        if (p == "m_Name")
        {
            return true;
        }
        if (mod.target is Transform && (
            p.StartsWith("m_LocalPosition") ||
            p.StartsWith("m_LocalRotation") ||
            p.StartsWith("m_RootOrder") ||
            p.StartsWith("m_LocalEulerAnglesHint")))
        {
            return true;
        }
        return false;
    }

    static Object ResolveInnermostAsset(GameObject instance)
    {
        Object current = PrefabUtility.GetCorrespondingObjectFromSource(instance);
        if (current == null)
        {
            return null;
        }

        while (true)
        {
            Object deeper = PrefabUtility.GetCorrespondingObjectFromSource(current);
            if (deeper == null || deeper == current)
            {
                return current;
            }
            current = deeper;
        }
    }
}
