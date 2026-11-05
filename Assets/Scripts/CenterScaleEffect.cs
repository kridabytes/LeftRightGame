
using UnityEngine;
using UnityEngine.UI;

public class CenterScaleEffect : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;

    [SerializeField, Range(0.5f, 1f)]
    private float sideScale = 0.65f;

    [SerializeField]
    private float centerScale = 1f;

    [SerializeField]
    private float scaleSmoothness = 10f;

    private RectTransform[] items;

    private void Start()
    {
        if (content == null || scrollRect == null)
            return;

        items = new RectTransform[content.childCount];

        for (int i = 0; i < content.childCount; i++)
            items[i] = content.GetChild(i) as RectTransform;
    }

    private void Update()
    {
        if (items == null || scrollRect == null)
            return;

        float centerX = scrollRect.viewport.TransformPoint(
            scrollRect.viewport.rect.center).x;

        float maxDistance = scrollRect.viewport.rect.width * 0.5f;

        foreach (RectTransform item in items)
        {
            float itemX = item.TransformPoint(
                item.rect.center).x;

            float distance = Mathf.Abs(itemX - centerX);
            float normalized = Mathf.Clamp01(distance / maxDistance);

            float scale = Mathf.Lerp(
                centerScale, sideScale, normalized);

            item.localScale = Vector3.Lerp(
                item.localScale,
                Vector3.one * scale,
                Time.unscaledDeltaTime * scaleSmoothness);
        }
    }
}
        