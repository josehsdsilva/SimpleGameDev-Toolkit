namespace SimpleGameDev.Editor
{
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class RenameButtonFromTextTool
{
    [MenuItem("GameObject/Rename Button from Text Content", false, 1000)]
    private static void RenameButtonFromText()
    {
        GameObject selected = Selection.activeGameObject;
        Transform parent = selected.transform.parent;

        if (parent == null)
        {
            EditorUtility.DisplayDialog("Erro", "O objeto selecionado nao tem parent.", "OK");
            return;
        }

        string textContent = GetTextContent(selected).Trim().Replace(" ", "");
        if (string.IsNullOrEmpty(textContent))
        {
            EditorUtility.DisplayDialog("Erro", "O componente de texto esta vazio.", "OK");
            return;
        }

        Undo.RecordObject(parent.gameObject, "Rename Button from Text Content");
        Undo.RecordObject(selected, "Rename Button from Text Content");

        parent.gameObject.name = textContent + "Button";
        selected.name = textContent + "Text";
    }

    [MenuItem("GameObject/Rename Button from Text Content", true)]
    private static bool Validate()
    {
        return Selection.activeGameObject != null
            && !string.IsNullOrEmpty(GetTextContent(Selection.activeGameObject));
    }

    private static string GetTextContent(GameObject go)
    {
        foreach (Component component in go.GetComponents<Component>())
        {
            if (component == null) continue;
            PropertyInfo prop = component.GetType().GetProperty("text",
                BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.PropertyType == typeof(string))
                return prop.GetValue(component) as string ?? "";
        }
        return "";
    }
}

}
