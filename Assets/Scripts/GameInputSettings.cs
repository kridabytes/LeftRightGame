using UnityEngine;

public static class GameInputSettings
{
    private const string InputModeKey = "InputModeSwipe";

    public static bool UseSwipeControls => PlayerPrefs.GetInt(InputModeKey, 0) == 1;

    public static void SetSwipeControls(bool enabled)
    {
        PlayerPrefs.SetInt(InputModeKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}
