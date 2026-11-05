using System;
using Edlore.GamePages;
using Edlore.Utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MatchSettingsManager : BasePage
{
    private const string SettingsKey = "MatchSettings";

    public enum GameMode
    {
        QuickMatch,
        PowerUpMode,
    }

    public enum MatchOrder
    {
        Circular,
        Random,
    }

    public enum ArrowCombination
    {
        LeftRight,
        LeftRightGreen,
        LeftRightOrange,
        LeftRightPink,
        AllDirections,
    }

    [Serializable]
    public class MatchSettings
    {
        public GameMode GameMode;
        public float TurnTime = 3f;
        public MatchOrder Order = MatchOrder.Circular;
        public bool[] SelectedArrows = new bool[5];
    }

    [Header("Game Mode")]
    [SerializeField]
    private Button[] gameModeButtons;

    [SerializeField]
    private GameObject[] gameModeIndicators;

    [Header("Arrow Selection")]
    [SerializeField]
    private Button[] arrowButtons;

    [SerializeField]
    private GameObject[] arrowIndicators;

    [SerializeField]
    private GameObject arrowContainer;

    [Header("Time Selection")]
    [SerializeField]
    private Button[] timeButtons;

    [SerializeField]
    private GameObject[] timeIndicators;

    [Header("Order Selection")]
    [SerializeField]
    private Button[] orderButtons;

    [SerializeField]
    private GameObject[] orderIndicators;

    [Header("Panel Controls")]
    [SerializeField]
    private Button closeButton;

    [Header("Default Settings")]
    [SerializeField]
    private int defaultTime = 3;

    [SerializeField]
    private GameMode defaultGameMode = GameMode.QuickMatch;

    private static readonly float[] availableTimes = { 3f, 2f, 1f, 0.5f };
    private UnityAction[] arrowButtonActions;

    public MatchSettings CurrentSettings { get; private set; }

    public event Action<MatchSettings> OnSettingsChanged;

    private void Awake()
    {
        PageFactory.Add(this);
    }

    private void OnEnable()
    {
        InitializeSettings();
        RegisterButtonEvents();
        RefreshUI();
    }

    private void OnDisable()
    {
        UnregisterButtonEvents();
    }

    public void OpenSettings()
    {
        ShowPage();
        RefreshUI();
    }

    private void InitializeSettings(bool loadSavedSettings = true)
    {
        CurrentSettings = loadSavedSettings && PlayerPrefs.HasKey(SettingsKey)
            ? GetSavedSettings(defaultTime, defaultGameMode)
            : new MatchSettings
            {
                GameMode = defaultGameMode,
                TurnTime = defaultTime,
                Order = MatchOrder.Circular,
                SelectedArrows = new bool[5],
            };

        if (CurrentSettings.SelectedArrows.Length == 0)
        {
            CurrentSettings.SelectedArrows = new bool[5];
        }

        if (!HasSelectedArrow(CurrentSettings.SelectedArrows))
            CurrentSettings.SelectedArrows[0] = true;

        SaveSettings(CurrentSettings);
    }

    private void RegisterButtonEvents()
    {
        RegisterButtons(gameModeButtons, SelectGameMode);
        RegisterArrowButtons();
        RegisterButtons(timeButtons, SelectTime);
        RegisterButtons(orderButtons, SelectOrder);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseSettings);
    }

    private void RegisterArrowButtons()
    {
        if (arrowButtons == null)
            return;

        arrowButtonActions = new UnityAction[arrowButtons.Length];
        for (int i = 0; i < arrowButtons.Length; i++)
        {
            if (arrowButtons[i] == null)
                continue;

            int index = i;
            arrowButtonActions[i] = () => SelectArrow(index);
            arrowButtons[i].onClick.AddListener(arrowButtonActions[i]);
        }
    }

    private void UnregisterArrowButtons()
    {
        if (arrowButtons == null || arrowButtonActions == null)
            return;

        int count = Mathf.Min(arrowButtons.Length, arrowButtonActions.Length);
        for (int i = 0; i < count; i++)
        {
            if (arrowButtons[i] != null && arrowButtonActions[i] != null)
                arrowButtons[i].onClick.RemoveListener(arrowButtonActions[i]);
        }
        arrowButtonActions = null;
    }

    private void UnregisterButtonEvents()
    {
        UnregisterButtons(gameModeButtons, SelectGameMode);
        UnregisterArrowButtons();
        UnregisterButtons(timeButtons, SelectTime);
        UnregisterButtons(orderButtons, SelectOrder);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseSettings);
    }

    private void RegisterButtons(Button[] buttons, Action<int> callback)
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            int index = i;
            buttons[i].onClick.AddListener(() => callback(index));
        }
    }

    private void UnregisterButtons(Button[] buttons, Action<int> callback)
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            int index = i;
            buttons[i].onClick.RemoveListener(() => callback(index));
        }
    }

    // Game Mode

    private void SelectGameMode(int index)
    {
        if (!Enum.IsDefined(typeof(GameMode), index))
            return;

        CurrentSettings.GameMode = (GameMode)index;
        RefreshGameModeVisibility();

        RefreshIndicators(gameModeIndicators, (int)CurrentSettings.GameMode);
        NotifySettingsChanged();
    }

    // Arrow Selection

    private void SelectArrow(int index)
    {
        if (index < 0 || index >= CurrentSettings.SelectedArrows.Length)
            return;

        Array.Clear(CurrentSettings.SelectedArrows, 0, CurrentSettings.SelectedArrows.Length);
        CurrentSettings.SelectedArrows[index] = true;
        RefreshArrowIndicators();
        NotifySettingsChanged();
    }

    // Time Selection

    private void SelectTime(int index)
    {
        if (index < 0 || index >= availableTimes.Length)
            return;

        CurrentSettings.TurnTime = availableTimes[index];

        RefreshIndicators(timeIndicators, Array.IndexOf(availableTimes, CurrentSettings.TurnTime));
        NotifySettingsChanged();
    }

    // Order Selection

    private void SelectOrder(int index)
    {
        if (!Enum.IsDefined(typeof(MatchOrder), index))
            return;

        CurrentSettings.Order = (MatchOrder)index;

        RefreshIndicators(orderIndicators, (int)CurrentSettings.Order);
        NotifySettingsChanged();
    }

    // UI Refresh

    private void RefreshUI()
    {
        RefreshIndicators(gameModeIndicators, (int)CurrentSettings.GameMode);
        RefreshGameModeVisibility();

        RefreshIndicators(timeIndicators, Array.IndexOf(availableTimes, CurrentSettings.TurnTime));

        RefreshIndicators(orderIndicators, (int)CurrentSettings.Order);

        RefreshArrowIndicators();
    }

    private void RefreshArrowIndicators()
    {
        if (arrowIndicators == null)
            return;

        for (int i = 0; i < arrowIndicators.Length; i++)
        {
            if (arrowIndicators[i] == null)
                continue;

            bool selected =
                i < CurrentSettings.SelectedArrows.Length && CurrentSettings.SelectedArrows[i];

            arrowIndicators[i].SetActive(selected);
        }
    }

    private void RefreshGameModeVisibility()
    {
        if (arrowContainer != null)
            arrowContainer.SetActive(CurrentSettings.GameMode == GameMode.PowerUpMode);
    }

    private void RefreshIndicators(GameObject[] indicators, int selectedIndex)
    {
        if (indicators == null)
            return;

        for (int i = 0; i < indicators.Length; i++)
        {
            if (indicators[i] != null)
            {
                Debug.Log($"Setting indicator {i} active: {i == selectedIndex}");
                bool isActive = i == selectedIndex;
                indicators[i].gameObject.GetComponent<Image>().enabled = isActive;
            }
        }
    }

    // Settings Access

    public MatchSettings GetSettings()
    {
        return CurrentSettings;
    }

    public static MatchSettings GetSavedSettings(
        float fallbackTurnTime = 3f,
        GameMode fallbackGameMode = GameMode.QuickMatch
    )
    {
        if (!PlayerPrefs.HasKey(SettingsKey))
        {
            return CreateDefaultSettings(fallbackTurnTime, fallbackGameMode);
        }

        MatchSettings settings = JsonUtility.FromJson<MatchSettings>(
            PlayerPrefs.GetString(SettingsKey)
        );
        if (settings == null)
            return CreateDefaultSettings(fallbackTurnTime, fallbackGameMode);

        if (!Enum.IsDefined(typeof(GameMode), settings.GameMode))
            settings.GameMode = fallbackGameMode;

        if (!IsAvailableTime(settings.TurnTime))
            settings.TurnTime = fallbackTurnTime;

        if (!Enum.IsDefined(typeof(MatchOrder), settings.Order))
            settings.Order = MatchOrder.Circular;

        bool[] selectedArrows = new bool[5];
        if (settings.SelectedArrows != null)
            Array.Copy(settings.SelectedArrows, selectedArrows, Mathf.Min(
                settings.SelectedArrows.Length,
                selectedArrows.Length
            ));

        if (!HasSelectedArrow(selectedArrows))
            selectedArrows[0] = true;

        settings.SelectedArrows = selectedArrows;
        return settings;
    }

    public void ResetSettings()
    {
        InitializeSettings(false);
        RefreshUI();
        NotifySettingsChanged();
    }

    private void NotifySettingsChanged()
    {
        SaveSettings(CurrentSettings);
        OnSettingsChanged?.Invoke(CurrentSettings);
    }

    private static MatchSettings CreateDefaultSettings(float turnTime, GameMode gameMode)
    {
        var settings = new MatchSettings
        {
            GameMode = gameMode,
            TurnTime = turnTime,
            Order = MatchOrder.Circular,
            SelectedArrows = new bool[5],
        };
        settings.SelectedArrows[0] = true;
        return settings;
    }

    private static bool IsAvailableTime(float time)
    {
        foreach (float availableTime in availableTimes)
        {
            if (Mathf.Approximately(time, availableTime))
                return true;
        }

        return false;
    }

    private static bool HasSelectedArrow(bool[] selectedArrows)
    {
        foreach (bool selected in selectedArrows)
        {
            if (selected)
                return true;
        }

        return false;
    }

    private static void SaveSettings(MatchSettings settings)
    {
        PlayerPrefs.SetString(SettingsKey, JsonUtility.ToJson(settings));
        PlayerPrefs.Save();
    }

    private void CloseSettings()
    {
        HidePage();
    }
}
