namespace SimpleGameDev.Editor
{
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

        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            CollectFromHierarchy(root, result);
        }

        return result;
    }

    /// Toolbar-only count. Deliberately does NOT build OverrideEntry: resolving the innermost
    /// asset, the outermost path and the property modifications costs an AssetDatabase lookup
    /// per prefab instance and contributes nothing to the total.
    internal static int CountOverrides()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return 0;
        }

        int total = 0;
        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            foreach (GameObject instanceRoot in EnumerateOverriddenInstanceRoots(root))
            {
                total += CountOverridesOnRoot(instanceRoot);
            }
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

    /// Yields every prefab instance root (outermost and nested) under `root` that Unity's own
    /// native check says carries at least one override. Each instance root is a distinct
    /// GameObject, so no dedup set is needed.
    ///
    /// The cheap `IsAnyPrefabInstanceRoot` + `HasPrefabInstanceAnyOverrides` pair rejects the
    /// overwhelming majority of instances before we ever touch `GetObjectOverrides` or build a
    /// `SerializedObject` — those are what made a scene with hundreds of nested prefabs (an art
    /// scene with a fully dressed mansion) stall the editor for seconds per scan.
    static IEnumerable<GameObject> EnumerateOverriddenInstanceRoots(GameObject root)
    {
        if (root == null)
        {
            yield break;
        }

        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            GameObject go = t.gameObject;
            if (!PrefabUtility.IsAnyPrefabInstanceRoot(go))
            {
                continue;
            }
            if (!PrefabUtility.HasPrefabInstanceAnyOverrides(go, false))
            {
                continue;
            }
            yield return go;
        }
    }

    static int CountOverridesOnRoot(GameObject instanceRoot)
    {
        int count = 0;

        List<ObjectOverride> objectOverrides = PrefabUtility.GetObjectOverrides(instanceRoot, false);
        if (objectOverrides != null)
        {
            foreach (ObjectOverride objectOverride in objectOverrides)
            {
                if (objectOverride == null || objectOverride.instanceObject == null)
                {
                    continue;
                }
                if (HasApplicableOverride(objectOverride.instanceObject))
                {
                    count++;
                }
            }
        }

        count += PrefabUtility.GetAddedComponents(instanceRoot)?.Count ?? 0;
        count += PrefabUtility.GetRemovedComponents(instanceRoot)?.Count ?? 0;
        count += PrefabUtility.GetAddedGameObjects(instanceRoot)?.Count ?? 0;
        return count;
    }

    static void CollectFromHierarchy(GameObject root, List<OverrideEntry> result)
    {
        foreach (GameObject nearestRoot in EnumerateOverriddenInstanceRoots(root))
        {
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

            GameObject outermostRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(nearestRoot);
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

}
