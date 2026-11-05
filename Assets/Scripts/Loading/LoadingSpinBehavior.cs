using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingSpinBehavior : MonoBehaviour
{
    public Image loaderImage;
    bool isLoading = false;

    private void OnEnable()
    {
        isLoading = true;
        StartCoroutine(RotateLoadingImage());
    }

    IEnumerator RotateLoadingImage()
    {
        float rotationSpeed = 300f;

        loaderImage.gameObject.SetActive(true);
        while (isLoading)
        {
            // Rotate the image over time
            loaderImage.rectTransform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
            yield return null; // Wait for the next frame
        }
        loaderImage.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        isLoading = false;
        StopCoroutine(RotateLoadingImage());
    }
}
