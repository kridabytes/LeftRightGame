using System.Collections.Generic;
using Edlore.GamePages;
using Edlore.Utilities;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResultManager : BasePage
{
    public TMP_Text winPlayerText;
    public TMP_Text winScoreText;
    public Transform playerListContainer;
    public Button exitButton;
    public Button playAgainButton;
    public GameObject playerDetailPrefab;

    public string homeSceneName = "HomeScene"; // Set this to your home scene name
    public string selectLevelSceneName = "SelectMatchPanel";

    private void Awake()
    {
        PageFactory.Add(this);
    }

    private void OnEnable()
    {
        exitButton.onClick.AddListener(OnExitClicked);
        playAgainButton.onClick.AddListener(OnPlayAgainClicked);
    }

    private void OnDisable()
    {
        exitButton.onClick.RemoveListener(OnExitClicked);
        playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
    }

    public void ShowResult(string playerindex, string score, List<PlayerInfo> playerInfoList)
    {
        Debug.Log($"Result: {playerindex} - {score}");

        ShowPage();
        winPlayerText.text = playerindex;
        winScoreText.text = score;

        // Clear existing player details
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        // Populate player details
        foreach (var playerInfo in playerInfoList)
        {
            GameObject playerDetailGO = new GameObject("PlayerDetail");
            playerDetailGO.transform.SetParent(playerListContainer);

            ResultPlayerListItemBehaviour playerDetail = Instantiate(playerDetailPrefab, playerListContainer).GetComponent<ResultPlayerListItemBehaviour>();
            playerDetail.Init(playerInfo);

            Debug.Log(
                $"[ResultManager] Player: {playerInfo.PlayerName}, Profile: {playerInfo.PlayerProfile}, Score: {playerInfo.PlayerScore}"
            );
        }
    }

    private async void OnExitClicked()
    {
        GameEvents.OnPlaySound();
        Debug.Log("Exit button clicked.");
        if (FusionConnectionManager.Instance != null)
        {
            await FusionConnectionManager.Instance.DisconnectRunner();

            Destroy(FusionConnectionManager.Instance.gameObject);
        }

        SceneManager.LoadScene(homeSceneName);
    }

    private async void OnPlayAgainClicked()
    {
        GameEvents.OnPlaySound();
        Debug.Log("Play Again button clicked.");
        if (FusionConnectionManager.Instance != null)
        {
            await FusionConnectionManager.Instance.DisconnectRunner();

            Destroy(FusionConnectionManager.Instance.gameObject);
        }

        SceneManager.LoadScene(selectLevelSceneName);
    }
}
