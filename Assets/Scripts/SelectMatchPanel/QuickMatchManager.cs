using Edlore.GamePages;
using Edlore.Utilities;
using UnityEngine;
using UnityEngine.UI;

public class QuickMatchManager : BasePage
{
    public Button backButton;
    public GameObject playerListContainer;

    private void Awake()
    {
        PageFactory.Add(this);
    }

    private void OnEnable()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBack);
    }

    private void OnDisable()
    {
        if (backButton != null)
            backButton.onClick.RemoveAllListeners();
    }

    public void Open()
    {
        ShowPage();
    }

    private async void OnBack()
    {
        GameEvents.OnPlaySound();
        HidePage();
        if (FusionConnectionManager.Instance != null)
        {
            await FusionConnectionManager.Instance.Disconnect();
        }

        if (playerListContainer != null)
        {
            foreach (Transform child in playerListContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
