using System.Data.Common;
using Edlore.GamePages;
using Edlore.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuPage : BasePage
{
    public static MainMenuPage mainMenuPage;
    public Button profileButton,
        friendsButton,
        settingButton,
        roomInvitesButton,
        rankButton,
        playbutton;
    public CharacterData characterData;
    public CharaacterDetails selectedCharacter;

    public void OnEnable()
    {
        GameEvents.SaveCharacter += SetProfile;

        profileButton.onClick.AddListener(OnClickProfile);
        roomInvitesButton.onClick.AddListener(OnClickRoomList);
        friendsButton.onClick.AddListener(OnClickFriendsButton);
        rankButton.onClick.AddListener(OnClickRank);
        settingButton.onClick.AddListener(OnClickSetting);
        playbutton.onClick.AddListener(PlayButtonClicked);
        SetProfile();
    }

    void OnDisable()
    {
        GameEvents.SaveCharacter -= SetProfile;

        profileButton.onClick.RemoveAllListeners();
        roomInvitesButton.onClick.RemoveAllListeners();
        friendsButton.onClick.RemoveAllListeners();
        rankButton.onClick.RemoveAllListeners();
        settingButton.onClick.RemoveAllListeners();
        playbutton.onClick.RemoveAllListeners();
    }

    private void OnClickSetting()
    {
        PlaySoundOnClick();
        GameEvents.OnSettingPageShow();
    }

    private void OnClickProfile()
    {
        // PageFactory.Get<ProfileManager>().ShowPage();
        PlaySoundOnClick();
        GameEvents.OnShowProfileScreen();
    }

    private void OnClickRoomList()
    {
        PlaySoundOnClick();
        GameEvents.OnRoomInvitesPageShow();
    }

    private void OnClickFriendsButton()
    {
        PlaySoundOnClick();
        GameEvents.OnShowFriendsPage();
    }

    private void OnClickRank()
    {
        PlaySoundOnClick();
        GameEvents.OnRankPageShow();
    }

    public void SetProfile(CharaacterDetails data = null)
    {
        Debug.Log("SetProfile called with data: " + (data != null ? data.characterName : "null"));
        if (data == null)
        {
            SetProfile(GetCharacterData());
        }
        if (data == null)
            return;

        selectedCharacter = data;
        var profileImage = profileButton.GetComponent<Image>();
        profileImage.color = data.characterColor;
        profileImage.transform.GetChild(0).gameObject.GetComponent<Image>().sprite =
            data.characterImage;
    }

    public CharaacterDetails GetCharacterData()
    {
        if (PlayerPrefs.HasKey("SelectedCharacter"))
        {
            int index = PlayerPrefs.GetInt("SelectedCharacter");
            for (int i = 0; i < characterData.charaacterDetails.Count; i++)
                if (i == index)
                    // SetProfile(characterData.charaacterDetails[i]);
                    return characterData.charaacterDetails[i];
        }
        return null;
    }

    public void PlaySoundOnClick()
    {
        GameEvents.OnPlaySound();
    }

    private void PlayButtonClicked()
    {
        PlaySoundOnClick();
        SceneManager.LoadScene("SelectMatchPanel");
    }
}
