using System.Collections.Generic;
using Edlore.GamePages;
using Edlore.Utilities;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchGameplayUI : BasePage
{
    [Header("Match Manager")]
    [SerializeField]
    private MatchManager matchManager;

    [Header("Timer and Turn")]
    [SerializeField]
    private TMP_Text matchTimerText;

    [SerializeField]
    private TMP_Text turnTimerText;

    [SerializeField]
    private TMP_Text activePlayerText;

    [SerializeField]
    private TMP_Text directionText;

    [Header("Score")]
    [SerializeField]
    private TMP_Text sharedPotText;

    [SerializeField]
    private GameObject[] playerObjects;

    private readonly List<PlayerRef> connectedPlayers = new List<PlayerRef>(4);
    private GameplayPlayerController[] playerControllers;
    public Image arrorwDirectionImage;
    public Sprite leftArrowSprite;
    public Sprite rightArrowSprite;

    [SerializeField]
    private Image leftButtonArrowImage;

    [SerializeField]
    private Image rightButtonArrowImage;

    [SerializeField]
    private GameObject leftInputButton;

    [SerializeField]
    private GameObject rightInputButton;

    public CharacterData characterDetails;

    private Vector2 swipeStartPosition;
    private bool isTrackingTouch;
    private int trackedTouchId;
    private bool isTrackingMouse;

    private void Awake()
    {
        PageFactory.Add(this);
        InitializePlayerControllers();
        UpdateInputControls();
    }

    private void Update()
    {
        HandleSwipeInput();

        if (matchManager == null || !matchManager.HasSpawned)
            return;

        RefreshConnectedPlayers();
        UpdateTimers();
        UpdateTurn();
        UpdatePlayerControllers();
    }

    private void InitializePlayerControllers()
    {
        int slotCount = playerObjects != null ? playerObjects.Length : 0;
        playerControllers = new GameplayPlayerController[slotCount];

        for (int position = 0; position < slotCount; position++)
        {
            GameObject playerObject = playerObjects[position];
            if (playerObject == null)
            {
                Debug.LogError($"Player seat {position} is not assigned.", this);
                continue;
            }

            GameplayPlayerController controller =
                playerObject.GetComponent<GameplayPlayerController>();
            if (controller == null)
            {
                Debug.LogError(
                    $"Player seat {position} is missing its GameplayPlayerController component.",
                    playerObject
                );
                continue;
            }

            playerControllers[position] = controller;
        }
    }

    public void OnLeftClicked()
    {
        if (
            !GameInputSettings.UseSwipeControls
            && matchManager != null
            && matchManager.HasSpawned
        )
            matchManager.SubmitLocalInput(PlayerDirectionInput.Left);
    }

    public void OnRightClicked()
    {
        if (
            !GameInputSettings.UseSwipeControls
            && matchManager != null
            && matchManager.HasSpawned
        )
            matchManager.SubmitLocalInput(PlayerDirectionInput.Right);
    }

    private void UpdateInputControls()
    {
        bool showArrowButtons = !GameInputSettings.UseSwipeControls;
        if (leftInputButton != null)
            leftInputButton.SetActive(showArrowButtons);
        if (rightInputButton != null)
            rightInputButton.SetActive(showArrowButtons);
    }

    private void HandleSwipeInput()
    {
        if (
            !GameInputSettings.UseSwipeControls
            || matchManager == null
            || !matchManager.HasSpawned
            || !matchManager.IsMatchRunning
        )
        {
            isTrackingTouch = false;
            isTrackingMouse = false;
            return;
        }

        if (Input.touchCount > 0)
        {
            isTrackingMouse = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (!isTrackingTouch && touch.phase == TouchPhase.Began)
                {
                    trackedTouchId = touch.fingerId;
                    swipeStartPosition = touch.position;
                    isTrackingTouch = true;
                }
                else if (isTrackingTouch && touch.fingerId == trackedTouchId)
                {
                    if (touch.phase == TouchPhase.Ended)
                    {
                        SubmitSwipe(touch.position);
                        isTrackingTouch = false;
                    }
                    else if (touch.phase == TouchPhase.Canceled)
                    {
                        isTrackingTouch = false;
                    }
                }
            }

            return;
        }

        isTrackingTouch = false;
        if (Input.GetMouseButtonDown(0))
        {
            swipeStartPosition = Input.mousePosition;
            isTrackingMouse = true;
        }
        else if (isTrackingMouse && Input.GetMouseButtonUp(0))
        {
            SubmitSwipe(Input.mousePosition);
            isTrackingMouse = false;
        }
    }

    private void SubmitSwipe(Vector2 endPosition)
    {
        Vector2 delta = endPosition - swipeStartPosition;
        if (Mathf.Abs(delta.x) < 80f || Mathf.Abs(delta.x) <= Mathf.Abs(delta.y))
            return;

        matchManager.SubmitLocalInput(
            delta.x < 0f ? PlayerDirectionInput.Left : PlayerDirectionInput.Right
        );
    }

    private void UpdateTimers()
    {
        UpdateTimerText(matchTimerText, matchManager.GetMatchTimeRemaining());
        UpdateTimerText(turnTimerText, matchManager.GetTurnTimeRemaining());
    }

    private void UpdateTimerText(TMP_Text text, float time)
    {
        if (text == null)
            return;

        int seconds = Mathf.CeilToInt(Mathf.Max(0f, time));
        text.text = $"{seconds / 60:00}:{seconds % 60:00}";
    }

    private void UpdateTurn()
    {
        int currentIndex = matchManager.CurrentPlayerIndex;
        string currentPlayerName = GetPlayerName(currentIndex);

        if (activePlayerText != null)
            activePlayerText.text = $"{currentPlayerName}'s Turn";

        // if (directionText != null)
        // {
        //     directionText.text =
        //         matchManager.ExpectedDirection == TurnDirection.Left ? "← LEFT" : "RIGHT →";
        // }

        Sprite leftSprite = matchManager.GetArrowSprite(TurnDirection.Left) ?? leftArrowSprite;
        Sprite rightSprite = matchManager.GetArrowSprite(TurnDirection.Right) ?? rightArrowSprite;

        if (arrorwDirectionImage != null)
            arrorwDirectionImage.sprite =
                matchManager.ExpectedDirection == TurnDirection.Left ? leftSprite : rightSprite;

        if (leftButtonArrowImage != null)
            leftButtonArrowImage.sprite = leftSprite;

        if (rightButtonArrowImage != null)
            rightButtonArrowImage.sprite = rightSprite;
    }

    private void UpdatePlayerControllers()
    {
        if (sharedPotText != null)
            sharedPotText.text = matchManager.SharedPot.ToString();

        for (int position = 0; position < playerControllers.Length; position++)
        {
            GameplayPlayerController controller = playerControllers[position];
            if (controller == null)
                continue;

            PlayerRef player = GetPlayerForPosition(position);
            bool playerExists = player != PlayerRef.None;
            int playerIndex = GetMatchPlayerIndex(player);
            int score =
                playerExists && playerIndex >= 0 ? matchManager.PlayerScores.Get(playerIndex) : 0;
            bool isActivePlayer =
                matchManager.IsMatchRunning
                && playerIndex >= 0
                && playerIndex == matchManager.CurrentPlayerIndex;

            controller.SetPlayer(
                playerExists,
                playerExists ? GetPlayerName(player) : "Waiting...",
                score,
                playerExists ? GetPlayerProfileSprite(player) : null,
                isActivePlayer,
                isActivePlayer ? matchManager.GetTurnTimeRemaining() : 0f,
                isActivePlayer ? matchManager.GetTurnDuration() : 0f
            );
        }
    }
    private void RefreshConnectedPlayers()
    {
        connectedPlayers.Clear();

        foreach (PlayerRef player in matchManager.Runner.ActivePlayers)
            connectedPlayers.Add(player);

        connectedPlayers.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));
    }

    private PlayerRef GetPlayerForPosition(int position)
    {
        if (connectedPlayers.Count == 0)
            return PlayerRef.None;

        int localPosition = connectedPlayers.IndexOf(matchManager.Runner.LocalPlayer);
        if (localPosition < 0)
            return PlayerRef.None;

        int rosterOffset = position switch
        {
            0 => 0,
            1 when connectedPlayers.Count >= 3 => 1,
            2 when connectedPlayers.Count == 2 => 1,
            2 when connectedPlayers.Count >= 3 => 2,
            3 when connectedPlayers.Count == 4 => 3,
            _ => -1,
        };

        if (rosterOffset < 0)
            return PlayerRef.None;

        return connectedPlayers[(localPosition + rosterOffset) % connectedPlayers.Count];
    }

    private int GetMatchPlayerIndex(PlayerRef player)
    {
        if (player == PlayerRef.None)
            return -1;

        for (int i = 0; i < 4; i++)
        {
            if (matchManager.Players.Get(i) == player)
                return i;
        }

        return -1;
    }

    private string GetPlayerName(int index)
    {
        if (index < 0 || index >= 4)
            return "Waiting...";

        return GetPlayerName(matchManager.Players.Get(index));
    }

    private string GetPlayerName(PlayerRef player)
    {
        if (player == PlayerRef.None)
            return "Waiting...";

        int playerIndex = GetMatchPlayerIndex(player);
        if (playerIndex >= 0)
        {
            string playerName = matchManager.PlayerNames.Get(playerIndex).ToString();
            if (!string.IsNullOrWhiteSpace(playerName))
                return playerName;
        }

        NetworkObject playerObject = matchManager.Runner.GetPlayerObject(player);
        PlayerNameSync nameSync =
            playerObject != null ? playerObject.GetComponent<PlayerNameSync>() : null;
        if (nameSync != null && !string.IsNullOrWhiteSpace(nameSync.PlayerName.ToString()))
            return nameSync.PlayerName.ToString();

        int connectedPlayerIndex = connectedPlayers.IndexOf(player);
        int fallbackNumber =
            connectedPlayerIndex >= 0 ? connectedPlayerIndex + 1 : player.PlayerId + 1;
        return $"Player {fallbackNumber}";
    }

    private Sprite GetPlayerProfileSprite(PlayerRef player)
    {
        if (player == PlayerRef.None)
            return null;

        int playerIndex = GetMatchPlayerIndex(player);
        if (playerIndex >= 0)
            return GetCharacterSprite(matchManager.PlayerProfiles.Get(playerIndex));

        NetworkObject playerObject = matchManager.Runner.GetPlayerObject(player);
        PlayerNameSync nameSync =
            playerObject != null ? playerObject.GetComponent<PlayerNameSync>() : null;
        if (nameSync != null)
            return GetCharacterSprite(nameSync.PlayerProfile);

        return null;
    }

    private Sprite GetCharacterSprite(int profileIndex)
    {
        if (
            characterDetails == null
            || characterDetails.charaacterDetails == null
            || profileIndex < 0
            || profileIndex >= characterDetails.charaacterDetails.Count
        )
            return null;

        return characterDetails.charaacterDetails[profileIndex].characterImage;
    }

}
