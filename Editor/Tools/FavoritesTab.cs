namespace SimpleGameDev.Editor
{
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public class FavoritesTab : EditorWindow
{
    [System.NonSerialized]
    private List<FavoriteItem> favoriteItems = new List<FavoriteItem>();
    private Vector2 scrollPosition;
    private string searchFilter = "";

    private const string FAVORITES_FILE = "Library/FavoritesData.json";

    private int draggedIndex = -1;
    [System.NonSerialized]
    private FavoriteItem draggedFromFolder = null;
    private bool isDraggingItem = false;

    [System.Serializable]
    public class FavoriteItem
    {
        public string path;
        public bool isFolder;
        public string folderName;
        public bool isExpanded;
        public List<FavoriteItem> children = new List<FavoriteItem>();

        [System.NonSerialized]
        [JsonIgnore]
        public Object cachedObject;
    }

    [System.Serializable]
    public class FavoritesData
    {
        public List<FavoriteItem> items = new List<FavoriteItem>();
    }

    public static void ShowWindow()
    {
        GetWindow<FavoritesTab>("Favorites");
    }

    private void OnEnable()
    {
        LoadFavorites();
    }

    private void OnDisable()
    {
        SaveFavorites();
    }

    private void OnGUI()
    {
        DrawHeader();
        DrawSearchBar();
        DrawFavoritesList();
        DrawDropArea();

        HandleDragAndDrop();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Favorites", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("New Folder", GUILayout.Width(80)))
        {
            CreateNewFolder();
        }

        if (GUILayout.Button("Clear All", GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("Clear Favorites", "Are you sure you want to clear all favorites?", "Yes", "No"))
            {
                favoriteItems.Clear();
                SaveFavorites();
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    private void DrawSearchBar()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);

        if (GUILayout.Button("X", GUILayout.Width(20)))
        {
            searchFilter = "";
            GUI.FocusControl(null);
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    private void DrawFavoritesList()
    {
        if (favoriteItems.Count == 0)
        {
            EditorGUILayout.HelpBox("No favorites added yet. Drag assets here to add them to your favorites!", MessageType.Info);
            return;
        }

        // Use BeginScrollView with GUILayout to have full control
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        for (int i = 0; i < favoriteItems.Count; i++)
        {
            DrawFavoriteItem(favoriteItems[i], i, 0, null);
        }

        EditorGUILayout.EndScrollView();
    }

    private void CreateNewFolder()
    {
        var newFolder = new FavoriteItem
        {
            isFolder = true,
            folderName = "New Folder",
            isExpanded = true,
            children = new List<FavoriteItem>()
        };

        favoriteItems.Add(newFolder);
        SaveFavorites();
    }

    private void DrawFavoriteItem(FavoriteItem item, int index, int indentLevel, FavoriteItem parentFolder)
    {
        if (item == null) return;

        // Filter search
        if (!string.IsNullOrEmpty(searchFilter))
        {
            bool matches = false;
            if (item.isFolder)
            {
                matches = item.folderName.ToLower().Contains(searchFilter.ToLower());
            }
            else
            {
                if (item.cachedObject != null)
                {
                    matches = item.cachedObject.name.ToLower().Contains(searchFilter.ToLower());
                }
            }

            if (!matches && (!item.isFolder || !HasMatchingChildren(item)))
            {
                return;
            }
        }

        // Calculate indent: folders stay aligned, items get extra indent
        float indent = 0;
        if (item.isFolder)
        {
            indent = indentLevel * 15f;
        }
        else
        {
            indent = indentLevel * 15f + 15f; // Items are 15px more indented than their parent folder
        }

        // Get a rect for this line
        var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

        // Apply indent by adjusting the rect
        rect.x += indent;
        rect.width -= indent;

        // Calculate handle rect for drag detection
        var handleRect = new Rect(rect.x, rect.y, 10, rect.height);

        if (item.isFolder)
        {
            DrawFolderInRect(item, index, parentFolder, rect);
        }
        else
        {
            DrawAssetInRect(item, index, parentFolder, rect);
        }

        // Handle reordering drag - only from handle
        HandleItemReordering(item, index, parentFolder, handleRect, rect);

        // Draw children if folder is expanded
        if (item.isFolder && item.isExpanded)
        {
            for (int i = 0; i < item.children.Count; i++)
            {
                DrawFavoriteItem(item.children[i], i, indentLevel + 1, item);
            }
        }
    }

    private bool HasMatchingChildren(FavoriteItem folder)
    {
        if (!folder.isFolder) return false;

        foreach (var child in folder.children)
        {
            if (child.isFolder)
            {
                if (child.folderName.ToLower().Contains(searchFilter.ToLower()))
                    return true;
                if (HasMatchingChildren(child))
                    return true;
            }
            else if (child.cachedObject != null)
            {
                if (child.cachedObject.name.ToLower().Contains(searchFilter.ToLower()))
                    return true;
            }
        }

        return false;
    }

    private void DrawFolderInRect(FavoriteItem folder, int index, FavoriteItem parentFolder, Rect rect)
    {
        float xPos = rect.x;

        // Drag handle (3 bars) - for reordering only
        var handleRect = new Rect(xPos, rect.y, 10, rect.height);
        DrawDragHandle(handleRect);
        xPos += 12;

        // Foldout arrow
        var foldoutRect = new Rect(xPos, rect.y, 12, rect.height);
        folder.isExpanded = EditorGUI.Foldout(foldoutRect, folder.isExpanded, "", true);
        xPos += 14;

        // Folder icon
        var iconRect = new Rect(xPos, rect.y, 20, rect.height);
        GUI.Label(iconRect, EditorGUIUtility.IconContent("Folder Icon"));
        xPos += 22;

        // Editable folder name
        var nameWidth = rect.width - (xPos - rect.x) - 25; // 25 for remove button
        var nameRect = new Rect(xPos, rect.y, nameWidth, rect.height);
        GUI.SetNextControlName($"FolderName_{index}");
        var newName = EditorGUI.TextField(nameRect, folder.folderName);
        if (newName != folder.folderName)
        {
            folder.folderName = newName;
            SaveFavorites();
        }
        xPos += nameWidth + 2;

        // Remove button
        var buttonRect = new Rect(xPos, rect.y, 20, rect.height);
        if (GUI.Button(buttonRect, "X"))
        {
            if (EditorUtility.DisplayDialog("Delete Folder",
                $"Delete folder '{folder.folderName}' and all its contents?", "Delete", "Cancel"))
            {
                RemoveItem(folder, parentFolder);
            }
        }

        // Visual feedback when dragging over folder
        var currentEvent = Event.current;
        if (currentEvent.type == EventType.Repaint && rect.Contains(currentEvent.mousePosition))
        {
            if (DragAndDrop.visualMode == DragAndDropVisualMode.Copy ||
                DragAndDrop.visualMode == DragAndDropVisualMode.Move)
            {
                EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 1f, 0.2f));
            }
        }

        HandleDropInFolder(rect, folder);
    }

    private void DrawAssetInRect(FavoriteItem item, int index, FavoriteItem parentFolder, Rect rect)
    {
        // Load cached object if needed
        if (item.cachedObject == null && !string.IsNullOrEmpty(item.path))
        {
            if (item.path.EndsWith(".unity"))
            {
                item.cachedObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(item.path);
            }
            else
            {
                item.cachedObject = AssetDatabase.LoadAssetAtPath<Object>(item.path);
            }
        }

        if (item.cachedObject == null) return;

        float xPos = rect.x;

        // Drag handle (3 bars) - for reordering only
        var handleRect = new Rect(xPos, rect.y, 10, rect.height);
        DrawDragHandle(handleRect);
        xPos += 12;

        // Icon
        var icon = AssetPreview.GetMiniThumbnail(item.cachedObject);
        if (icon != null)
        {
            var iconRect = new Rect(xPos, rect.y, 20, rect.height);
            GUI.Label(iconRect, icon);
            xPos += 22;
        }

        // Custom object field without picker button
        var objectFieldWidth = rect.width - (xPos - rect.x) - 25; // 25 for remove button
        var objectFieldRect = new Rect(xPos, rect.y, objectFieldWidth, rect.height);

        // Draw background like objectField
        var objectFieldStyle = new GUIStyle(EditorStyles.objectField);
        GUI.Box(objectFieldRect, "", objectFieldStyle);

        // Draw object name as label
        var labelRect = new Rect(objectFieldRect.x + 2, objectFieldRect.y, objectFieldRect.width - 4, objectFieldRect.height);
        var labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.alignment = TextAnchor.MiddleLeft;
        GUI.Label(labelRect, item.cachedObject.name, labelStyle);

        // Handle click to ping/open
        if (Event.current.type == EventType.MouseDown && objectFieldRect.Contains(Event.current.mousePosition))
        {
            if (Event.current.button == 0)
            {
                if (Event.current.clickCount == 1)
                {
                    // Single click - ping/highlight in project
                    EditorGUIUtility.PingObject(item.cachedObject);
                    Selection.activeObject = item.cachedObject;
                }
                else if (Event.current.clickCount == 2)
                {
                    // Double click - open the asset
                    AssetDatabase.OpenAsset(item.cachedObject);
                }
                Event.current.Use();
            }
        }

        xPos += objectFieldWidth + 2;

        // Remove button
        var buttonRect = new Rect(xPos, rect.y, 20, rect.height);
        if (GUI.Button(buttonRect, "X"))
        {
            RemoveItem(item, parentFolder);
        }
    }

    private void HandleItemReordering(FavoriteItem item, int index, FavoriteItem parentFolder, Rect handleRect, Rect fullRect)
    {
        var currentEvent = Event.current;

        // Start dragging - ONLY if clicking on the handle
        if (currentEvent.type == EventType.MouseDown && handleRect.Contains(currentEvent.mousePosition))
        {
            if (currentEvent.button == 0)
            {
                draggedIndex = index;
                draggedFromFolder = parentFolder;
                isDraggingItem = true;
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[0];
                DragAndDrop.SetGenericData("DraggedItem", item);
                DragAndDrop.StartDrag(item.isFolder ? "Drag Folder" : "Reorder Item");
                currentEvent.Use();
            }
        }

        // Show drop indicator
        if (isDraggingItem && draggedIndex != -1)
        {
            if (fullRect.Contains(currentEvent.mousePosition))
            {
                var targetList = parentFolder != null ? parentFolder.children : favoriteItems;

                // Draw line indicator
                var lineRect = new Rect(fullRect.x, fullRect.y - 2, fullRect.width, 2);
                EditorGUI.DrawRect(lineRect, Color.cyan);

                if (currentEvent.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    currentEvent.Use();
                }

                if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    var sourceList = draggedFromFolder != null ? draggedFromFolder.children : favoriteItems;

                    if (draggedIndex >= 0 && draggedIndex < sourceList.Count)
                    {
                        var draggedItem = sourceList[draggedIndex];
                        sourceList.RemoveAt(draggedIndex);

                        int targetIndex = index;
                        if (sourceList == targetList && draggedIndex < index)
                        {
                            targetIndex--;
                        }

                        targetList.Insert(targetIndex, draggedItem);
                        SaveFavorites();
                    }

                    draggedIndex = -1;
                    draggedFromFolder = null;
                    isDraggingItem = false;
                    currentEvent.Use();
                    Repaint();
                }
            }
        }

        if (currentEvent.type == EventType.MouseUp || currentEvent.type == EventType.DragExited)
        {
            if (isDraggingItem)
            {
                draggedIndex = -1;
                draggedFromFolder = null;
                isDraggingItem = false;
                currentEvent.Use();
            }
        }
    }

    private void HandleDropInFolder(Rect rect, FavoriteItem folder)
    {
        var currentEvent = Event.current;

        switch (currentEvent.type)
        {
            case EventType.DragUpdated:
                if (rect.Contains(currentEvent.mousePosition))
                {
                    // Check if dragging internal item
                    var draggedItem = DragAndDrop.GetGenericData("DraggedItem") as FavoriteItem;

                    // Prevent dropping folder into itself or its descendants
                    if (draggedItem != null && draggedItem.isFolder)
                    {
                        if (draggedItem == folder || IsDescendantOf(folder, draggedItem))
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                            currentEvent.Use();
                            break;
                        }
                    }

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    currentEvent.Use();
                }
                break;

            case EventType.DragPerform:
                if (rect.Contains(currentEvent.mousePosition))
                {
                    DragAndDrop.AcceptDrag();

                    // Check if dragging internal item (folder or asset within favorites)
                    var draggedItem = DragAndDrop.GetGenericData("DraggedItem") as FavoriteItem;

                    if (draggedItem != null && isDraggingItem)
                    {
                        // Moving internal item into folder
                        var sourceList = draggedFromFolder != null ? draggedFromFolder.children : favoriteItems;

                        // Prevent moving folder into itself or its descendants
                        if (draggedItem.isFolder && (draggedItem == folder || IsDescendantOf(folder, draggedItem)))
                        {
                            currentEvent.Use();
                            break;
                        }

                        sourceList.Remove(draggedItem);
                        folder.children.Add(draggedItem);

                        draggedIndex = -1;
                        draggedFromFolder = null;
                        isDraggingItem = false;
                    }
                    else
                    {
                        // Add dragged assets from project to folder
                        foreach (var draggedObject in DragAndDrop.objectReferences)
                        {
                            AddToFolder(draggedObject, folder);
                        }
                    }

                    currentEvent.Use();
                    SaveFavorites();
                    Repaint();
                }
                break;
        }
    }

    private void RemoveItem(FavoriteItem item, FavoriteItem parentFolder)
    {
        if (parentFolder != null)
        {
            parentFolder.children.Remove(item);
        }
        else
        {
            favoriteItems.Remove(item);
        }
        SaveFavorites();
    }

    private void DrawDropArea()
    {
        EditorGUILayout.Space();

        var dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag assets here to add to favorites");

        switch (Event.current.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(Event.current.mousePosition))
                    break;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (Event.current.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (var draggedObject in DragAndDrop.objectReferences)
                    {
                        AddToFavorites(draggedObject);
                    }
                }
                break;
        }
    }

    private void HandleDragAndDrop()
    {
        if (Event.current.type == EventType.DragUpdated && !isDraggingItem)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }
        else if (Event.current.type == EventType.DragPerform && !isDraggingItem)
        {
            DragAndDrop.AcceptDrag();

            foreach (var draggedObject in DragAndDrop.objectReferences)
            {
                AddToFavorites(draggedObject);
            }

            Event.current.Use();
        }
    }

    private void AddToFavorites(Object obj)
    {
        if (obj == null) return;

        var path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path)) return;

        // Check if already exists
        if (ItemExists(path, favoriteItems)) return;

        var newItem = new FavoriteItem
        {
            path = path,
            isFolder = false,
            cachedObject = obj
        };

        favoriteItems.Add(newItem);
        SaveFavorites();
    }

    private void AddToFolder(Object obj, FavoriteItem folder)
    {
        if (obj == null || !folder.isFolder) return;

        var path = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(path)) return;

        // Check if already exists in folder
        if (ItemExists(path, folder.children)) return;

        var newItem = new FavoriteItem
        {
            path = path,
            isFolder = false,
            cachedObject = obj
        };

        folder.children.Add(newItem);
        SaveFavorites();
    }

    private bool IsDescendantOf(FavoriteItem potentialChild, FavoriteItem potentialParent)
    {
        if (!potentialParent.isFolder) return false;

        foreach (var child in potentialParent.children)
        {
            if (child == potentialChild)
                return true;

            if (child.isFolder && IsDescendantOf(potentialChild, child))
                return true;
        }

        return false;
    }

    private bool ItemExists(string path, List<FavoriteItem> items)
    {
        foreach (var item in items)
        {
            if (!item.isFolder && item.path == path)
                return true;

            if (item.isFolder && ItemExists(path, item.children))
                return true;
        }

        return false;
    }

    private void SaveFavorites()
    {
        var data = new FavoritesData
        {
            items = favoriteItems
        };

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(FAVORITES_FILE, json);
    }

    private void LoadFavorites()
    {
        favoriteItems.Clear();

        if (File.Exists(FAVORITES_FILE))
        {
            try
            {
                string json = File.ReadAllText(FAVORITES_FILE);
                FavoritesData data = JsonConvert.DeserializeObject<FavoritesData>(json);

                if (data != null && data.items != null)
                {
                    favoriteItems = data.items;
                    LoadCachedObjects(favoriteItems);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading favorites: {e.Message}");
            }
        }
    }

    private void LoadCachedObjects(List<FavoriteItem> items)
    {
        foreach (var item in items)
        {
            if (!item.isFolder && !string.IsNullOrEmpty(item.path))
            {
                if (item.path.EndsWith(".unity"))
                {
                    item.cachedObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(item.path);
                }
                else
                {
                    item.cachedObject = AssetDatabase.LoadAssetAtPath<Object>(item.path);
                }
            }

            if (item.isFolder && item.children != null)
            {
                LoadCachedObjects(item.children);
            }
        }
    }

    private void DrawDragHandle(Rect rect)
    {
        // Draw 3 horizontal lines (drag handle)
        var lineColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        var lineHeight = 1f;
        var spacing = 2f;
        var lineWidth = 6f;

        var startX = rect.x + (rect.width - lineWidth) * 0.5f;
        var startY = rect.y + (rect.height - (lineHeight * 3 + spacing * 2)) * 0.5f;

        // Draw 3 lines
        for (int i = 0; i < 3; i++)
        {
            var lineRect = new Rect(startX, startY + i * (lineHeight + spacing), lineWidth, lineHeight);
            EditorGUI.DrawRect(lineRect, lineColor);
        }

        // Change cursor to move cursor when hovering
        EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);
    }
}

}
