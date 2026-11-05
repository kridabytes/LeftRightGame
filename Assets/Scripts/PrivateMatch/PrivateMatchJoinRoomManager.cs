using Edlore.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrivateMatchJoinRoomManager : Edlore.GamePages.BasePage
{
    [SerializeField]
    private Button backButton;

    [SerializeField]
    private Button joinRoomButton;

    [SerializeField]
    private TMP_InputField roomCodeInputField;

    [SerializeField]
    private TMP_Text errorText;

    private void Awake()
    {
        PageFactory.Add(this);
    }

    private void OnEnable()
    {
        errorText.gameObject.SetActive(false);
        backButton.onClick.AddListener(OnBackClicked);
        joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
    }

    private void OnDisable()
    {
        backButton.onClick.RemoveListener(OnBackClicked);
        joinRoomButton.onClick.RemoveListener(OnJoinRoomClicked);
    }

    private void OnBackClicked()
    {
        GameEvents.OnPlaySound();
        HidePage();
    }

    private async void OnJoinRoomClicked()
    {
        GameEvents.OnPlaySound();
        string roomCode = roomCodeInputField.text;

        var selectMatchManager = PageFactory.Get<SelectMatchManager>();

        if (!string.IsNullOrEmpty(roomCode))
        {
            // Implement the logic to join the room using the provided room code
            Debug.Log($"Joining room with code: {roomCode}");
            if (selectMatchManager != null)
                selectMatchManager.loadingScreen.SetActive(true);
            bool isJoined = await FusionConnectionManager.Instance.JoinPrivateMatch(roomCode);

            if (isJoined)
            {
                Debug.Log($"Successfully joined room with code: {roomCode}");
                // You can add your logic here to handle the successful room join
                var privateMatchManager = PageFactory.Get<PrivateMatchManager>();
                if (privateMatchManager != null)
                    privateMatchManager.Open();

                selectMatchManager.loadingScreen.SetActive(false);

                HidePage();
            }
            else
            {
                Debug.LogWarning(
                    $"Failed to join room with code: {roomCode}. Please check the code and try again."
                );
                selectMatchManager.loadingScreen.SetActive(false);
                // You can add your logic here to handle the failure case
            }

            // You can add your networking logic here to join the room
        }
        else
        {
            Debug.LogWarning("Room code is empty. Please enter a valid room code.");

            if (errorText != null)
            {
                errorText.text = "Please enter a valid room code.";
                errorText.gameObject.SetActive(true);
            }
        }
    }
}
