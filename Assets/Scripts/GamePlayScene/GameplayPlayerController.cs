using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GameplayPlayerController : MonoBehaviour
{
    [SerializeField]
    private TMP_Text playerNameText;

    [SerializeField]
    private TMP_Text playerScoreText;

    [SerializeField]
    private Image playerProfileImage;

    [SerializeField]
    private Color activePlayerColor;

    [SerializeField]
    private Image fillImage;

    [SerializeField]
    private Color defaultPlayerColor;

    private void Awake()
    {
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = (int)Image.Origin360.Top;
            fillImage.fillClockwise = true;
        }
    }

    public void SetPlayer(
        bool hasPlayer,
        string playerName,
        int score,
        Sprite profileSprite,
        bool isActivePlayer,
        float turnTimeRemaining,
        float turnDuration
    )
    {
        if (gameObject.activeSelf != hasPlayer)
            gameObject.SetActive(hasPlayer);

        if (playerNameText != null)
            playerNameText.text = hasPlayer ? playerName : "Waiting...";

        if (playerScoreText != null)
            playerScoreText.text = hasPlayer ? score.ToString() : string.Empty;

        if (
            playerProfileImage != null
            && profileSprite != null
            && playerProfileImage.GetComponent<Image>().sprite != profileSprite
        )
            playerProfileImage.sprite = profileSprite;

        float fillAmount =
            isActivePlayer && turnDuration > 0f
                ? Mathf.Clamp01(turnTimeRemaining / turnDuration)
                : 0f;

        if (fillImage != null)
            fillImage.fillAmount = fillAmount;
    }
}
