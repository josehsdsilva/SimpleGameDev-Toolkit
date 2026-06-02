namespace SimpleGameDev.Editor
{
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ColorPaletteWindow : EditorWindow
{
    private ColorPalette currentPalette;
    private Vector2 scrollPosition;
    private Color selectedColor = Color.white;
    private string newColorName = "New Color";
    
    public static void ShowWindow()
    {
        ColorPaletteWindow window = GetWindow<ColorPaletteWindow>("Color Palette Manager");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }
    
    void OnGUI()
    {
        EditorGUILayout.Space(10);
        
        // Título
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 16;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField("Color Palette Manager", titleStyle);
        
        EditorGUILayout.Space(10);
        
        // Seleção da paleta
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Palette Asset", EditorStyles.boldLabel);
        ColorPalette newPalette = (ColorPalette)EditorGUILayout.ObjectField(
            currentPalette, typeof(ColorPalette), false);
        
        if (newPalette != currentPalette)
        {
            currentPalette = newPalette;
        }
        
        // Botão para criar nova paleta
        if (GUILayout.Button("Create New Palette", GUILayout.Height(25)))
        {
            CreateNewPalette();
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        if (currentPalette == null)
        {
            EditorGUILayout.HelpBox("Select or create a Color Palette to start.", 
                MessageType.Info);
            return;
        }
        
        // Seção de adicionar cor
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Add New Color", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Name:", GUILayout.Width(50));
        newColorName = EditorGUILayout.TextField(newColorName, GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Color:", GUILayout.Width(50));
        selectedColor = EditorGUILayout.ColorField(
            GUIContent.none, selectedColor, true, true, false, GUILayout.Width(150));
        
        // Preview da cor selecionada
        Rect previewRect = GUILayoutUtility.GetRect(60, 20);
        EditorGUI.DrawRect(previewRect, selectedColor);
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button("Add to Palette", GUILayout.Height(30)))
        {
            AddColorToPalette();
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Lista de cores
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"Colors in Palette ({currentPalette.colors.Count})", 
            EditorStyles.boldLabel);
        
        if (currentPalette.colors.Count > 0)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, 
                GUILayout.Height(250));
            
            for (int i = 0; i < currentPalette.colors.Count; i++)
            {
                EditorGUILayout.BeginHorizontal("box");
                
                // Preview grande da cor
                Rect colorPreview = GUILayoutUtility.GetRect(50, 50);
                EditorGUI.DrawRect(colorPreview, currentPalette.colors[i].color);
                
                EditorGUILayout.BeginVertical();
                
                // Nome
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Name:", GUILayout.Width(50));
                currentPalette.colors[i].name = EditorGUILayout.TextField(
                    currentPalette.colors[i].name);
                EditorGUILayout.EndHorizontal();
                
                // Color picker
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Color:", GUILayout.Width(50));
                currentPalette.colors[i].color = EditorGUILayout.ColorField(
                    GUIContent.none, currentPalette.colors[i].color, 
                    true, true, false);
                EditorGUILayout.EndHorizontal();
                
                // Hex value
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Hex:", GUILayout.Width(50));
                string hexValue = ColorUtility.ToHtmlStringRGBA(currentPalette.colors[i].color);
                string newHex = EditorGUILayout.TextField("#" + hexValue);
                
                // Atualizar cor se hex mudou
                if (newHex != "#" + hexValue && newHex.Length >= 7)
                {
                    Color newColor;
                    if (ColorUtility.TryParseHtmlString(newHex, out newColor))
                    {
                        currentPalette.colors[i].color = newColor;
                        EditorUtility.SetDirty(currentPalette);
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                // RGB values
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("RGB:", GUILayout.Width(50));
                EditorGUILayout.LabelField(
                    $"R:{(int)(currentPalette.colors[i].color.r * 255)} " +
                    $"G:{(int)(currentPalette.colors[i].color.g * 255)} " +
                    $"B:{(int)(currentPalette.colors[i].color.b * 255)}",
                    EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
                
                // Botões de ação
                EditorGUILayout.BeginVertical(GUILayout.Width(80));
                
                if (GUILayout.Button("Copy", GUILayout.Height(20)))
                {
                    selectedColor = currentPalette.colors[i].color;
                    ShowNotification(new GUIContent("Color copied!"));
                }
                
                if (GUILayout.Button("Duplicate", GUILayout.Height(20)))
                {
                    DuplicateColor(i);
                }
                
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Delete", GUILayout.Height(20)))
                {
                    DeleteColor(i);
                    break;
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("No colors in this palette yet.", MessageType.Info);
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(10);
        
        // Botões de utilidade
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Add Common UI Colors", GUILayout.Height(30)))
        {
            AddCommonUIColors();
        }
        
        GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
        if (GUILayout.Button("Clear All", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Clear Palette", 
                "Remove all colors from this palette?", "Yes", "Cancel"))
            {
                Undo.RecordObject(currentPalette, "Clear Palette");
                currentPalette.colors.Clear();
                EditorUtility.SetDirty(currentPalette);
            }
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.EndHorizontal();
        
        if (GUI.changed && currentPalette != null)
        {
            EditorUtility.SetDirty(currentPalette);
        }
    }
    
    private void CreateNewPalette()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "Create Color Palette",
            "NewColorPalette",
            "asset",
            "Choose where to save the palette");
        
        if (!string.IsNullOrEmpty(path))
        {
            ColorPalette newPalette = CreateInstance<ColorPalette>();
            AssetDatabase.CreateAsset(newPalette, path);
            AssetDatabase.SaveAssets();
            currentPalette = newPalette;
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = newPalette;
        }
    }
    
    private void AddColorToPalette()
    {
        if (currentPalette != null)
        {
            Undo.RecordObject(currentPalette, "Add Color");
            currentPalette.colors.Add(new ColorEntry(newColorName, selectedColor));
            EditorUtility.SetDirty(currentPalette);
            newColorName = "New Color";
        }
    }
    
    private void DuplicateColor(int index)
    {
        if (currentPalette != null && index < currentPalette.colors.Count)
        {
            Undo.RecordObject(currentPalette, "Duplicate Color");
            ColorEntry original = currentPalette.colors[index];
            currentPalette.colors.Insert(index + 1, 
                new ColorEntry(original.name + " Copy", original.color));
            EditorUtility.SetDirty(currentPalette);
        }
    }
    
    private void DeleteColor(int index)
    {
        if (currentPalette != null && index < currentPalette.colors.Count)
        {
            Undo.RecordObject(currentPalette, "Delete Color");
            currentPalette.colors.RemoveAt(index);
            EditorUtility.SetDirty(currentPalette);
        }
    }
    
    private void AddCommonUIColors()
    {
        if (currentPalette == null) return;
        
        Undo.RecordObject(currentPalette, "Add Common UI Colors");
        
        currentPalette.colors.Add(new ColorEntry("Primary Blue", new Color(0.2f, 0.4f, 0.8f)));
        currentPalette.colors.Add(new ColorEntry("Success Green", new Color(0.2f, 0.7f, 0.3f)));
        currentPalette.colors.Add(new ColorEntry("Warning Yellow", new Color(1f, 0.8f, 0.2f)));
        currentPalette.colors.Add(new ColorEntry("Danger Red", new Color(0.9f, 0.2f, 0.2f)));
        currentPalette.colors.Add(new ColorEntry("Dark Background", new Color(0.15f, 0.15f, 0.15f)));
        currentPalette.colors.Add(new ColorEntry("Light Text", new Color(0.95f, 0.95f, 0.95f)));
        
        EditorUtility.SetDirty(currentPalette);
        ShowNotification(new GUIContent("Added 6 common UI colors!"));
    }
}

}
