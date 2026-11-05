using System;
using System.Collections.Generic;
using Edlore.Utilities;
using Facebook.Unity;
using UnityEngine;
using UnityEngine.UI;

public class FacebookLoginController : MonoBehaviour
{
    private Button signInButton;
    private Action<string> onLoginSucceeded;
    private bool loginInProgress;

    public void Configure(Button button, Action<string> loginSucceededCallback)
    {
        if (signInButton != null)
            signInButton.onClick.RemoveListener(OnSignInClicked);

        signInButton = button;
        onLoginSucceeded = loginSucceededCallback;

        if (signInButton == null)
        {
            Debug.LogWarning("Facebook Sign-In button is not assigned.", this);
            return;
        }

        signInButton.onClick.RemoveListener(OnSignInClicked);
        signInButton.onClick.AddListener(OnSignInClicked);
    }

    private void Start()
    {
        if (FB.IsInitialized)
        {
            OnFacebookSdkInitialized();
            return;
        }

        FB.Init(OnFacebookSdkInitialized, OnFacebookUnityVisibilityChanged);
    }

    private void OnDestroy()
    {
        if (signInButton != null)
            signInButton.onClick.RemoveListener(OnSignInClicked);
    }

    private void OnFacebookSdkInitialized()
    {
        if (!FB.IsInitialized)
        {
            Debug.LogError("Failed to initialize the Facebook SDK.", this);
            return;
        }

        FB.ActivateApp();
    }

    private void OnFacebookUnityVisibilityChanged(bool isGameShown)
    {
        Time.timeScale = isGameShown ? 1f : 0f;
    }

    private void OnSignInClicked()
    {
        GameEvents.OnPlaySound();

        if (loginInProgress)
            return;

        if (!FB.IsInitialized)
        {
            Debug.LogError("Facebook Sign-In is unavailable because the SDK is not initialized.", this);
            return;
        }

        loginInProgress = true;
        if (signInButton != null)
            signInButton.interactable = false;

        FB.LogInWithReadPermissions(
            new List<string> { "public_profile" },
            OnFacebookLoginCompleted
        );
    }

    private void OnFacebookLoginCompleted(ILoginResult result)
    {
        if (!string.IsNullOrEmpty(result.Error))
        {
            FinishLogin();
            Debug.LogError($"Facebook Sign-In failed: {result.Error}", this);
            return;
        }

        if (!FB.IsLoggedIn)
        {
            FinishLogin();
            Debug.Log("Facebook Sign-In was cancelled.", this);
            return;
        }

        FB.CurrentProfile(OnFacebookProfileRetrieved);
    }

    private void OnFacebookProfileRetrieved(IProfileResult result)
    {
        FinishLogin();

        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError($"Unable to retrieve the Facebook profile: {result.Error}", this);
            return;
        }

        if (result.CurrentProfile == null)
        {
            Debug.LogError("Facebook Sign-In succeeded, but no profile was returned.", this);
            return;
        }

        string profileName = result.CurrentProfile.Name;
        if (string.IsNullOrWhiteSpace(profileName))
        {
            profileName =
                $"{result.CurrentProfile.FirstName} {result.CurrentProfile.LastName}".Trim();
        }

        if (string.IsNullOrWhiteSpace(profileName))
        {
            Debug.LogError("The Facebook profile does not include a usable name.", this);
            return;
        }

        if (onLoginSucceeded == null)
        {
            Debug.LogError("Facebook login succeeded, but no success callback is configured.", this);
            return;
        }

        onLoginSucceeded(profileName);
    }

    private void FinishLogin()
    {
        loginInProgress = false;
        if (signInButton != null)
            signInButton.interactable = true;
    }
}
