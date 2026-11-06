using System.Collections.Generic;
using DG.Tweening;
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

    // [SerializeField]
    // private TMP_Text turnTimerText;

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
    private bool hasDisplayedDirection;
    private TurnDirection displayedDirection;
    private int displayedPlayerIndex;
    private float lastTurnTimeRemaining;
    private Vector3 directionArrowBaseScale;
    private Vector2 directionArrowBasePosition;
    private Vector3 leftButtonBaseScale;
    private Vector3 rightButtonBaseScale;
    private Tween directionArrowTween;
    private Tween leftButtonTween;
    private Tween rightButtonTween;

    private void Awake()
    {
        PageFactory.Add(this);
        CacheAnimationDefaults();
        InitializePlayerControllers();
        UpdateInputControls();
    }

    private void OnDestroy()
    {
        directionArrowTween?.Kill();
        leftButtonTween?.Kill();
        rightButtonTween?.Kill();
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
        AnimateInputButton(leftInputButton, leftButtonBaseScale, ref leftButtonTween);
        if (!GameInputSettings.UseSwipeControls && matchManager != null && matchManager.HasSpawned)
            matchManager.SubmitLocalInput(PlayerDirectionInput.Left);
    }

    public void OnRightClicked()
    {
        AnimateInputButton(rightInputButton, rightButtonBaseScale, ref rightButtonTween);
        if (!GameInputSettings.UseSwipeControls && matchManager != null && matchManager.HasSpawned)
            matchManager.SubmitLocalInput(PlayerDirectionInput.Right);
    }

    private void CacheAnimationDefaults()
    {
        if (arrorwDirectionImage != null)
        {
            RectTransform arrowTransform = arrorwDirectionImage.rectTransform;
            directionArrowBaseScale = arrowTransform.localScale;
            directionArrowBasePosition = arrowTransform.anchoredPosition;
        }

        if (leftInputButton != null)
            leftButtonBaseScale = leftInputButton.transform.localScale;

        if (rightInputButton != null)
            rightButtonBaseScale = rightInputButton.transform.localScale;
    }

    private void AnimateInputButton(GameObject button, Vector3 baseScale, ref Tween tween)
    {
        if (button == null)
            return;

        tween?.Kill();
        Transform buttonTransform = button.transform;
        buttonTransform.localScale = baseScale;
        tween = buttonTransform.DOPunchScale(baseScale * 0.12f, 0.22f, 6, 0.5f).SetLink(gameObject);
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
        // UpdateTimerText(turnTimerText, matchManager.GetTurnTimeRemaining());
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

        TurnDirection expectedDirection = matchManager.ExpectedDirection;
        float turnTimeRemaining = matchManager.GetTurnTimeRemaining();
        bool nextTurnStarted =
            hasDisplayedDirection
            && (
                displayedPlayerIndex != currentIndex
                || turnTimeRemaining > lastTurnTimeRemaining + 0.1f
            );

        if (arrorwDirectionImage != null)
        {
            Sprite expectedSprite =
                expectedDirection == TurnDirection.Left ? leftSprite : rightSprite;

            if (!hasDisplayedDirection)
            {
                arrorwDirectionImage.sprite = expectedSprite;
                AnimateDirectionArrowIntro();
                hasDisplayedDirection = true;
            }
            else if (displayedDirection != expectedDirection || nextTurnStarted)
            {
                AnimateDirectionArrowChange(expectedSprite, expectedDirection);
            }
            else if (arrorwDirectionImage.sprite != expectedSprite)
            {
                arrorwDirectionImage.sprite = expectedSprite;
            }

            displayedDirection = expectedDirection;
            displayedPlayerIndex = currentIndex;
        }

        lastTurnTimeRemaining = turnTimeRemaining;

        if (leftButtonArrowImage != null)
            leftButtonArrowImage.sprite = leftSprite;

        if (rightButtonArrowImage != null)
            rightButtonArrowImage.sprite = rightSprite;
    }

    private void AnimateDirectionArrowIntro()
    {
        RectTransform arrowTransform = arrorwDirectionImage.rectTransform;
        directionArrowTween?.Kill();
        arrowTransform.localScale = directionArrowBaseScale * 0.75f;
        directionArrowTween = arrowTransform
            .DOScale(directionArrowBaseScale, 0.32f)
            .SetEase(Ease.OutBack)
            .SetLink(gameObject);
    }

    private void AnimateDirectionArrowChange(Sprite expectedSprite, TurnDirection direction)
    {
        RectTransform arrowTransform = arrorwDirectionImage.rectTransform;
        directionArrowTween?.Kill();
        arrowTransform.anchoredPosition = directionArrowBasePosition;
        arrowTransform.localScale = directionArrowBaseScale;

        float offset = direction == TurnDirection.Left ? -28f : 28f;
        Vector2 directionPosition = directionArrowBasePosition + Vector2.right * offset;
        directionArrowTween = DOTween
            .Sequence()
            .Append(arrowTransform.DOAnchorPos(directionPosition, 0.12f).SetEase(Ease.InQuad))
            .Join(arrowTransform.DOScale(directionArrowBaseScale * 0.8f, 0.12f))
            .AppendCallback(() => arrorwDirectionImage.sprite = expectedSprite)
            .Append(
                arrowTransform.DOAnchorPos(directionArrowBasePosition, 0.22f).SetEase(Ease.OutBack)
            )
            .Join(arrowTransform.DOScale(directionArrowBaseScale, 0.22f).SetEase(Ease.OutBack))
            .SetLink(gameObject);
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
