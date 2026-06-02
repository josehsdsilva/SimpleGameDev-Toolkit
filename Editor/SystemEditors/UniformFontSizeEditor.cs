namespace SimpleGameDev.Editor
{
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UniformFontSize))]
public class UniformFontSizeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Refresh Now"))
        {
            var component = (UniformFontSize)target;
            component.Refresh();
        }
    }
}

}
