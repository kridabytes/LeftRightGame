using TMPro;
using UnityEngine;

public class MessageController : MonoBehaviour
{
    public TMP_Text messageText;
    public TMP_Text senderNameText;

    public void DisplayMessage(string message, string senderName = "Player")
    {
        messageText.text = message;
        senderNameText.text = senderName;
    }
}
