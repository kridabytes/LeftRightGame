using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Button that only fires on a genuine tap and forwards drag gestures
/// to the parent ScrollRect so the list can still be scrolled.
/// </summary>
public class SmartButtonHandler
    : Button,
        IPointerDownHandler,
        IPointerUpHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
{
    private const float MAX_CLICK_TIME = 0.5f; // seconds allowed for a click

    private Button button;
    private ScrollRect parentScroll;
    private Vector2 startPos;
    private float startTime;
    private bool isClick;

    void Awake()
    {
        button = GetComponent<Button>();
        parentScroll = GetComponentInParent<ScrollRect>();
    }

    #region Click Handling

    public void OnPointerDown(PointerEventData eventData)
    {
        startPos = eventData.position;
        startTime = Time.time;
        isClick = true;
    }

    // Clicks are raised from OnPointerUp instead.
    public override void OnPointerClick(PointerEventData eventData) { }

    public override void OnPointerUp(PointerEventData eventData)
    {
        float elapsed = Time.time - startTime;
        if (isClick && elapsed <= MAX_CLICK_TIME)
        {
            Debug.Log("✅ Smart Button Click Detected on: " + gameObject.name);
            button.onClick.Invoke();
        }
    }

    #endregion

    #region Drag Forwarding

    public void OnBeginDrag(PointerEventData eventData)
    {
        ScrollEventForwarder.Forward(parentScroll, eventData, ExecuteEvents.beginDragHandler);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Vector2.Distance(startPos, eventData.position) > 0f)
        {
            isClick = false;
            ScrollEventForwarder.Forward(parentScroll, eventData, ExecuteEvents.dragHandler);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ScrollEventForwarder.Forward(parentScroll, eventData, ExecuteEvents.endDragHandler);
    }

    #endregion
}
