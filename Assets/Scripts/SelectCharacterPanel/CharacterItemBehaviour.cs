using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterItemBehaviour : MonoBehaviour
{
    public TMP_Text characterName;
    public TMP_Text description;
    public Image image;
    public Button selfButton;
    public GameObject rightIcon;
    public TMP_Text poseText;
    public CharaacterDetails charaacterDetails;

    public void Init(CharaacterDetails data)
    {
        charaacterDetails = data;
        characterName.text = data.characterName;
        description.text = data.description;
        image.sprite = data.characterImage;
        var selfImage = GetComponent<Image>();
        if (selfImage != null)
        {
            selfImage.color = data.characterColor;
        }
        poseText.text = data.poseName;
        poseText.color = data.characterColor;

        selfButton.onClick.RemoveAllListeners();
        selfButton.onClick.AddListener(onSelectButton);
    }

    private void onSelectButton()
    {
        GameEvents.OnPlaySound();
        GameEvents.OnSelectCharacter(characterName.text);
    }

    public void OnApplyIndicator(bool isSelect)
    {
        if (rightIcon == null)
        {
            Debug.LogError("Right Icon is not assigned in CharacterItemBehaviour");
            return;
        }
        rightIcon.SetActive(isSelect);
    }
}
