namespace SimpleGameDev
{
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollToVisible : MonoBehaviour
{
    private ScrollRect scrollRect;
    private RectTransform targetRect;
    private readonly Vector3[] itemCorners = new Vector3[4];

    
    private void Awake()
    {
        scrollRect = GetComponentInParent<ScrollRect>();
        targetRect = transform as RectTransform;
    }

     public void Scroll(int siblingOffset = 0)
    {
        Transform parent = transform.parent;
        if (parent == null || parent.childCount == 0)
            return;

        int currentIndex = transform.GetSiblingIndex();
        int targetIndex = Mathf.Clamp(currentIndex + siblingOffset, 0, parent.childCount - 1);

        Transform targetSibling = parent.GetChild(targetIndex);

        if (targetSibling.TryGetComponent(out ScrollToVisible targetScroll))
        {
            targetScroll.Scroll();
        }
        else if (targetSibling is RectTransform siblingRect && EnsureScrollRect())
        {
            RectTransform originalTarget = targetRect;
            targetRect = siblingRect;
            Scroll();
            targetRect = originalTarget;
        }
    }
    
    public void ScrollToBottom()
    {
        if (!EnsureScrollRect())
            return;

        Canvas.ForceUpdateCanvases();

        RectTransform contentRect = scrollRect.content;
        RectTransform viewportRect = scrollRect.viewport;
        Vector2 newPos = contentRect.anchoredPosition;

        targetRect.GetWorldCorners(itemCorners);

        if (scrollRect.vertical)
        {
            float viewportHeight = viewportRect.rect.height;
            float halfHeight = viewportHeight * 0.5f;
            float itemBottom = viewportRect.InverseTransformPoint(itemCorners[0]).y;

            newPos.y += -halfHeight - itemBottom;

            float maxY = Mathf.Max(0f, contentRect.rect.height - viewportHeight);
            newPos.y = Mathf.Clamp(newPos.y, 0f, maxY);
        }

        if (scrollRect.horizontal)
        {
            float viewportWidth = viewportRect.rect.width;
            float halfWidth = viewportWidth * 0.5f;
            float itemRight = viewportRect.InverseTransformPoint(itemCorners[2]).x;

            newPos.x -= itemRight - halfWidth;

            float minX = -Mathf.Max(0f, contentRect.rect.width - viewportWidth);
            newPos.x = Mathf.Clamp(newPos.x, minX, 0f);
        }

        contentRect.anchoredPosition = newPos;
    }

    private void Scroll()
    {
        if (!EnsureScrollRect())
            return;

        Canvas.ForceUpdateCanvases();

        RectTransform contentRect = scrollRect.content;
        RectTransform viewportRect = scrollRect.viewport;
        Vector2 newPos = contentRect.anchoredPosition;

        targetRect.GetWorldCorners(itemCorners);

        if (scrollRect.vertical)
        {
            float viewportHeight = viewportRect.rect.height;
            float halfHeight = viewportHeight * 0.5f;

            float itemTop = viewportRect.InverseTransformPoint(itemCorners[1]).y;
            float itemBottom = viewportRect.InverseTransformPoint(itemCorners[0]).y;

            if (itemTop > halfHeight)
                newPos.y -= itemTop - halfHeight;
            else if (itemBottom < -halfHeight)
                newPos.y += -halfHeight - itemBottom;

            float maxY = Mathf.Max(0f, contentRect.rect.height - viewportHeight);
            newPos.y = Mathf.Clamp(newPos.y, 0f, maxY);
        }

        if (scrollRect.horizontal)
        {
            float viewportWidth = viewportRect.rect.width;
            float halfWidth = viewportWidth * 0.5f;

            float itemLeft = viewportRect.InverseTransformPoint(itemCorners[0]).x;
            float itemRight = viewportRect.InverseTransformPoint(itemCorners[2]).x;

            if (itemLeft < -halfWidth)
                newPos.x += -halfWidth - itemLeft;
            else if (itemRight > halfWidth)
                newPos.x -= itemRight - halfWidth;

            float minX = -Mathf.Max(0f, contentRect.rect.width - viewportWidth);
            newPos.x = Mathf.Clamp(newPos.x, minX, 0f);
        }

        contentRect.anchoredPosition = newPos;
    }

    private bool EnsureScrollRect()
    {
        if (scrollRect == null)
            Awake();
        return scrollRect != null;
    }
}

}
