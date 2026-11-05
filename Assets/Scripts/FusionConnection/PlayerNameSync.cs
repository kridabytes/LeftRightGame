using Fusion;
using TMPro;
using UnityEngine;

public class PlayerNameSync : NetworkBehaviour
{
    [Networked]
    public NetworkString<_32> PlayerName { get; set; }
    [Networked]
    public int PlayerProfile { get; set; }

    [SerializeField]
    private TMP_Text playerNameText;

    private string lastLoggedDisplayName;

    public override void Spawned()
    {
        Runner.SetPlayerObject(Object.StateAuthority, Object);
        Debug.Log(
            $"[PlayerNameSync] Spawned for {Object.StateAuthority}; state authority: {Object.HasStateAuthority}.",
            this
        );

        RefreshPlayerName();

        if (Object.HasStateAuthority)
        {
            string localName = PlayerPrefs.GetString("UserName", "Player");
            Debug.Log($"[PlayerNameSync] Publishing local player name '{localName}'.", this);
            int localProfile = PlayerPrefs.GetInt("SelectedCharacter", 0);
            SetPlayerName(localName, localProfile);
        }
    }

    public void SetPlayerName(string playerName, int playerProfile)
    {
        if (!Object.HasStateAuthority)
            return;

        if (string.IsNullOrWhiteSpace(playerName))
            playerName = "Player";

        PlayerName = playerName.Trim();
        PlayerProfile = playerProfile;
        Debug.Log($"[PlayerNameSync] Name set to '{PlayerName}'. Profile: {PlayerProfile}", this);
        RefreshPlayerName();
    }

    public override void Render()
    {
        RefreshPlayerName();
    }

    private void RefreshPlayerName()
    {
        string displayName = PlayerName.ToString();
        if (string.IsNullOrEmpty(displayName))
            displayName = $"Player {Object.StateAuthority.PlayerId}";

        if (lastLoggedDisplayName != displayName)
        {
            Debug.Log($"[PlayerNameSync] Display name updated: '{displayName}'.", this);
            lastLoggedDisplayName = displayName;
        }

        if (playerNameText != null)
            playerNameText.text = displayName;

        
        PlayerDetailsBehaviour details = GetComponent<PlayerDetailsBehaviour>();
        if (details != null)
            details.Init(displayName, PlayerProfile);
        Debug.Log($"[PlayerNameSync] PlayerDetailsBehaviour initialized with name '{displayName}' and profile {PlayerProfile}.", this);
    }
}
