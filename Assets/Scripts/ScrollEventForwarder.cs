using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Forwards pointer drag events from a child control to its parent ScrollRect,
/// so lists stay scrollable when the drag starts on a button, toggle or dropdown.
/// </summary>
public static class ScrollEventForwarder
{
    public static void Forward<T>(ScrollRect parentScroll, PointerEventData eventData, ExecuteEvents.EventFunction<T> handler)
        where T : IEventSystemHandler
    {
        if (parentScroll != null)
        {
            ExecuteEvents.ExecuteHierarchy(parentScroll.gameObject, eventData, handler);
        }
    }
}
