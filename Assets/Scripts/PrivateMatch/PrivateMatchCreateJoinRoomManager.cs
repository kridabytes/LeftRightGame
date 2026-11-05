using Edlore.GamePages;
using Edlore.Utilities;
using UnityEngine;
using UnityEngine.UI;

public class PrivateMatchCreateJoinRoomManager : BasePage
{
    [SerializeField]
    private Button createRoomButton;

    [SerializeField]
    private Button joinRoomButton;

    [SerializeField]
    private Button backButton;

    [SerializeField]
    private PrivateMatchJoinRoomManager privateMatchCreateJoinRoom;

    private void Awake()
    {
        PageFactory.Add(this);
    }

    private void OnEnable()
    {
        createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
        backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnDisable()
    {
        createRoomButton.onClick.RemoveListener(OnCreateRoomClicked);
        joinRoomButton.onClick.RemoveListener(OnJoinRoomClicked);
        backButton.onClick.RemoveListener(OnBackClicked);
    }

    private async void OnCreateRoomClicked()
    {
        GameEvents.OnPlaySound();
        string roomCode = "Edlore_" + Random.Range(1000, 9999).ToString();
        Debug.Log($"Creating room with code: {roomCode}");

        var selectMatchManager = PageFactory.Get<SelectMatchManager>();
        if (selectMatchManager != null)
            selectMatchManager.loadingScreen.SetActive(true);

        bool isCreated = await FusionConnectionManager.Instance.CreatePrivateMatch(roomCode);

        if (isCreated)
        {
            Debug.Log($"Room created successfully with code: {roomCode}");
            var privateMatchManager = PageFactory.Get<PrivateMatchManager>();
            if (privateMatchManager != null)
                privateMatchManager.Open();

            selectMatchManager.loadingScreen.SetActive(false);
            // You can add your logic here to handle the successful room creation
        }
        else
        {
            Debug.LogWarning("Failed to create room. Please try again.");
            selectMatchManager.loadingScreen.SetActive(false);
            // You can add your logic here to handle the failure case
        }

        HidePage();
    }

    private void OnJoinRoomClicked()
    {
        GameEvents.OnPlaySound();
        privateMatchCreateJoinRoom.ShowPage();
        HidePage();
    }

    private void OnBackClicked()
    {
        GameEvents.OnPlaySound();
        HidePage();
    }
}
