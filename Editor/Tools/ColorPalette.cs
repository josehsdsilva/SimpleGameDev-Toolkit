namespace SimpleGameDev.Editor
{
using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SimpleGameDev/New Color Palette", menuName = "Color Palette")]
public class ColorPalette : ScriptableObject
{
    [SerializeField]
    public List<ColorEntry> colors = new List<ColorEntry>();
}

[Serializable]
public class ColorEntry
{
    public string name;
    public Color color;
    
    public ColorEntry(string name, Color color)
    {
        this.name = name;
        this.color = color;
    }
}

}
