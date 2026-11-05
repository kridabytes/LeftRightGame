using Edlore.GamePages;
using UnityEngine;
using UnityEngine.UI;

public class RoomInvitesManager : BasePage
{
    public Button closeButton;

    private void OnEnable()
    {
        GameEvents.RoomInvitesPageShow += OnShowPage;

        closeButton.onClick.AddListener(OnClosePanel);
    }

    private void OnDisable()
    {
        GameEvents.RoomInvitesPageShow -= OnShowPage;

        closeButton.onClick.RemoveAllListeners();
    }

    private void OnShowPage()
    {
        ShowPage();
    }

    private void OnClosePanel()
    {
        GameEvents.OnPlaySound();
        HidePage();
    }
}
