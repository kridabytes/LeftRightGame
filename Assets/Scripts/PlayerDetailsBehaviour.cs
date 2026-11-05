using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDetailsBehaviour : MonoBehaviour
{
    public TMP_Text playerNameText;
    public Image playerImagel;

    public CharacterData characterDetails;

    public void Init(string playerName, int playerProfile)
    {
        playerNameText.text = playerName;

        // Assuming you have a method to get the sprite based on the profile index
        playerImagel.sprite = GetProfileSprite(playerProfile);
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
