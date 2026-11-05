using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScrollSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [SerializeField]
    private ScrollRect scrollRect;

    [SerializeField]
    private RectTransform content;

    [SerializeField]
    private float snapSpeed = 12f;

    [SerializeField]
    private float swipeThreshold = 50f;

    private float dragStartX;
    private Coroutine snapCoroutine;

    private void Awake()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();

        if (content == null)
            content = scrollRect.content;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragStartX = eventData.position.x;

        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float dragDistance = eventData.position.x - dragStartX;
        int nearestIndex = GetNearestIndex();

        if (Mathf.Abs(dragDistance) >= swipeThreshold)
        {
            // Swipe left = next item, swipe right = previous item.
            nearestIndex += dragDistance < 0 ? 1 : -1;
        }

        nearestIndex = Mathf.Clamp(nearestIndex, 0, content.childCount - 1);

        snapCoroutine = StartCoroutine(SnapToItem(nearestIndex));
    }

    private int GetNearestIndex()
    {
        RectTransform viewport =
            scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.GetComponent<RectTransform>();

        float centerX = viewport.TransformPoint(viewport.rect.center).x;

        int nearestIndex = 0;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < content.childCount; i++)
        {
            RectTransform item = content.GetChild(i) as RectTransform;

            float itemX = item.TransformPoint(item.rect.center).x;
            float distance = Mathf.Abs(itemX - centerX);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private IEnumerator SnapToItem(int index)
    {
        scrollRect.velocity = Vector2.zero;

        RectTransform viewport =
            scrollRect.viewport != null
                ? scrollRect.viewport
                : scrollRect.GetComponent<RectTransform>();

        RectTransform item = content.GetChild(index) as RectTransform;

        while (true)
        {
            float itemX = item.TransformPoint(item.rect.center).x;
            float viewportX = viewport.TransformPoint(viewport.rect.center).x;

            float difference = itemX - viewportX;

            if (Mathf.Abs(difference) < 0.5f)
                break;

            float movement = difference * snapSpeed * Time.unscaledDeltaTime;

            content.anchoredPosition += Vector2.left * movement;

            yield return null;
        }

        snapCoroutine = null;
    }
}
