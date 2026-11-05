using Edlore.GamePages;
using Edlore.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrivateMatchManager : BasePage
{
    public Button backButton;
    public Button settingsButton;
    public Button startMatchButton;
    public TMP_Text roomCodeText;
    public TMP_Text playerCountText;

    private void Awake()
    {
        PageFactory.Add(this);
    }

    private void OnEnable()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBack);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettings);

        if (startMatchButton != null)
            startMatchButton.onClick.AddListener(OnStartMatch);

        RefreshSettingsButtonVisibility();
        RefreshStartButtonVisibility();
    }

    private void OnDisable()
    {
        if (backButton != null)
            backButton.onClick.RemoveAllListeners();

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettings);

        if (startMatchButton != null)
            startMatchButton.onClick.RemoveListener(OnStartMatch);
    }

    public void Open()
    {
        Debug.Log("PrivateMatchManager Opened");
        RefreshRoomCode();
        RefreshSettingsButtonVisibility();
        RefreshStartButtonVisibility();
        ShowPage();
    }

    private void RefreshRoomCode()
    {
        if (roomCodeText == null)
            return;

        FusionConnectionManager connectionManager = FusionConnectionManager.Instance;
        string roomCode =
            connectionManager != null && connectionManager.IsPrivateMatch
                ? connectionManager.GetSessionName()
                : string.Empty;

        roomCodeText.text = string.IsNullOrEmpty(roomCode)
            ? string.Empty
            : $"Room Code: {roomCode}";
    }

    private void RefreshSettingsButtonVisibility()
    {
        if (settingsButton != null)
            settingsButton.gameObject.SetActive(IsHost());
    }

    private void RefreshStartButtonVisibility()
    {
        if (startMatchButton != null)
            startMatchButton.gameObject.SetActive(IsHost());
    }

    private bool IsHost()
    {
        return FusionConnectionManager.Instance != null && FusionConnectionManager.Instance.IsHost;
    }

    private async void OnBack()
    {
        GameEvents.OnPlaySound();
        HidePage();
        if (FusionConnectionManager.Instance != null)
        {
            await FusionConnectionManager.Instance.Disconnect();
        }
    }

    private void OnSettings()
    {
        if (!IsHost())
            return;

        GameEvents.OnPlaySound();
        var settingsManager = PageFactory.Get<MatchSettingsManager>();
        if (settingsManager != null)
            settingsManager.OpenSettings();
        // Implement settings logic here
    }

    private void OnStartMatch()
    {
        if (!IsHost())
            return;

        GameEvents.OnPlaySound();
        FusionConnectionManager connectionManager = FusionConnectionManager.Instance;
        if (connectionManager == null || !connectionManager.IsPrivateMatch)
        {
            Debug.LogError("[PrivateMatch] Cannot start because the private room is unavailable.");
            return;
        }

        connectionManager.StartPrivateMatch();
    }
}
