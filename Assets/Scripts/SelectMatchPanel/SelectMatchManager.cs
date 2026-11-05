using System.Threading.Tasks;
using Edlore.GamePages;
using Edlore.Utilities;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectMatchManager : BasePage
{
    public Button backButoon,
        quickMatchBtn,
        privateMatchBtn,
        powerUpModeButton;

    [Header("Loading")]
    [SerializeField]
    public GameObject loadingScreen;

    private bool isQuickMatchInProgress;

    private void Awake()
    {
        PageFactory.Add(this);
    }

    private void OnEnable()
    {
        quickMatchBtn.onClick.AddListener(OnQuickMatchClicked);
        privateMatchBtn.onClick.AddListener(OpenPrivateMatch);
        powerUpModeButton.onClick.AddListener(OpenPowerUpModePanel);
        backButoon.onClick.AddListener(BackButtonClicked);
    }

    private void OnDisable()
    {
        quickMatchBtn.onClick.RemoveAllListeners();
        privateMatchBtn.onClick.RemoveAllListeners();
        powerUpModeButton.onClick.RemoveAllListeners();
        backButoon.onClick.RemoveAllListeners();
    }

    // private async void OpenQuickMatchPanelAsync()
    // {
    //     // var quickMatchManager = PageFactory.Get<QuickMatchManager>();
    //     // if (quickMatchManager != null)
    //     //     quickMatchManager.Open();
    //     // Quick Match button
    //     await FusionConnectionManager.Instance.QuickMatch();
    // }

    private async void OnQuickMatchClicked()
    {
        GameEvents.OnPlaySound();
        Debug.Log("Quick Match button clicked.");
        if (isQuickMatchInProgress)
            return;
        Debug.Log("Quick Match button clicked. 2");

        isQuickMatchInProgress = true;

        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        quickMatchBtn.interactable = false;

        try
        {
            if (FusionConnectionManager.Instance == null)
            {
                Debug.LogError("FusionConnectionManager instance not found.");
                return;
            }

            bool isConnected = await FusionConnectionManager.Instance.QuickMatch();
            Debug.Log($"QuickMatchManager Quick Match result: {isConnected}");

            if (!isConnected)
                return;

            Debug.Log("QuickMatchManager Opening. Quick Match successful. ");
            var quickMatchManager = PageFactory.Get<QuickMatchManager>();

            if (quickMatchManager != null)
            {
                Debug.Log("QuickMatchManager found. Opening panel.");
                quickMatchManager.Open();
            }
        }
        finally
        {
            if (loadingScreen != null)
                loadingScreen.SetActive(false);

            if (quickMatchBtn != null)
                quickMatchBtn.interactable = true;

            isQuickMatchInProgress = false;
        }
    }

    private void OpenPrivateMatch()
    {
        GameEvents.OnPlaySound();
        // var privateMatchManager = PageFactory.Get<PrivateMatchManager>();
        // if (privateMatchManager != null)
        //     privateMatchManager.Open();

        var privateMatchCreateJoinRoomManager =
            PageFactory.Get<PrivateMatchCreateJoinRoomManager>();
        if (privateMatchCreateJoinRoomManager != null)
            privateMatchCreateJoinRoomManager.ShowPage();
    }

    private void OpenPowerUpModePanel()
    {
        GameEvents.OnPlaySound();
        var powerUpModeManager = PageFactory.Get<PoweUpModeManager>();
        if (powerUpModeManager != null)
            powerUpModeManager.Open();
    }

    private async void BackButtonClicked()
    {
        GameEvents.OnPlaySound();

        if (FusionConnectionManager.Instance != null)
        {
            await FusionConnectionManager.Instance.DisconnectRunner();

            Destroy(FusionConnectionManager.Instance.gameObject);
        }

        SceneManager.LoadScene("HomeScene");
    }
}
