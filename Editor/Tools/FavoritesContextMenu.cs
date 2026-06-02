using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class FavoritesContextMenu
{
    private const string FAVORITES_FILE = "Library/FavoritesData.json";
    
    [System.Serializable]
    public class FavoritesData
    {
        public List<string> favoritePaths = new List<string>();
    }
    
    [MenuItem("Assets/Add to Favorites", false, 20)]
    public static void AddToFavorites()
    {
        var selectedObjects = Selection.objects;
        
        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select an asset to add to favorites.", "OK");
            return;
        }
        
        var data = LoadFavoritesData();
        int addedCount = 0;
        
        foreach (var obj in selectedObjects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path))
            {
                // For scenes, make sure we're working with the correct asset
                if (path.EndsWith(".unity"))
                {
                    var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                    if (sceneAsset != null && !data.favoritePaths.Contains(path))
                    {
                        data.favoritePaths.Add(path);
                        addedCount++;
                    }
                }
                else if (!data.favoritePaths.Contains(path))
                {
                    data.favoritePaths.Add(path);
                    addedCount++;
                }
            }
        }
        
        if (addedCount > 0)
        {
            SaveFavoritesData(data);
            
            // Refresh the favorites window if it's open
            RefreshFavoritesWindow();
        }
        else
        {
            EditorUtility.DisplayDialog("Already in Favorites", 
                "The selected item(s) are already in your favorites.", "OK");
        }
    }
    
    [MenuItem("Assets/Remove from Favorites", false, 21)]
    public static void RemoveFromFavorites()
    {
        var selectedObjects = Selection.objects;
        
        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select an asset to remove from favorites.", "OK");
            return;
        }
        
        var data = LoadFavoritesData();
        int removedCount = 0;
        
        foreach (var obj in selectedObjects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && data.favoritePaths.Contains(path))
            {
                data.favoritePaths.Remove(path);
                removedCount++;
            }
        }
        
        if (removedCount > 0)
        {
            SaveFavoritesData(data);
            
            // Refresh the favorites window if it's open
            RefreshFavoritesWindow();
            
            EditorUtility.DisplayDialog("Favorites Updated", 
                $"Removed {removedCount} item(s) from favorites.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Not in Favorites", 
                "The selected item(s) are not in your favorites.", "OK");
        }
    }
    
    [MenuItem("Assets/Add to Favorites", true)]
    public static bool ValidateAddToFavorites()
    {
        return Selection.objects.Length > 0;
    }
    
    [MenuItem("Assets/Remove from Favorites", true)]
    public static bool ValidateRemoveFromFavorites()
    {
        if (Selection.objects.Length == 0) return false;
        
        var data = LoadFavoritesData();
        foreach (var obj in Selection.objects)
        {
            var path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path))
            {
                // Check if this path exists in favorites
                if (data.favoritePaths.Contains(path))
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    private static void RefreshFavoritesWindow()
    {
        // Try to find and refresh the favorites window if it's open
        var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
        foreach (var window in windows)
        {
            if (window.GetType().Name == "FavoritesTab")
            {
                // Force reload of favorites data
                var loadMethod = window.GetType().GetMethod("LoadFavorites", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (loadMethod != null)
                {
                    loadMethod.Invoke(window, null);
                }
                window.Repaint();
                break;
            }
        }
    }
    
    private static FavoritesData LoadFavoritesData()
    {
        if (File.Exists(FAVORITES_FILE))
        {
            try
            {
                var json = File.ReadAllText(FAVORITES_FILE);
                return JsonUtility.FromJson<FavoritesData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading favorites: {e.Message}");
            }
        }
        
        return new FavoritesData();
    }
    
    private static void SaveFavoritesData(FavoritesData data)
    {
        try
        {
            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(FAVORITES_FILE, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving favorites: {e.Message}");
        }
    }
}