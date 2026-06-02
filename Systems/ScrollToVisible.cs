namespace SimpleGameDev
{
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollToVisible : MonoBehaviour
{
    private ScrollRect scrollRect;
    private RectTransform targetRect;
    
    private void Awake()
    {
        scrollRect = GetComponentInParent<ScrollRect>();
        targetRect = transform as RectTransform;
    }
    
    public void Scroll()
    {
        if (scrollRect == null) return;
        
        Canvas.ForceUpdateCanvases();
        
        RectTransform contentRect = scrollRect.content;
        RectTransform viewportRect = scrollRect.viewport;
        
        // Scroll vertical
        if (scrollRect.vertical)
        {
            float viewportHeight = viewportRect.rect.height;
            float contentHeight = contentRect.rect.height;
            
            // Posições em espaço do viewport
            Vector3[] itemCorners = new Vector3[4];
            targetRect.GetWorldCorners(itemCorners);
            
            Vector3[] viewportCorners = new Vector3[4];
            viewportRect.GetWorldCorners(viewportCorners);
            
            // Converte para espaço local do viewport
            float itemTop = viewportRect.InverseTransformPoint(itemCorners[1]).y;      // Top-left corner
            float itemBottom = viewportRect.InverseTransformPoint(itemCorners[0]).y;   // Bottom-left corner
            
            float viewportTop = viewportHeight / 2;
            float viewportBottom = -viewportHeight / 2;
            
            Vector2 newPos = contentRect.anchoredPosition;
            
            // Se o topo do item está cortado (acima do viewport)
            if (itemTop > viewportTop)
            {
                float diff = itemTop - viewportTop;
                newPos.y -= diff;
            }
            // Se o fundo do item está cortado (abaixo do viewport)
            else if (itemBottom < viewportBottom)
            {
                float diff = viewportBottom - itemBottom;
                newPos.y += diff;
            }
            
            // CLAMP: limita aos bounds válidos
            float minY = 0;
            float maxY = Mathf.Max(0, contentHeight - viewportHeight);
            newPos.y = Mathf.Clamp(newPos.y, minY, maxY);
            
            contentRect.anchoredPosition = newPos;
        }
        
        // Scroll horizontal
        if (scrollRect.horizontal)
        {
            float viewportWidth = viewportRect.rect.width;
            float contentWidth = contentRect.rect.width;
            
            Vector3[] itemCorners = new Vector3[4];
            targetRect.GetWorldCorners(itemCorners);
            
            Vector3[] viewportCorners = new Vector3[4];
            viewportRect.GetWorldCorners(viewportCorners);
            
            float itemLeft = viewportRect.InverseTransformPoint(itemCorners[0]).x;
            float itemRight = viewportRect.InverseTransformPoint(itemCorners[2]).x;
            
            float viewportLeft = -viewportWidth / 2;
            float viewportRight = viewportWidth / 2;
            
            Vector2 newPos = contentRect.anchoredPosition;
            
            if (itemLeft < viewportLeft)
            {
                float diff = viewportLeft - itemLeft;
                newPos.x += diff;
            }
            else if (itemRight > viewportRight)
            {
                float diff = itemRight - viewportRight;
                newPos.x -= diff;
            }
            
            float minX = -Mathf.Max(0, contentWidth - viewportWidth);
            float maxX = 0;
            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
            
            contentRect.anchoredPosition = newPos;
        }
    }
}

}
