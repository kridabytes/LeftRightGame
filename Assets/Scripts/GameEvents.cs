using System;
using UnityEngine;

public static class GameEvents
{
    public static Action<String> SelctCharacter;
    public static Action ShowProfileScreen;
    public static event Action<CharaacterDetails> SaveCharacter;
    public static event Action RoomInvitesPageShow;
    public static event Action FriendsPageShow;
    public static event Action RankPageShow;
    public static event Action ShowSettingPage;
    public static event Action PlaySound;

    public static void OnSelectCharacter(string name)
    {
        SelctCharacter.Invoke(name);
    }

    public static void OnShowProfileScreen()
    {
        ShowProfileScreen.Invoke();
    }

    public static void OnSaveCharacter(CharaacterDetails details)
    {
        SaveCharacter.Invoke(details);
    }

    public static void OnRoomInvitesPageShow()
    {
        RoomInvitesPageShow.Invoke();
    }

    public static void OnShowFriendsPage()
    {
        FriendsPageShow.Invoke();
    }

    public static void OnRankPageShow()
    {
        RankPageShow.Invoke();
    }

    public static void OnSettingPageShow()
    {
        ShowSettingPage.Invoke();
    }

    public static void OnPlaySound()
    {
        PlaySound.Invoke();
    }
}
