using System;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMessageManager : NetworkBehaviour
{
    public static event Action<PlayerRef, string> OnMessageReceived;
    public TMP_InputField messageInputField;
    public Button sendButton;

    [Header("Prefabs")]
    public MessageController yourSidePrefab;
    public MessageController opponentSidePrefab;

    public Transform messageContainer;

    private void OnEnable()
    {
        sendButton.onClick.AddListener(SendMessage);
        OnMessageReceived += HandleMessageReceived;
        ClearMessages();
    }

    private void OnDisable()
    {
        sendButton.onClick.RemoveListener(SendMessage);
        OnMessageReceived -= HandleMessageReceived;
        ClearMessages();
    }

    private void SendMessage()
    {
        string message = messageInputField.text;
        if (string.IsNullOrWhiteSpace(message))
            return;

        messageInputField.text = string.Empty; // Clear the input field after sending
        RPC_SendMessage(message);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SendMessage(string message, RpcInfo info = default)
    {
        Debug.Log($"Message from {info.Source}: {message}");

        OnMessageReceived?.Invoke(info.Source, message);
    }

    private void HandleMessageReceived(PlayerRef sender, string message)
    {
        // Determine which prefab to instantiate based on the sender
        MessageController prefabToInstantiate =
            (sender == Runner.LocalPlayer) ? yourSidePrefab : opponentSidePrefab;

        // Instantiate the appropriate prefab
        MessageController messageInstance = Instantiate(prefabToInstantiate, messageContainer);

        string senderName = GetPlayerName(sender);
        // Set the message text
        messageInstance.DisplayMessage(message, senderName);
    }

    private string GetPlayerName(PlayerRef player)
    {
        if (Runner.TryGetPlayerObject(player, out NetworkObject playerObject))
        {
            PlayerNameSync playerNameSync = playerObject.GetComponent<PlayerNameSync>();

            if (playerNameSync != null)
                return playerNameSync.PlayerName.ToString();
        }

        return "Player";
    }

    private void ClearMessages()
    {
        foreach (Transform child in messageContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
