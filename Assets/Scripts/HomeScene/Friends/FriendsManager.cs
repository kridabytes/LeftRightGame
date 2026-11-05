using Edlore.GamePages;
using UnityEngine;
using UnityEngine.UI;

public class FriendsManager : BasePage
{
    [Header("Navigation")]
    public Button backButton;
    public Button recentTabButton;
    public Button friendsTabButton;

    [Header("Main Panels")]
    public GameObject friendsPanel;
    public GameObject recentPanel;

    // [Header("Friends Sections")]
    // public GameObject friendRequestsPanel;
    // public GameObject friendsListPanel;

    private void OnEnable()
    {
        GameEvents.FriendsPageShow += OnShowPage;

        backButton.onClick.AddListener(OnBackClick);
        recentTabButton.onClick.AddListener(OnRecentTabClick);
        friendsTabButton.onClick.AddListener(OnFriendsTabClick);

        ShowFriendsTab();
    }

    private void OnDisable()
    {
        GameEvents.FriendsPageShow -= OnShowPage;

        backButton.onClick.RemoveListener(OnBackClick);
        recentTabButton.onClick.RemoveListener(OnRecentTabClick);
        friendsTabButton.onClick.RemoveListener(OnFriendsTabClick);
    }

    private void OnShowPage()
    {
        ShowPage();
        ShowFriendsTab();
    }

    private void OnRecentTabClick()
    {
        GameEvents.OnPlaySound();
        ShowRecentTab();
    }

    private void OnFriendsTabClick()
    {
        GameEvents.OnPlaySound();
        ShowFriendsTab();
    }

    private void ShowRecentTab()
    {
        friendsPanel.SetActive(false);
        recentPanel.SetActive(true);

        recentTabButton.image.enabled = true;
        friendsTabButton.image.enabled = false;
    }

    private void ShowFriendsTab()
    {
        recentPanel.SetActive(false);
        friendsPanel.SetActive(true);

        recentTabButton.image.enabled = false;
        friendsTabButton.image.enabled = true;
    }

    // public void ShowFriendsList()
    // {
    //     friendsListPanel.SetActive(true);
    //     friendRequestsPanel.SetActive(false);
    // }

    // public void ShowFriendRequests()
    // {
    //     friendsListPanel.SetActive(false);
    //     friendRequestsPanel.SetActive(true);
    // }

    private void OnBackClick()
    {
        GameEvents.OnPlaySound();
        HidePage();
    }
}
