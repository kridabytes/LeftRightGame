using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ForceLayoutRebuild : MonoBehaviour
{
    public bool rebuildOnEnable = false;
    public bool rebuildConstantly = false;
    public RectTransform layoutRoot = null;

    public IEnumerator Start()
    {
        if (rebuildOnEnable)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
            yield return new WaitForEndOfFrame();
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        }
        yield return null;
    }
    private void Update()
    {
        if (rebuildConstantly)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        }
    }
    public void OnEnable()
    {
        if (rebuildOnEnable)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        }
    }

    public void ForceRebuild()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
    }
}
