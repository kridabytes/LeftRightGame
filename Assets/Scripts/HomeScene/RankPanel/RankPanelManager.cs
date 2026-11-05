using Edlore.GamePages;
using UnityEngine;
using UnityEngine.UI;

public class RankPanelManager : BasePage
{
    public Button backButton;

    void OnEnable()
    {
        GameEvents.RankPageShow += OnShowPage;

        backButton.onClick.AddListener(OnBackPage);
    }

    void OnDisable()
    {
        GameEvents.RankPageShow -= OnShowPage;

        backButton.onClick.RemoveAllListeners();
    }

    private void OnShowPage()
    {
        ShowPage();
    }

    private void OnBackPage()
    {
        GameEvents.OnPlaySound();
        HidePage();
    }
}
