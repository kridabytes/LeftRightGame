using Edlore.GamePages;
using UnityEngine;
using UnityEngine.UI;

public class SettingManager : BasePage
{
    [Header("Settings Buttons")]
    public Button soundButton;
    public Button musicButton;
    public Button vibrateButton;

    [Header("DisableImages")]
    public GameObject soundDisableImage;
    public GameObject musicDisbleImage;
    public GameObject vibrateDisableButton;

    [Header("Input Mode Buttons")]
    public Button tapArrowButton;
    public Button swipeButton;

    [Header("Input Mode Indicators")]
    public GameObject tapArrowSelectedIndicator;
    public GameObject swipeSelectedIndicator;

    public Button backButton;

    private const string SoundKey = "SoundEnabled";
    private const string MusicKey = "MusicEnabled";
    private const string VibrateKey = "VibrateEnabled";

    public static bool SoundEnabled { get; private set; }
    public static bool MusicEnabled { get; private set; }
    public static bool VibrateEnabled { get; private set; }

    private void Awake()
    {
        SoundEnabled = PlayerPrefs.GetInt(SoundKey, 1) == 1;
        MusicEnabled = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        VibrateEnabled = PlayerPrefs.GetInt(VibrateKey, 1) == 1;
        ApplySettings();
        ApplySettings();
        UpdateHighlights();
        UpdateInputModeIndicators();
    }

    private void OnEnable()
    {
        GameEvents.ShowSettingPage += OnShowPage;
        soundButton.onClick.AddListener(ToggleSound);
        musicButton.onClick.AddListener(ToggleMusic);
        vibrateButton.onClick.AddListener(ToggleVibrate);
        tapArrowButton.onClick.AddListener(SelectTapArrow);
        swipeButton.onClick.AddListener(SelectSwipe);

        backButton.onClick.AddListener(OnBackClick);

        UpdateHighlights();
        UpdateInputModeIndicators();
    }

    private void OnDisable()
    {
        GameEvents.ShowSettingPage -= OnShowPage;

        soundButton.onClick.RemoveListener(ToggleSound);
        musicButton.onClick.RemoveListener(ToggleMusic);
        vibrateButton.onClick.RemoveListener(ToggleVibrate);
        tapArrowButton.onClick.RemoveListener(SelectTapArrow);
        swipeButton.onClick.RemoveListener(SelectSwipe);

        backButton.onClick.RemoveAllListeners();
    }

    private void OnShowPage()
    {
        ShowPage();
    }

    private void OnBackClick()
    {
        PlaySound();
        HidePage();
    }

    private void ToggleSound()
    {
        PlaySound();
        SoundEnabled = !SoundEnabled;
        PlayerPrefs.SetInt(SoundKey, SoundEnabled ? 1 : 0);
        PlayVibration();
        ApplySettings();
        UpdateHighlights();
    }

    private void ToggleMusic()
    {
        PlaySound();
        MusicEnabled = !MusicEnabled;
        PlayerPrefs.SetInt(MusicKey, MusicEnabled ? 1 : 0);
        PlayVibration();
        ApplySettings();
        UpdateHighlights();
    }

    private void ToggleVibrate()
    {
        PlaySound();
        VibrateEnabled = !VibrateEnabled;
        PlayerPrefs.SetInt(VibrateKey, VibrateEnabled ? 1 : 0);
        PlayVibration();
        ApplySettings();
        UpdateHighlights();
    }

    private void SelectTapArrow()
    {
        PlaySound();
        GameInputSettings.SetSwipeControls(false);
        PlayVibration();
        UpdateInputModeIndicators();
    }

    private void SelectSwipe()
    {
        PlaySound();
        GameInputSettings.SetSwipeControls(true);
        PlayVibration();
        UpdateInputModeIndicators();
    }

    private void UpdateInputModeIndicators()
    {
        bool useSwipe = GameInputSettings.UseSwipeControls;
        if (tapArrowSelectedIndicator != null)
            tapArrowSelectedIndicator.SetActive(!useSwipe);
        if (swipeSelectedIndicator != null)
            swipeSelectedIndicator.SetActive(useSwipe);
    }

    private void ApplySettings()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicEnabled(MusicEnabled);
            AudioManager.Instance.SetSoundEnabled(SoundEnabled);
        }

        PlayerPrefs.Save();
    }

    private void UpdateHighlights()
    {
        soundDisableImage.gameObject.SetActive(!SoundEnabled);
        musicDisbleImage.gameObject.SetActive(!MusicEnabled);
        vibrateDisableButton.gameObject.SetActive(!VibrateEnabled);
    }

    public static void PlayVibration()
    {
        if (VibrateEnabled)
        {
            Handheld.Vibrate();
        }
    }

    public void PlaySound()
    {
        GameEvents.OnPlaySound();
    }
}
