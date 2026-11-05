using System;
using System.Collections.Generic;
using Edlore.Utilities;
using Fusion;
using UnityEngine;

public enum TurnDirection
{
    Left,
    Right,
}

public enum PlayerDirectionInput
{
    Left,
    Right,
}

public class MatchManager : NetworkBehaviour
{
    private const int MaxPlayers = 4;
    private const int MinimumPlayers = 1;
    private const float MatchDuration = 60f;
    private const float QuickMatchTurnTimeSeconds = 5f;

    [SerializeField, Min(1f)]
    private float turnTimeoutSeconds = 5f;

    [SerializeField]
    private Sprite[] leftArrowSprites;

    [SerializeField]
    private Sprite[] rightArrowSprites;

    private MatchSettingsManager.MatchSettings matchSettings;

    public bool HasSpawned { get; private set; }

    [Networked]
    public NetworkBool IsMatchRunning { get; private set; }

    [Networked]
    public NetworkBool IsMatchFinished { get; private set; }

    [Networked]
    public int CurrentPlayerIndex { get; private set; }

    [Networked]
    public int ActivePlayerCount { get; private set; }

    [Networked]
    public int SharedPot { get; private set; }

    [Networked]
    public TickTimer MatchTimer { get; private set; }

    [Networked]
    public TickTimer TurnTimer { get; private set; }

    [Networked]
    public float TurnDurationSeconds { get; private set; }

    [Networked]
    public TurnDirection ExpectedDirection { get; private set; }

    [Networked]
    public int SelectedArrowIndex { get; private set; }

    [Networked, Capacity(MaxPlayers)]
    public NetworkArray<PlayerRef> Players => default;

    [Networked, Capacity(MaxPlayers)]
    public NetworkArray<int> PlayerScores => default;

    [Networked, Capacity(MaxPlayers)]
    public NetworkArray<NetworkBool> PlayerReady => default;

    [Networked, Capacity(MaxPlayers)]
    public NetworkArray<NetworkBool> PlayerConnected => default;

    [Networked]
    public int WinnerIndex { get; private set; } = -1;

    [Networked, Capacity(4)]
    public NetworkArray<NetworkString<_32>> PlayerNames => default;

    [Networked, Capacity(MaxPlayers)]
    public NetworkArray<int> PlayerProfiles => default;

    [Networked, Capacity(MaxPlayers)]
    public NetworkArray<PlayerInfo> ResultPlayers => default;

    public override void Spawned()
    {
        HasSpawned = true;

        if (Object.HasStateAuthority)
        {
            FusionConnectionManager connectionManager = FusionConnectionManager.Instance;
            matchSettings =
                connectionManager != null && connectionManager.IsPrivateMatch
                    ? MatchSettingsManager.GetSavedSettings(QuickMatchTurnTimeSeconds)
                    : CreateQuickMatchSettings();
            turnTimeoutSeconds = matchSettings.TurnTime;
            SelectedArrowIndex = Array.FindIndex(
                matchSettings.SelectedArrows,
                selected => selected
            );
            if (SelectedArrowIndex < 0)
                SelectedArrowIndex = 0;

            IsMatchRunning = false;
            IsMatchFinished = false;
            CurrentPlayerIndex = 0;
            ActivePlayerCount = 0;
            SharedPot = 0;
            WinnerIndex = -1;

            for (int i = 0; i < MaxPlayers; i++)
            {
                Players.Set(i, PlayerRef.None);
                PlayerScores.Set(i, 0);
                PlayerReady.Set(i, false);
                PlayerConnected.Set(i, false);
                PlayerNames.Set(i, default);
                PlayerProfiles.Set(i, 0);
            }

            RefreshPlayers();
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        HasSpawned = false;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || IsMatchFinished)
            return;

        RefreshPlayerNames();

        if (!IsMatchRunning)
        {
            RefreshPlayers();
            TryStartMatch();
            return;
        }

        if (MatchTimer.Expired(Runner))
        {
            FinishMatch();
            return;
        }

        if (TurnTimer.Expired(Runner))
        {
            AdvanceTurn();
        }
    }

    private void RefreshPlayers()
    {
        if (IsMatchRunning || IsMatchFinished)
            return;

        var activePlayers = new List<PlayerRef>(Runner.ActivePlayers);
        activePlayers.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));

        for (int i = 0; i < MaxPlayers; i++)
        {
            PlayerRef nextPlayer = i < activePlayers.Count ? activePlayers[i] : PlayerRef.None;

            PlayerRef oldPlayer = Players.Get(i);

            if (oldPlayer == nextPlayer)
                continue;

            Players.Set(i, nextPlayer);
            PlayerReady.Set(i, false);
            PlayerConnected.Set(i, nextPlayer != PlayerRef.None);
            PlayerNames.Set(i, default);
            PlayerProfiles.Set(i, 0);
        }
    }

    private void RefreshPlayerNames()
    {
        FusionConnectionManager connectionManager = FusionConnectionManager.Instance;

        for (int i = 0; i < MaxPlayers; i++)
        {
            PlayerRef player = Players.Get(i);
            if (player == PlayerRef.None)
            {
                PlayerNames.Set(i, default);
                PlayerProfiles.Set(i, 0);
                continue;
            }

            // The lobby player objects do not survive the Gameplay scene load, so this is
            // usually null here and the values captured before the load are used instead.
            NetworkObject playerObject = Runner.GetPlayerObject(player);
            PlayerNameSync nameSync =
                playerObject != null ? playerObject.GetComponent<PlayerNameSync>() : null;

            NetworkString<_32> playerName = default;
            int playerProfile = 0;
            bool hasProfile = false;

            if (nameSync != null && !string.IsNullOrWhiteSpace(nameSync.PlayerName.ToString()))
            {
                playerName = nameSync.PlayerName;
                playerProfile = nameSync.PlayerProfile;
                hasProfile = true;
            }
            else if (connectionManager != null)
            {
                if (connectionManager.TryGetGameplayPlayerName(player, out string capturedName))
                    playerName = capturedName;

                hasProfile = connectionManager.TryGetGameplayPlayerProfile(
                    player,
                    out playerProfile
                );
            }

            if (playerName.Length > 0 && PlayerNames.Get(i) != playerName)
                PlayerNames.Set(i, playerName);

            if (hasProfile && PlayerProfiles.Get(i) != playerProfile)
            {
                PlayerProfiles.Set(i, playerProfile);
                Debug.Log(
                    $"[MatchManager] Player {player.PlayerId} (index {i}) profile set to {playerProfile}."
                );
            }
        }
    }

    public void SetLocalPlayerReady(bool ready)
    {
        if (!HasSpawned || IsMatchRunning || IsMatchFinished)
            return;

        RPC_SetReady(ready);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetReady(NetworkBool ready, RpcInfo info = default)
    {
        if (IsMatchRunning || IsMatchFinished)
            return;

        int index = FindPlayerIndex(info.Source);

        if (index < 0)
            return;

        PlayerReady.Set(index, ready);
        TryStartMatch();
    }

    private void TryStartMatch()
    {
        if (IsMatchRunning || IsMatchFinished)
            return;

        int connectedPlayerCount = 0;
        for (int i = 0; i < MaxPlayers; i++)
        {
            if (Players.Get(i) != PlayerRef.None && PlayerConnected.Get(i))
                connectedPlayerCount++;
        }

        if (connectedPlayerCount < MinimumPlayers)
            return;

        ActivePlayerCount = connectedPlayerCount;
        StartMatch();
    }

    private void StartMatch()
    {
        if (matchSettings.Order == MatchSettingsManager.MatchOrder.Random)
            RandomizePlayerOrder();

        for (int i = 0; i < MaxPlayers; i++)
            PlayerScores.Set(i, 0);

        SharedPot = 0;
        CurrentPlayerIndex = 0;
        WinnerIndex = -1;

        IsMatchRunning = true;

        MatchTimer = TickTimer.CreateFromSeconds(Runner, MatchDuration);

        BeginTurn();
    }

    private void RandomizePlayerOrder()
    {
        var players = new PlayerRef[ActivePlayerCount];
        var playerNames = new NetworkString<_32>[ActivePlayerCount];
        var playerProfiles = new int[ActivePlayerCount];

        for (int i = 0; i < ActivePlayerCount; i++)
        {
            players[i] = Players.Get(i);
            playerNames[i] = PlayerNames.Get(i);
            playerProfiles[i] = PlayerProfiles.Get(i);
        }

        for (int i = players.Length - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (players[i], players[randomIndex]) = (players[randomIndex], players[i]);
            (playerNames[i], playerNames[randomIndex]) = (playerNames[randomIndex], playerNames[i]);
            (playerProfiles[i], playerProfiles[randomIndex]) = (
                playerProfiles[randomIndex],
                playerProfiles[i]
            );
        }

        for (int i = 0; i < ActivePlayerCount; i++)
        {
            Players.Set(i, players[i]);
            PlayerNames.Set(i, playerNames[i]);
            PlayerProfiles.Set(i, playerProfiles[i]);
        }
    }

    private void BeginTurn()
    {
        if (!IsMatchRunning)
            return;

        ExpectedDirection =
            UnityEngine.Random.Range(0, 2) == 0 ? TurnDirection.Left : TurnDirection.Right;

        TurnDurationSeconds = turnTimeoutSeconds;
        TurnTimer = TickTimer.CreateFromSeconds(Runner, turnTimeoutSeconds);
    }

    private static MatchSettingsManager.MatchSettings CreateQuickMatchSettings()
    {
        return new MatchSettingsManager.MatchSettings
        {
            TurnTime = QuickMatchTurnTimeSeconds,
            Order = MatchSettingsManager.MatchOrder.Circular,
            SelectedArrows = new[] { true, false, false, false, false },
        };
    }

    public void SubmitLocalInput(PlayerDirectionInput input)
    {
        if (!HasSpawned || !IsMatchRunning || IsMatchFinished)
            return;

        RPC_SubmitInput(input);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SubmitInput(PlayerDirectionInput input, RpcInfo info = default)
    {
        if (!IsMatchRunning || IsMatchFinished)
            return;

        if (MatchTimer.Expired(Runner) || TurnTimer.Expired(Runner))
        {
            if (MatchTimer.Expired(Runner))
                FinishMatch();
            else
                AdvanceTurn();

            return;
        }

        int senderIndex = FindPlayerIndex(info.Source);

        // Only the active player may submit an input.
        if (senderIndex != CurrentPlayerIndex)
            return;

        bool correct =
            (input == PlayerDirectionInput.Left && ExpectedDirection == TurnDirection.Left)
            || (input == PlayerDirectionInput.Right && ExpectedDirection == TurnDirection.Right);

        if (correct)
        {
            // Correct input: add one point to the shared pot.
            SharedPot++;
        }
        else
        {
            // Wrong input: transfer the pot to this player.
            int score = PlayerScores.Get(senderIndex);
            PlayerScores.Set(senderIndex, score + SharedPot);
            SharedPot = 0;
        }

        AdvanceTurn();
    }

    private void AdvanceTurn()
    {
        if (!IsMatchRunning || IsMatchFinished)
            return;

        if (MatchTimer.Expired(Runner))
        {
            FinishMatch();
            return;
        }

        CurrentPlayerIndex = (CurrentPlayerIndex + 1) % ActivePlayerCount;

        BeginTurn();
    }

    private int FindPlayerIndex(PlayerRef player)
    {
        for (int i = 0; i < MaxPlayers; i++)
        {
            if (Players.Get(i) == player)
                return i;
        }

        return -1;
    }

    private void FinishMatch()
    {
        if (IsMatchFinished)
            return;

        IsMatchRunning = false;
        IsMatchFinished = true;
        WinnerIndex = -1;

        int lowestScore = int.MaxValue;
        bool tied = false;

        for (int i = 0; i < ActivePlayerCount; i++)
        {
            int score = PlayerScores.Get(i);

            if (score < lowestScore)
            {
                lowestScore = score;
                WinnerIndex = i;
                tied = false;
            }
            else if (score == lowestScore)
            {
                tied = true;
            }
        }

        if (tied)
            WinnerIndex = -1;

        for (int i = 0; i < ActivePlayerCount; i++)
        {
            ResultPlayers.Set(
                i,
                new PlayerInfo
                {
                    PlayerName = PlayerNames.Get(i),
                    PlayerProfile = PlayerProfiles.Get(i),
                    PlayerScore = PlayerScores.Get(i),
                }
            );
        }

        RPC_ShowResult(WinnerIndex, GetWinnerScore());
    }

    private int GetWinnerScore()
    {
        return WinnerIndex >= 0 ? PlayerScores.Get(WinnerIndex) : 0;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowResult(int winnerIndex, int winnerScore)
    {
        var resultManager = PageFactory.Get<ResultManager>();

        if (resultManager == null)
        {
            Debug.LogWarning("ResultManager not found on this client.");
            return;
        }

        string winnerName =
            winnerIndex >= 0 ? PlayerNames.Get(winnerIndex).ToString() : string.Empty;
        if (winnerIndex >= 0 && string.IsNullOrWhiteSpace(winnerName))
            winnerName = $"Player #{winnerIndex + 1}";

        string winnerText = winnerIndex >= 0 ? $"{winnerName} Won!" : "It's a Tie!";

        string scoreText = winnerIndex >= 0 ? $"{winnerScore} Coins" : "N/A";

        List<PlayerInfo> players = new List<PlayerInfo>();

        for (int i = 0; i < ActivePlayerCount; i++)
            players.Add(ResultPlayers.Get(i));

        resultManager.ShowResult(winnerText, scoreText, players);
    }

    public float GetMatchTimeRemaining()
    {
        if (!HasSpawned)
            return 0f;

        return MatchTimer.RemainingTime(Runner) ?? 0f;
    }

    public float GetTurnTimeRemaining()
    {
        if (!HasSpawned)
            return 0f;

        return TurnTimer.RemainingTime(Runner) ?? 0f;
    }

    public float GetTurnDuration()
    {
        if (!HasSpawned)
            return 0f;

        return TurnDurationSeconds;
    }

    public Sprite GetArrowSprite(TurnDirection direction)
    {
        Sprite[] arrowSprites =
            direction == TurnDirection.Left ? leftArrowSprites : rightArrowSprites;

        if (
            arrowSprites == null
            || SelectedArrowIndex < 0
            || SelectedArrowIndex >= arrowSprites.Length
        )
            return null;

        return arrowSprites[SelectedArrowIndex];
    }

    public void SetPlayerName(string playerName)
    {
        if (!HasSpawned)
            return;

        RPC_SetPlayerName(playerName);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerName(NetworkString<_32> playerName, RpcInfo info = default)
    {
        int index = FindPlayerIndex(info.Source);

        if (index < 0)
            return;

        PlayerNames.Set(index, playerName);
    }
}

public class PlayerInfoList
{
    public List<PlayerInfo> players = new List<PlayerInfo>();
}

public struct PlayerInfo : INetworkStruct
{
    public NetworkString<_32> PlayerName;
    public int PlayerProfile;
    public int PlayerScore;
}
