using UnityEditor;
using UnityEngine;

public static class RenameToClassNameTool
{
    [MenuItem("CONTEXT/MonoBehaviour/Rename GameObject to Class Name")]
    private static void RenameToClassName(MenuCommand command)
    {
        var component = command.context as MonoBehaviour;
        if (component == null) return;

        var go = component.gameObject;
        var newName = component.GetType().Name;

        Undo.RecordObject(go, "Rename GameObject to Class Name");
        go.name = newName;
    }
}
