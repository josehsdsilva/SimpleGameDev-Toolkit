using UnityEditor;
using UnityEngine.UI;

public static class RaycastTargetTool
{
    public static void SetTrue()
    {
        foreach (var obj in Selection.gameObjects)
        {
            var graphics = obj.GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in graphics)
            {
                Undo.RecordObject(graphic, "Ativar Raycast Target");
                graphic.raycastTarget = true;
            }
        }

        EditorUtility.DisplayDialog("Concluído",
            $"Raycast Target ativado em {Selection.gameObjects.Length} objeto(s) e seus filhos.",
            "OK");
    }

    public static void SetFalse()
    {
        foreach (var obj in Selection.gameObjects)
        {
            var graphics = obj.GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in graphics)
            {
                Undo.RecordObject(graphic, "Desativar Raycast Target");
                graphic.raycastTarget = false;
            }
        }

        EditorUtility.DisplayDialog("Concluído",
            $"Raycast Target desativado em {Selection.gameObjects.Length} objeto(s) e seus filhos.",
            "OK");
    }

    public static void SetFalseExceptButtons()
    {
        int count = 0;
        foreach (var obj in Selection.gameObjects)
        {
            var graphics = obj.GetComponentsInChildren<Graphic>(true);
            foreach (var graphic in graphics)
            {
                if (graphic.GetComponent<Button>() != null) continue;

                Undo.RecordObject(graphic, "Desativar Raycast Target");
                graphic.raycastTarget = false;
                count++;
            }
        }

        EditorUtility.DisplayDialog("Concluído",
            $"Raycast Target desativado em {count} componente(s), exceto Buttons.",
            "OK");
    }
}
