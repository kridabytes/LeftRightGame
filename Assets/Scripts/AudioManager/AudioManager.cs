using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField]
    private AudioSource musicSource;

    [SerializeField]
    private AudioSource soundSource;

    private const string MusicKey = "MusicEnabled";
    private const string SoundKey = "SoundEnabled";
    private const string VibrateKey = "VibrateEnabled";

    public bool MusicEnabled { get; private set; }
    public bool SoundEnabled { get; private set; }
    public bool VibrateEnabled { get; private set; }

    private static AudioClip TapSound;

    private AudioListener audioListener;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioListener = GetComponentInChildren<AudioListener>(true);

        MusicEnabled = PlayerPrefs.GetInt(MusicKey, 1) == 1;
        SoundEnabled = PlayerPrefs.GetInt(SoundKey, 1) == 1;
        VibrateEnabled = PlayerPrefs.GetInt(VibrateKey, 1) == 1;

        TapSound = soundSource.clip;

        ApplyMusic();
        ApplySound();
    }

    void OnEnable()
    {
        GameEvents.PlaySound += PlaySound;
    }

    void OnDisable()
    {
        GameEvents.PlaySound -= PlaySound;
    }

    public void ToggleMusic()
    {
        SetMusicEnabled(!MusicEnabled);
    }

    public void SetMusicEnabled(bool enabled)
    {
        MusicEnabled = enabled;
        PlayerPrefs.SetInt(MusicKey, enabled ? 1 : 0);

        ApplyMusic();
        PlayerPrefs.Save();
    }

    public void ToggleSound()
    {
        SetSoundEnabled(!SoundEnabled);
    }

    public void SetSoundEnabled(bool enabled)
    {
        SoundEnabled = enabled;
        PlayerPrefs.SetInt(SoundKey, enabled ? 1 : 0);

        ApplySound();
        PlayerPrefs.Save();
    }

    public void ToggleVibration()
    {
        SetVibrationEnabled(!VibrateEnabled);
    }

    public void SetVibrationEnabled(bool enabled)
    {
        VibrateEnabled = enabled;
        PlayerPrefs.SetInt(VibrateKey, enabled ? 1 : 0);

        PlayerPrefs.Save();
    }

    private void ApplyMusic()
    {
        if (musicSource == null)
            return;

        musicSource.mute = !MusicEnabled;
    }

    private void ApplySound()
    {
        if (soundSource == null)
            return;

        soundSource.mute = !SoundEnabled;
    }

    public void PlaySound()
    {
        if (!SoundEnabled || soundSource == null || TapSound == null)
            return;

        soundSource.PlayOneShot(TapSound);
    }

    public void PlayVibration()
    {
        if (VibrateEnabled)
            Handheld.Vibrate();
    }
}
