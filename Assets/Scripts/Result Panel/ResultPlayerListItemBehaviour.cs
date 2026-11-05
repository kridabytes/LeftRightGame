using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultPlayerListItemBehaviour : MonoBehaviour
{
    public TMP_Text playerNameText;
    public TMP_Text playerScoreText;
    public Image playerImage;
    public CharacterData characterDetails;

    public void Init(PlayerInfo playerInfo)
    {
        playerNameText.text = playerInfo.PlayerName.ToString();
        playerScoreText.text = $"Score: {playerInfo.PlayerScore}";

        // Assuming you have a method to get the sprite based on the profile index
        playerImage.sprite = GetProfileSprite(playerInfo.PlayerProfile);
    }

    private Sprite GetProfileSprite(int profileIndex)
    {
        if (
            characterDetails != null
            && characterDetails.charaacterDetails != null
            && profileIndex >= 0
            && profileIndex < characterDetails.charaacterDetails.Count
        )
        {
            return characterDetails.charaacterDetails[profileIndex].characterImage;
        }
        else
        {
            Debug.LogWarning($"Invalid profile index: {profileIndex}. Returning default sprite.");
            return characterDetails.charaacterDetails[0].characterImage;
        }
    }
}
