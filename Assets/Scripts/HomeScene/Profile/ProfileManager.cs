using System.Collections.Generic;
using System.Threading.Tasks;
using Edlore.GamePages;
using Edlore.Utilities;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ProfileManager : BasePage
{
    public CharacterData characterData;
    public TMP_InputField nameInputField;
    public Image characterImage;
    public Button saveButton;
    public Transform charactersParent;
    public CharaacterDetails selectedCharacter;
    public List<CharacterItemBehaviour> characterItemList;
    public Button backButton;

    private void Awake()
    {
        PageFactory.Add(this);
    }

    void OnEnable()
    {
        GameEvents.ShowProfileScreen += OnShowProfile;
        GameEvents.SelctCharacter += OnSelectCharacter;

        if (backButton != null)
        {
            Debug.Log("Add Back Listenner");
            backButton.onClick.AddListener(BackButtonClick);
        }

        if (saveButton != null)
            saveButton.onClick.AddListener(Save);

        // if (PlayerPrefs.HasKey("SelectedCharacter"))
        // {
        //     int index = PlayerPrefs.GetInt("SelectedCharacter");

        //     for (int i = 0; i < characterData.charaacterDetails.Count; i++)
        //     {
        //         if (index == i)
        //         {
        //             GameManager.charaacterDetails = characterData.charaacterDetails[i];
        //             break;
        //         }
        //     }
        // }

        var mainMenuPage = PageFactory.Get<MainMenuPage>();
        selectedCharacter =
            GetSavedCharacter()
            ?? (mainMenuPage != null
                ? mainMenuPage.selectedCharacter ?? mainMenuPage.GetCharacterData()
                : null);
        SetProfileDetails();
    }

    void OnDisable()
    {
        GameEvents.ShowProfileScreen -= OnShowProfile;
        GameEvents.SelctCharacter -= OnSelectCharacter;

        if (backButton != null)
            backButton.onClick.RemoveAllListeners();

        if (saveButton != null)
            saveButton.onClick.RemoveAllListeners();
    }

    private void OnShowProfile()
    {
        ShowPage();
        ClearCharacters();
        selectedCharacter = GetSavedCharacter() ?? selectedCharacter;
        if (backButton != null)
            backButton.onClick.RemoveAllListeners();

        if (characterData != null && characterData.charaacterDetails.Count > 0)
        {
            for (int i = 0; i < characterData.charaacterDetails.Count; i++)
            {
                var item = characterData.charaacterDetails[i];
                var prefab = ObjectPooler.objectInstance.GetPoolItemViaType(
                    PoolObjectsType.CHARCTER
                );
                var character = Instantiate(prefab, charactersParent);
                CharacterItemBehaviour characterItemBehaviour =
                    character.GetComponent<CharacterItemBehaviour>();
                characterItemBehaviour.Init(item);
                character.gameObject.SetActive(true);
                characterItemList.Add(characterItemBehaviour);
                characterItemBehaviour.OnApplyIndicator(
                    selectedCharacter != null
                    && selectedCharacter == item
                );
            }
        }
        SetProfileDetails();
        // if (nameInputField != null && PlayerPrefs.HasKey("UserName"))
        // {
        //     nameInputField.text = PlayerPrefs.GetString("UserName");
        // }
    }

    public void BackButtonClick()
    {
        GameEvents.OnPlaySound();
        Debug.Log("OnClickBack");
        HidePage();
    }

    public void SetProfileDetails()
    {
        if (selectedCharacter == null)
            return;

        characterImage.color = selectedCharacter.characterColor;
        characterImage.transform.GetChild(0).gameObject.GetComponent<Image>().sprite =
            selectedCharacter.characterImage;

        if (PlayerPrefs.HasKey("UserName"))
        {
            nameInputField.text = PlayerPrefs.GetString("UserName");
        }
    }

    private CharaacterDetails GetSavedCharacter()
    {
        if (
            characterData == null
            || characterData.charaacterDetails == null
            || !PlayerPrefs.HasKey("SelectedCharacter")
        )
            return null;

        int index = PlayerPrefs.GetInt("SelectedCharacter");
        return index >= 0 && index < characterData.charaacterDetails.Count
            ? characterData.charaacterDetails[index]
            : null;
    }

    private void OnSelectCharacter(string name)
    {
        foreach (var item in characterItemList)
        {
            if (item.characterName.text == name)
            {
                item.OnApplyIndicator(true);
                selectedCharacter = item.charaacterDetails;
            }
            else
            {
                item.OnApplyIndicator(false);
            }
        }
    }

    public void ClearCharacters()
    {
        if (characterItemList != null && characterItemList.Count == 0)
            return;

        foreach (var item in characterItemList)
        {
            // Destroy(item);
            ObjectPooler.objectInstance.ReturnObjectToPool(
                PoolObjectsType.CHARCTER,
                item.gameObject
            );
        }
        characterItemList.Clear();
    }

    private void Save()
    {
        GameEvents.OnPlaySound();
        GameEvents.OnSaveCharacter(selectedCharacter);
        if (!string.IsNullOrWhiteSpace(nameInputField.text) && PlayerPrefs.HasKey("UserName"))
        {
            PlayerPrefs.SetString("UserName", nameInputField.text);
        }

        int selectedCharacterIndex = characterData.charaacterDetails.IndexOf(selectedCharacter);
        if (selectedCharacterIndex >= 0)
        {
            PlayerPrefs.SetInt("SelectedCharacter", selectedCharacterIndex);
            PlayerPrefs.Save();
        }

        GameManager.charaacterDetails = selectedCharacter;
        HidePage();
    }
}
