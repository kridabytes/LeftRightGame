using Edlore.GamePages;
using Edlore.Utilities;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WellcomeSceneController : BasePage
{
    public Button playGamesSignInButton,
        appleSignInButton,
        facebookSignInButton,
        guestSignInButton;

    [SerializeField]
    private Image loadingBar;

    [SerializeField]
    private float loadingDuration = 3f;

    [SerializeField]
    private GameObject loadingPanel;

    [SerializeField]
    private GameObject createProfilePanel;

    [SerializeField]
    private TMP_InputField profileNameInput;

    [SerializeField]
    private GameObject playerGuestPanel;

    public string homeSceneName = "HomeScene";

    public const string ProfileNameKey = "UserName";

    private bool loadingCompleteHandled;
    private float loadingTimer;
    private FacebookLoginController facebookLoginController;

    private void Awake()
    {
        PageFactory.Add(this);
        if (
            loadingBar == null
            || createProfilePanel == null
            || profileNameInput == null
            || playerGuestPanel == null
        )
        {
            Debug.LogError("Assign all required UI references in the Inspector.", this);

            enabled = false;
            return;
        }

        loadingBar.type = Image.Type.Filled;
        loadingBar.fillAmount = 0f;

        createProfilePanel.SetActive(false);
        playerGuestPanel.SetActive(false);

        facebookLoginController = GetComponent<FacebookLoginController>();
        if (facebookLoginController == null)
            facebookLoginController = gameObject.AddComponent<FacebookLoginController>();

        facebookLoginController.Configure(facebookSignInButton, OnFacebookLoginSucceeded);
    }

    private void OnEnable()
    {
        if (playGamesSignInButton != null)
            playGamesSignInButton.onClick.AddListener(OnPlayGamesSignInClicked);
        if (appleSignInButton != null)
            appleSignInButton.onClick.AddListener(OnAppleSignInClicked);
        if (guestSignInButton != null)
            guestSignInButton.onClick.AddListener(OnClickGuest);

#if UNITY_IOS
        if (appleSignInButton != null)
            appleSignInButton.gameObject.SetActive(true);
#else
        if (appleSignInButton != null)
            appleSignInButton.gameObject.SetActive(false);
#endif
    }

    private void OnDisable()
    {
        if (playGamesSignInButton != null)
            playGamesSignInButton.onClick.RemoveListener(OnPlayGamesSignInClicked);
        if (appleSignInButton != null)
            appleSignInButton.onClick.RemoveListener(OnAppleSignInClicked);
        if (guestSignInButton != null)
            guestSignInButton.onClick.RemoveListener(OnClickGuest);
    }

    private void OnPlayGamesSignInClicked()
    {
        GameEvents.OnPlaySound();
        Debug.Log("Play Games Sign-In button clicked.");
        // Implement Play Games sign-in logic here
    }

    private void OnAppleSignInClicked()
    {
        GameEvents.OnPlaySound();
        Debug.Log("Apple Sign-In button clicked.");
        // Implement Apple sign-in logic here
    }

    private void OnFacebookLoginSucceeded(string profileName)
    {
        if (!HasProfile())
        {
            PlayerPrefs.SetString(ProfileNameKey, profileName);
            PlayerPrefs.Save();
        }

        loadingCompleteHandled = true;
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
        createProfilePanel.SetActive(false);
        playerGuestPanel.SetActive(false);

        PickCharacterManager pickCharacterManager = PageFactory.Get<PickCharacterManager>();
        if (pickCharacterManager == null)
        {
            Debug.LogError("Unable to open character selection after Facebook Sign-In.", this);
            createProfilePanel.SetActive(true);
            profileNameInput.text = profileName;
            return;
        }

        pickCharacterManager.Open();
    }

    private void Update()
    {
        if (loadingCompleteHandled)
            return;

        loadingTimer += Time.deltaTime;

        loadingBar.fillAmount = Mathf.Clamp01(loadingTimer / loadingDuration);

        if (loadingBar.fillAmount >= 1f)
        {
            loadingCompleteHandled = true;

            if (HasProfile())
            {
                LoadHomeScene();
            }
            else
            {
                loadingPanel.SetActive(false);
                createProfilePanel.SetActive(true);
            }
        }
    }

    public void OnClickGuest()
    {
        createProfilePanel.SetActive(false);
        playerGuestPanel.SetActive(true);
    }

    public void CreateProfile()
    {
        string profileName = profileNameInput.text.Trim();

        if (string.IsNullOrEmpty(profileName))
        {
            Debug.LogWarning("Enter a profile name before continuing.", this);
            return;
        }

        PlayerPrefs.SetString(ProfileNameKey, profileName);
        PlayerPrefs.Save();

        // LoadHomeScene();
        playerGuestPanel.SetActive(false);
        // PageFactory.Get<PickCharacterManager>().ShowPage();
        PageFactory.Get<PickCharacterManager>().Open();
    }

    private static bool HasProfile()
    {
        return PlayerPrefs.HasKey("UserName")
            && !string.IsNullOrWhiteSpace(PlayerPrefs.GetString("UserName"));
    }

    private void LoadHomeScene()
    {
        if (SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{homeSceneName}.unity") < 0)
        {
            Debug.LogError($"Scene '{homeSceneName}' must be added to Build Settings.", this);
            return;
        }

        SceneManager.LoadScene(homeSceneName);
    }
}
