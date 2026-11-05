using System.Collections.Generic;
using Edlore.GamePages;
using Edlore.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;

public class PickCharacterManager : BasePage
{
    public Transform avatarRoot;
    public CharacterData characterData;
    public Button letsGoButton;
    public List<CharacterItemBehaviour> characterItemList = new();
    public CharaacterDetails selectedCharacter;
    private int selectedCharacterIndex = -1;

    private void Awake()
    {
        PageFactory.Add(this);
    }

    void OnEnable()
    {
        if (letsGoButton != null)
        {
            letsGoButton.onClick.RemoveAllListeners();
            letsGoButton.onClick.AddListener(OnClickLetsGo);
        }

        GameEvents.SelctCharacter += OnSelectCharacter;
    }

    void OnDisable()
    {
        ClearCharacters();
        GameEvents.SelctCharacter -= OnSelectCharacter;
    }

    public void Open()
    {
        ShowPage();
        ClearCharacters();
        if (characterData.charaacterDetails.Count >= 0)
        {
            foreach (var item in characterData.charaacterDetails)
            {
                var prefab = ObjectPooler.objectInstance.GetPoolItemViaType(
                    PoolObjectsType.CHARCTER
                );
                var character = Instantiate(prefab, avatarRoot);
                CharacterItemBehaviour characterItemBehaviour =
                    character.GetComponent<CharacterItemBehaviour>();
                characterItemBehaviour.Init(item);
                character.gameObject.SetActive(true);
                characterItemList.Add(characterItemBehaviour);
            }
        }
    }

    private void OnClickLetsGo()
    {
        GameEvents.OnPlaySound();
        var wellcomeSceneController = PageFactory.Get<WellcomeSceneController>();

        if (wellcomeSceneController != null)
        {
            if (
                SceneUtility.GetBuildIndexByScenePath(
                    $"Assets/Scenes/{wellcomeSceneController.homeSceneName}.unity"
                ) < 0
            )
            {
                Debug.LogError(
                    $"Scene '{wellcomeSceneController.homeSceneName}' must be added to Build Settings.",
                    this
                );
                return;
            }

            if (selectedCharacterIndex >= 0)
            {
                PlayerPrefs.SetInt("SelectedCharacter", selectedCharacterIndex);
                PlayerPrefs.Save();
            }

            GameManager.charaacterDetails = selectedCharacter;
            SceneManager.LoadScene(wellcomeSceneController.homeSceneName);
        }
    }

    private void OnSelectCharacter(string name)
    {
        int index = 0;
        foreach (var item in characterItemList)
        {
            if (item.characterName.text == name)
            {
                item.OnApplyIndicator(true);
                selectedCharacter = item.charaacterDetails;
                selectedCharacterIndex = index;
            }
            else
            {
                item.OnApplyIndicator(false);
            }
            index++;
        }
    }

    public void ClearCharacters()
    {
        if (characterItemList != null && characterItemList.Count > 0)
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
}
