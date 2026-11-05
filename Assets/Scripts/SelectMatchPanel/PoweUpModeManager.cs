using Edlore.GamePages;
using Edlore.Utilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PoweUpModeManager : BasePage
{
    public Button backButton;

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

    private void OnBack()
    {
        GameEvents.OnPlaySound();
        HidePage();
    }
}
