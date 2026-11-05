using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Edlore.Utilities;
using Fusion;
using Fusion.Photon.Realtime;
using Fusion.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FusionConnectionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static FusionConnectionManager Instance { get; private set; }

    public enum MatchType
    {
        QuickMatch,
        PrivateMatch,
    }

    private MatchType currentMatchType;

    [Header("Session Settings")]
    [SerializeField]
    private int maxPlayers = 2;

    [SerializeField]
    private bool persistBetweenScenes = true;

    [SerializeField, Min(1f)]
    private float quickMatchTimeoutSeconds = 60f;

    public NetworkRunner Runner { get; private set; }
    public bool IsConnected => Runner != null && Runner.IsRunning;
    public bool IsHost => IsConnected && Runner.IsSharedModeMasterClient;
    public bool IsPrivateMatch => currentMatchType == MatchType.PrivateMatch;
    public bool IsConnecting { get; private set; }
    public int ConnectedPlayerCount { get; private set; }

    public event Action OnConnected;
    public event Action<PlayerRef> OnPlayerJoinedGame;
    public event Action<PlayerRef> OnPlayerLeftGame;
    public event Action<string> OnConnectionFailed;
    public event Action OnDisconnected;

    private NetworkSceneManagerDefault sceneManager;
    private GameObject runnerGameObject;
    private string connectionError;
    private Task disconnectTask;

    [SerializeField]
    private NetworkObject playerPrefab;

    [SerializeField]
    private Transform playerListContainer;

    // [SerializeField]
    // private TMP_Text playerCountText;

    [SerializeField]
    private PlayerDetailsBehaviour playerDetailsPrefab;
    public event Action OnMatchReady;

    [SerializeField]
    private string gameplaySceneName = "Gameplay";

    [Header("Matchmaking Countdown")]
    [SerializeField]
    private TMP_Text countdownText;

    [SerializeField]
    public TMP_Text quickMatchCountdownText;

    private bool isLoadingGameplay;
    private readonly Dictionary<PlayerRef, PlayerDetailsBehaviour> playerDetails = new();
    private readonly Dictionary<PlayerRef, string> gameplayPlayerNames = new();
    private readonly Dictionary<PlayerRef, int> gameplayPlayerProfiles = new();

    [Header("Quick Match Player Settings")]
    [SerializeField]
    private NetworkObject quickMatchPlayerPrefab;

    [SerializeField]
    private Transform quickMatchPlayerListContainer;

    [Header("Private Match Player Settings")]
    [SerializeField]
    private NetworkObject privateMatchPlayerPrefab;

    [SerializeField]
    private Transform privateMatchPlayerListContainer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // if (playerPrefab == null && playerDetailsPrefab != null)
        //     playerPrefab = playerDetailsPrefab.GetComponent<NetworkObject>();

        if (quickMatchPlayerPrefab == null && playerDetailsPrefab != null)
            quickMatchPlayerPrefab = playerDetailsPrefab.GetComponent<NetworkObject>();

        if (persistBetweenScenes)
            DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Runner != null && Runner.IsRunning)
            RefreshPlayerList(Runner);
    }

    // Quick Match: find or create a session with available space.
    public async Task<bool> QuickMatch()
    {
        currentMatchType = MatchType.QuickMatch;

        Debug.Log(
            $"[QuickMatch] Search started. Max players: {maxPlayers}, timeout: {quickMatchTimeoutSeconds}s.",
            this
        );
        connectionError = null;
        bool connected = await ConnectToSession(
            "QuickMatch",
            GameMode.Shared,
            MatchmakingMode.FillRoom
        );
        if (!connected)
        {
            Debug.LogWarning("[QuickMatch] Could not join a session.", this);
            return false;
        }

        if (connected)
        {
            Debug.Log(
                $"[QuickMatch] Joined session '{GetSessionName()}'. Waiting for {maxPlayers} ready players.",
                this
            );
            return true;
        }

        string error = string.IsNullOrEmpty(connectionError)
            ? "Quick Match timed out while waiting for four players and their names."
            : connectionError;
        Debug.LogWarning(error, this);
        OnConnectionFailed?.Invoke(error);
        await Disconnect();
        return false;
    }

    // Private Match: create a session using a room code.
    public async Task<bool> CreatePrivateMatch(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            OnConnectionFailed?.Invoke("Room code cannot be empty.");
            return false;
        }

        currentMatchType = MatchType.PrivateMatch;

        return await ConnectToSession(roomCode.Trim(), GameMode.Shared);
    }

    // Private Match: join an existing session using its room code.
    public async Task<bool> JoinPrivateMatch(string roomCode)
    {
        if (string.IsNullOrWhiteSpace(roomCode))
        {
            OnConnectionFailed?.Invoke("Room code cannot be empty.");
            return false;
        }

        currentMatchType = MatchType.PrivateMatch;

        return await ConnectToSession(roomCode.Trim(), GameMode.Shared);
    }

    private async Task<bool> ConnectToSession(
        string sessionName,
        GameMode gameMode,
        MatchmakingMode matchmakingMode = MatchmakingMode.FillRoom
    )
    {
        if (IsConnecting)
        {
            Debug.LogWarning(
                $"[Fusion] Ignoring connection request for '{sessionName}'; a connection is already in progress.",
                this
            );
            return false;
        }

        IsConnecting = true;
        try
        {
            if (disconnectTask != null && !disconnectTask.IsCompleted)
                await disconnectTask;

            if (Runner != null)
            {
                Debug.Log(
                    $"[Fusion] Disconnecting from existing runner before joining '{sessionName}'.",
                    this
                );
                await Disconnect();
            }

            Debug.Log(
                $"[Fusion] Starting {gameMode} session '{sessionName ?? "<auto>"}' with max {maxPlayers}.",
                this
            );

            // Fusion runners cannot be restarted after shutdown.
            runnerGameObject = new GameObject("Fusion NetworkRunner");
            Runner = runnerGameObject.AddComponent<NetworkRunner>();

            Runner.ProvideInput = true;
            Runner.AddCallbacks(this);

            sceneManager = runnerGameObject.AddComponent<NetworkSceneManagerDefault>();

            var startArgs = new StartGameArgs
            {
                GameMode = gameMode,
                SessionName = sessionName,
                PlayerCount = maxPlayers,
                MatchmakingMode = matchmakingMode,
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
                SceneManager = sceneManager,
            };

            StartGameResult result = await Runner.StartGame(startArgs);

            if (!result.Ok)
            {
                string error = result.ShutdownReason.ToString();
                Debug.LogError(
                    $"[Fusion] StartGame failed for '{sessionName ?? "<auto>"}': {error}.",
                    this
                );
                OnConnectionFailed?.Invoke(error);
                await Disconnect();
                return false;
            }

            Debug.Log(
                $"[Fusion] Connected to session '{GetSessionName()}'; local player is {Runner.LocalPlayer}.",
                this
            );
            OnConnected?.Invoke();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            OnConnectionFailed?.Invoke(exception.Message);
            await Disconnect();
            return false;
        }
        finally
        {
            IsConnecting = false;
        }
    }

    public Task Disconnect()
    {
        if (disconnectTask != null && !disconnectTask.IsCompleted)
            return disconnectTask;

        disconnectTask = DisconnectRunner();
        return disconnectTask;
    }

    public async Task DisconnectRunner()
    {
        NetworkRunner runner = Runner;
        GameObject runnerObject = runnerGameObject;

        Debug.Log($"[Fusion] Disconnect requested. Active session: '{GetSessionName()}'.", this);

        Runner = null;
        runnerGameObject = null;
        ConnectedPlayerCount = 0;
        isLoadingGameplay = false;
        gameplayPlayerNames.Clear();
        gameplayPlayerProfiles.Clear();
        UpdatePlayerCountDisplay();
        playerDetails.Clear();

        if (runner != null)
        {
            runner.RemoveCallbacks(this);

            if (runner.IsRunning)
                await runner.Shutdown(destroyGameObject: false);
        }

        if (runnerObject != null)
            Destroy(runnerObject);

        sceneManager = null;
        HideCountdown();

        Debug.Log("[Fusion] Runner disconnected.", this);

        OnDisconnected?.Invoke();

        while (runnerObject != null)
            await Task.Yield();
    }

    public string GetSessionName()
    {
        return Runner != null && Runner.IsRunning ? Runner.SessionInfo.Name : string.Empty;
    }

    public bool IsSessionFull()
    {
        return ConnectedPlayerCount >= maxPlayers;
    }

    public void StartPrivateMatch()
    {
        if (!IsPrivateMatch || !IsHost)
        {
            Debug.LogWarning("[PrivateMatch] Only the room host can start the match.", this);
            return;
        }

        if (isLoadingGameplay)
            return;

        LoadGameplayScene(Runner);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Debug.Log("[Fusion] Connection manager is being destroyed.", this);
            Instance = null;

            if (Runner != null && Runner.IsRunning)
                Runner.Shutdown(destroyGameObject: true);
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        ConnectedPlayerCount = runner.ActivePlayers.Count();
        UpdatePlayerCountDisplay();

        Debug.Log(
            $"[Fusion] Player joined: {player}, Local: {player == runner.LocalPlayer}, "
                + $"Count: {ConnectedPlayerCount}/{maxPlayers}",
            this
        );

        // Spawn only the local player's network object.
        if (player == runner.LocalPlayer && runner.GetPlayerObject(player) == null)
        {
            NetworkObject currentPrefab = GetCurrentPlayerPrefab();

            if (currentPrefab == null)
            {
                string error =
                    $"{currentMatchType} player prefab is missing or has no NetworkObject.";

                connectionError = error;
                Debug.LogError(error, this);
                OnConnectionFailed?.Invoke(error);
                return;
            }

            NetworkObject networkPlayer = runner.Spawn(currentPrefab);

            if (networkPlayer == null)
            {
                const string error = "Failed to spawn the network player.";

                connectionError = error;
                Debug.LogError(error, this);
                OnConnectionFailed?.Invoke(error);
                return;
            }

            runner.SetPlayerObject(player, networkPlayer);

            PlayerNameSync nameSync = networkPlayer.GetComponent<PlayerNameSync>();

            if (nameSync == null)
            {
                Debug.LogError("Player prefab is missing PlayerNameSync.", networkPlayer);
            }
            else if (networkPlayer.HasStateAuthority)
            {
                string localName = PlayerPrefs.GetString("UserName", "Player");
                int localProfile = PlayerPrefs.GetInt("SelectedCharacter", 0);
                nameSync.SetPlayerName(localName, localProfile);
            }

            Debug.Log(
                $"[{currentMatchType}] Local player object spawned for {player}.",
                networkPlayer
            );
        }
        RefreshPlayerList(runner);
        OnPlayerJoinedGame?.Invoke(player);

        if (currentMatchType == MatchType.QuickMatch && runner.ActivePlayers.Count() >= maxPlayers)
        {
            LoadGameplayScene(runner);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        ConnectedPlayerCount = runner.ActivePlayers.Count();
        UpdatePlayerCountDisplay();
        Debug.Log(
            $"[Fusion] Player left: {player}; count {ConnectedPlayerCount}/{maxPlayers}.",
            this
        );

        if (playerDetails.ContainsKey(player))
            playerDetails.Remove(player);

        OnPlayerLeftGame?.Invoke(player);
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        ConnectedPlayerCount = 0;
        UpdatePlayerCountDisplay();
        playerDetails.Clear();
        gameplayPlayerNames.Clear();
        gameplayPlayerProfiles.Clear();
        Debug.Log($"Fusion shutdown: {shutdownReason}");
    }

    public bool TryGetGameplayPlayerName(PlayerRef player, out string playerName)
    {
        return gameplayPlayerNames.TryGetValue(player, out playerName);
    }

    public bool TryGetGameplayPlayerProfile(PlayerRef player, out int playerProfile)
    {
        return gameplayPlayerProfiles.TryGetValue(player, out playerProfile);
    }

    // The lobby player objects are destroyed by the Gameplay scene load, so their
    // synced name and profile are captured here for MatchManager to publish.
    private void CaptureGameplayPlayerNames(NetworkRunner runner)
    {
        gameplayPlayerNames.Clear();
        gameplayPlayerProfiles.Clear();

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            NetworkObject playerObject = runner.GetPlayerObject(player);
            PlayerNameSync nameSync =
                playerObject != null ? playerObject.GetComponent<PlayerNameSync>() : null;

            if (nameSync != null)
            {
                gameplayPlayerProfiles[player] = nameSync.PlayerProfile;
                Debug.Log(
                    $"[Gameplay] Captured profile {nameSync.PlayerProfile} for {player}.",
                    this
                );
            }

            if (nameSync == null || string.IsNullOrWhiteSpace(nameSync.PlayerName.ToString()))
            {
                Debug.LogWarning(
                    $"Could not capture a display name for {player} before the Gameplay scene load.",
                    this
                );
                continue;
            }

            gameplayPlayerNames[player] = nameSync.PlayerName.ToString();
        }
    }

    private bool AreAllQuickMatchPlayersReady(NetworkRunner runner)
    {
        if (runner.ActivePlayers.Count() != maxPlayers)
            return false;

        List<string> playerNames = new();
        foreach (PlayerRef player in runner.ActivePlayers)
        {
            NetworkObject networkPlayer = runner.GetPlayerObject(player);
            if (networkPlayer == null)
                return false;

            PlayerNameSync nameSync = networkPlayer.GetComponent<PlayerNameSync>();
            if (nameSync == null || string.IsNullOrWhiteSpace(nameSync.PlayerName.ToString()))
                return false;

            playerNames.Add($"{player}: {nameSync.PlayerName}");
        }

        Debug.Log($"[QuickMatch] Ready player data: {string.Join(", ", playerNames)}.", this);
        return true;
    }

    private void RefreshPlayerList(NetworkRunner runner)
    {
        ConnectedPlayerCount = runner.ActivePlayers.Count();
        UpdatePlayerCountDisplay();
        Transform currentContainer = GetCurrentPlayerListContainer();

        if (currentContainer == null)
            return;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            NetworkObject networkPlayer = runner.GetPlayerObject(player);

            if (networkPlayer == null)
                continue;

            PlayerNameSync nameSync = networkPlayer.GetComponent<PlayerNameSync>();

            PlayerDetailsBehaviour details = networkPlayer.GetComponent<PlayerDetailsBehaviour>();

            if (details == null)
            {
                Debug.LogWarning(
                    $"Player prefab has no PlayerDetailsBehaviour: {player}",
                    networkPlayer
                );
                continue;
            }

            if (
                !playerDetails.TryGetValue(player, out PlayerDetailsBehaviour currentDetails)
                || currentDetails == null
            )
            {
                playerDetails[player] = details;
            }

            if (details.transform.parent != currentContainer)
            {
                details.transform.SetParent(currentContainer, false);
            }

            string displayName =
                nameSync != null && !string.IsNullOrWhiteSpace(nameSync.PlayerName.ToString())
                    ? nameSync.PlayerName.ToString()
                    : $"Player {player.PlayerId}";

            int playerProfile = nameSync != null ? nameSync.PlayerProfile : 0;

            details.Init(displayName, playerProfile);
        }
    }

    private async void LoadGameplayScene(NetworkRunner runner)
    {
        if (isLoadingGameplay || runner == null || !runner.IsRunning)
            return;

        isLoadingGameplay = true;

        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        for (int i = 5; i > 0; i--)
        {
            if (runner == null || !runner.IsRunning)
            {
                HideCountdown();
                isLoadingGameplay = false;
                return;
            }

            if (countdownText != null)
                countdownText.text = $"Game starts in {i}...";

            await System.Threading.Tasks.Task.Delay(1000);
        }

        if (runner == null || !runner.IsRunning)
        {
            HideCountdown();
            isLoadingGameplay = false;
            return;
        }

        // Only Master Client loads the Gameplay scene.
        if (runner.IsSharedModeMasterClient)
        {
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath(
                $"Assets/Scenes/{gameplaySceneName}.unity"
            );

            if (sceneIndex < 0)
                sceneIndex = SceneManager.GetSceneByName(gameplaySceneName).buildIndex;

            if (sceneIndex < 0)
            {
                Debug.LogError($"Gameplay scene '{gameplaySceneName}' was not found.", this);

                HideCountdown();
                isLoadingGameplay = false;
                return;
            }

            if (countdownText != null)
                countdownText.text = "GO!";

            await System.Threading.Tasks.Task.Delay(500);

            if (runner != null && runner.IsRunning)
            {
                CaptureGameplayPlayerNames(runner);
                await runner.LoadScene(SceneRef.FromIndex(sceneIndex));
            }
        }
    }

    private void HideCountdown()
    {
        if (currentMatchType == MatchType.PrivateMatch)
        {
            UpdatePlayerCountDisplay();
            return;
        }

        if (countdownText != null)
            countdownText.text = "Searching...";
    }

    private void UpdatePlayerCountDisplay()
    {
        if (currentMatchType == MatchType.PrivateMatch)
        {
            var privateMatchManager = PageFactory.Get<PrivateMatchManager>();
            if (privateMatchManager != null && privateMatchManager.playerCountText != null)
            {
                countdownText = privateMatchManager.playerCountText;
            }
        }
        else if (currentMatchType == MatchType.QuickMatch)
        {
           countdownText = quickMatchCountdownText;
            return;
        }

        countdownText.gameObject.SetActive(true);

        int playersNeeded = Mathf.Max(0, maxPlayers - ConnectedPlayerCount);
        if (playersNeeded > 0)
        {
            string playerWord = playersNeeded == 1 ? "player" : "players";
            countdownText.text = $"Waiting for {playersNeeded} more {playerWord}";
        }
        else
        {
            countdownText.text = "Waiting for host to start...";
        }
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[Fusion] Disconnected from server: {reason}.", this);
        connectionError = reason.ToString();
        OnConnectionFailed?.Invoke(reason.ToString());
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log($"[Fusion] Connected to server. Local player: {runner.LocalPlayer}.", this);
    }

    public void OnConnectFailed(
        NetworkRunner runner,
        NetAddress remoteAddress,
        NetConnectFailedReason reason
    )
    {
        Debug.LogError($"[Fusion] Connect failed at {remoteAddress}: {reason}.", this);
        connectionError = reason.ToString();
        OnConnectionFailed?.Invoke(reason.ToString());
    }

    public void OnConnectRequest(
        NetworkRunner runner,
        NetworkRunnerCallbackArgs.ConnectRequest request,
        byte[] token
    )
    {
        Debug.Log($"[Fusion] Accepting connection request from {request.RemoteAddress}.", this);
        request.Accept();
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    public void OnInput(NetworkRunner runner, NetworkInput input) { }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnCustomAuthenticationResponse(
        NetworkRunner runner,
        Dictionary<string, object> data
    ) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnReliableDataReceived(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        ArraySegment<byte> data
    ) { }

    public void OnReliableDataProgress(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        float progress
    ) { }

    public void OnSceneLoadDone(NetworkRunner runner) { }

    public void OnSceneLoadStart(NetworkRunner runner) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    #region Prefabs and Cointainer healper methods
    private NetworkObject GetCurrentPlayerPrefab()
    {
        return currentMatchType switch
        {
            MatchType.QuickMatch => quickMatchPlayerPrefab,
            MatchType.PrivateMatch => privateMatchPlayerPrefab,
            _ => null,
        };
    }

    private Transform GetCurrentPlayerListContainer()
    {
        return currentMatchType switch
        {
            MatchType.QuickMatch => quickMatchPlayerListContainer,
            MatchType.PrivateMatch => privateMatchPlayerListContainer,
            _ => null,
        };
    }
    #endregion
}
