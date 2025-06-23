using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class GameLobbyManager : Singleton<GameLobbyManager>
{
    #region Variables
    [Header("Lobby Panel")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private Button refreshLobbiesBtn;
    [SerializeField] private GameObject roomInfoPrefab;
    [SerializeField] private GameObject roomInfoContent;
    [SerializeField] private TMP_InputField playerNameIF;

    [Space(10)]
    [Header("Create Room Panel")]
    [SerializeField] private TMP_InputField roomNameIF;
    [SerializeField] private TMP_InputField roomPasswordIF;
    [SerializeField] private Button createRoomBtn;

    [Space(10)]
    [Header("Room Panel")]
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI roomCodeText;
    [SerializeField] private GameObject playerInfoPrefab;
    [SerializeField] private GameObject playerInfoContent;
    [SerializeField] private Button leaveRoomBtn;
    [SerializeField] private Button startGameBtn;

    private CreateLobbyOptions createOptions;
    private JoinLobbyByIdOptions joinOptions;
    [HideInInspector] public Lobby currentRoom;
    private float heartbeatTimer = 15f;
    private float roomUpdateTimer = 2.5f;
    private string roomName;
    private int maxPlayers = 2; // max player alway 2

    #endregion

    #region Main Method
    void Start()
    {
        PlayerName();
    }

    void Update()
    {
        HandleRoomHeartbeat();
        HandleRoomUpdate();
    }

    void OnEnable()
    {
        createRoomBtn.onClick.AddListener(CreateRoom);
        refreshLobbiesBtn.onClick.AddListener(ListOfRooms);
        startGameBtn.onClick.AddListener(StartGame);
        leaveRoomBtn.onClick.AddListener(LeaveRoom);
    }
    void OnDisable()
    {
        // Prevent multiple click
        createRoomBtn.onClick.RemoveAllListeners();
        refreshLobbiesBtn.onClick.RemoveAllListeners();
        startGameBtn.onClick.RemoveAllListeners();
        leaveRoomBtn.onClick.RemoveAllListeners();
    }

    #endregion

    #region Room Management

    private async void CreateRoom()
    {
        try
        {
            roomName = roomNameIF.text;
            CreateLobbyOptions();
            // Check if the lobby name is empty

            // Check if the lobby name is current exist

            // Create a lobby with the specified name and max players
            // after the lobby is created, the host will enter the room
            currentRoom = await LobbyService.Instance.CreateLobbyAsync(roomName, maxPlayers, createOptions);
            EnterRoom();
            // After we create Room we want to create an allocation also...
            // ...So that the player could join on difference computer, wifi, etc. 
            string relayCode = await GameRelayManager.Instance.CreateRelay();
            Debug.Log("Lobby created: " + currentRoom.Name + " with ID: " + currentRoom.Id + " and Max Players: " + currentRoom.MaxPlayers);
        }
        // Handle the case where the lobby creation fails
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    // When the player enters the room, wen display the room panel
    // and update the room name and code
    private void EnterRoom()
    {
        roomNameText.text = currentRoom.Name;
        roomCodeText.text = currentRoom.LobbyCode;

        lobbyPanel.SetActive(false);
        roomPanel.SetActive(true);


        VisualizePlayerInRoom();
    }

    private void ExitRoom()
    {
        lobbyPanel.SetActive(true);
        roomPanel.SetActive(false);

        // Reset the room name and code text to default values
        // This is to clear the room information when the player exits the room
        roomNameText.text = "";
        roomCodeText.text = "";
        currentRoom = null;

        for (int i = 0; i < playerInfoContent.transform.childCount; i++)
        {
            // Destroy all the previous player info UI elements
            Destroy(playerInfoContent.transform.GetChild(i).gameObject);
        }
    }

    private async void JoinRoom(string roomID)
    {
        try
        {
            JoinLobbyByIDOptions();
            currentRoom = await LobbyService.Instance.JoinLobbyByIdAsync(roomID, joinOptions);
            EnterRoom();
            if (!CheckIfHost()) // Room Host already join Relay
            {
                GameRelayManager.Instance.JoinRelay(createOptions.Data["IsGameStarted"].Value);
            }
            Debug.Log("Player in the room: " + currentRoom.Players.Count);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }

    /// <summary>
    /// This function is called when play click "Leave Room" 
    /// </summary>
    /// <returns></returns>
    private async void LeaveRoom()
    {
        try
        {
            // If the player is the host, we need to find the next host before leaving the room
            // If the player is not the host, we can leave the room directly
            if (CheckIfHost() && currentRoom.Players.Count > 1)
            {
                string nextHostId = GetNextHostId();
                if (string.IsNullOrEmpty(nextHostId))
                {
                    await LobbyService.Instance.UpdateLobbyAsync(currentRoom.Id, new UpdateLobbyOptions
                    {
                        HostId = nextHostId // If no next host, the current player will be the host
                    });

                    Debug.Log("New host set to: " + nextHostId);
                }
            }

            // Remove the player from the room
            await LobbyService.Instance.RemovePlayerAsync(currentRoom.Id, AuthenticationService.Instance.PlayerId);
        
            // Disconnect internet, and relay
            if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.Shutdown();
            }

            ExitRoom();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private async void KickPlayer(string playerId)
    {
        try
        {
            if (CheckIfHost())
            {
                await LobbyService.Instance.RemovePlayerAsync(currentRoom.Id, playerId);
                Debug.Log("Player with ID: " + playerId + " has been kicked from the room.");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    // When the host click Start Button 
    // We need to update the value of the data to true...
    private async void StartGame()
    {
        if (currentRoom != null && CheckIfHost())
        {
            try
            {
                UpdateLobbyOptions updateLobbyOptions = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {"IsGameStarted", new DataObject(DataObject.VisibilityOptions.Member, "true")}
                    }
                };
                // ...and update the current room
                currentRoom = await LobbyService.Instance.UpdateLobbyAsync(currentRoom.Id, updateLobbyOptions);

                // Then both go to the game
                NetworkManager.Singleton.SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    #endregion

    #region Room UI Visualization
    private async void ListOfRooms()
    {
        try
        {
            // Get response from LobbyService to query all public lobbies
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();

            // Get the list of public rooms after have the list of Rooms
            VisualizeRoomList(queryResponse.Results);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private void VisualizeRoomList(List<Lobby> publicRooms)
    {
        foreach (Lobby room in publicRooms)
        {
            // Create a new room info UI element for each room...
            GameObject newRoomInfo = Instantiate(roomInfoPrefab, roomInfoContent.transform);
            // ...and set its parent to the room info content
            // Because the room info prefab is a child of the room info content, it will be displayed in the room info content
            var roomDetailsText = newRoomInfo.GetComponentsInChildren<TextMeshProUGUI>();
            // ...and set the room details text (name and player count)
            // first text is the room name
            // second text is the player count
            roomDetailsText[0].text = room.Name;
            roomDetailsText[1].text = (room.MaxPlayers - room.AvailableSlots).ToString() + " / " + room.MaxPlayers.ToString();

            // when the player clicks on the room info, it will join the room
            // We use the room ID to join the room
            newRoomInfo.GetComponentInChildren<Button>().onClick.RemoveAllListeners();
            newRoomInfo.GetComponentInChildren<Button>().onClick.AddListener(() => JoinRoom(room.Id));
        }
    }

    private void VisualizePlayerInRoom()
    {
        for (int i = 0; i < playerInfoContent.transform.childCount; i++)
        {
            // Destroy all the previous player info UI elements
            Destroy(playerInfoContent.transform.GetChild(i).gameObject);
        }

        // If the player is not in the room, we don't visualize the player info
        if (IsInRoom())
        {
            // If the player is in the room, we visualize the player info
            foreach (Player player in currentRoom.Players)
            {
                // Create a new player info UI element for each player...
                GameObject newPlayerInfo = Instantiate(playerInfoPrefab, playerInfoContent.transform);
                // ...and set its parent to the player info content
                // Because the player info prefab is a child of the player info content, it will be displayed in the player info content
                var playerDetailsText = newPlayerInfo.GetComponentInChildren<TextMeshProUGUI>();
                // ...and set the player details text (name)
                playerDetailsText.text = player.Data["Name"].Value;

                if (CheckIfHost() && player.Id != AuthenticationService.Instance.PlayerId)
                {
                    // If the player is the host, we add a kick button to the player info UI element
                    // This is to allow the host to kick other players from the room
                    // We need to include inactive children to find the button
                    // Because the button is not active until the host is in the room 
                    Button kickButton = newPlayerInfo.GetComponentInChildren<Button>(true);
                    kickButton.onClick.RemoveAllListeners(); // prevent click multiple time
                    kickButton.onClick.AddListener(() => KickPlayer(player.Id));
                    kickButton.gameObject.SetActive(true);
                    // And only can Start Game
                    startGameBtn.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            ExitRoom();
        }
    }
    #endregion


    #region Player Info

    private void PlayerName()
    {
        playerNameIF.onValueChanged.AddListener(delegate
        {
            PlayerPrefs.SetString("PlayerName", playerNameIF.text);
        });
        playerNameIF.text = PlayerPrefs.GetString("PlayerName");
    }

    private Player GetPlayerInfo()
    {
        string playerName = PlayerPrefs.GetString("PlayerName");
        if (playerName == "" && playerName == null)
        {
            playerName = "Player" + UnityEngine.Random.Range(1, 99).ToString();
            Debug.Log("Player name is empty, setting to default: " + playerName);
        }

        Player player = new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
            }
        };
        return player;
    }

    #endregion

    #region Other Controller

    private void CreateLobbyOptions()
    {
        createOptions = new CreateLobbyOptions
        {
            Player = GetPlayerInfo(),
            Data = new Dictionary<string, DataObject>
            {
                // A custom data to check Game is started yet
                // At first is not started so we set it false
                { "IsGameStarted", new DataObject(DataObject.VisibilityOptions.Member, "false")}
            }
        };
    }

    private void JoinLobbyByIDOptions()
    {
        joinOptions = new JoinLobbyByIdOptions
        {
            Player = GetPlayerInfo()
        };
    }

    // Only the host can send heartbeats to keep the lobby alive
    // If the client is sent heartbeats, it will cause an error (increased bandwidth cost)
    private async void HandleRoomHeartbeat()
    {
        if (currentRoom != null && CheckIfHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                heartbeatTimer = 15f; // Reset the timer
                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(currentRoom.Id);
                }
                catch (LobbyServiceException e)
                {
                    Debug.Log(e);
                }
            }
        }
    }

    // This method is used to update the room information periodically
    private async void HandleRoomUpdate()
    {
        if (currentRoom != null)
        {
            roomUpdateTimer -= Time.deltaTime;
            if (roomUpdateTimer <= 0f)
            {
                roomUpdateTimer = 2f;
                try
                {
                    // If the player is in the room, we update the room information
                    // This is to keep the room information up to date
                    // And prevent the host sending heartbeats while they are not in the room
                    if (IsInRoom())
                    {
                        currentRoom = await LobbyService.Instance.GetLobbyAsync(currentRoom.Id);
                        VisualizePlayerInRoom();
                    }
                }
                catch (LobbyServiceException e)
                {
                    Debug.Log(e);
                }
            }
        }
    }

    // Check if the player is already in the room
    // This is used to prevent the player from joining the room again if they are already in it
    private bool IsInRoom()
    {
        foreach (Player player in currentRoom.Players)
        {
            if (player.Id == AuthenticationService.Instance.PlayerId)
            {
                return true; // Player is already in the room
            }
        }

        return false; // Player is not in the room
    }

    public string GetNextHostId()
    {
        // If the current host leaves the room, we need to find the next host
        // We can do this by checking the player list and returning the first player that is not the current host
        foreach (Player player in currentRoom.Players)
        {
            if (player.Id != AuthenticationService.Instance.PlayerId)
            {
                return player.Id; // Return the next host ID
            }
        }
        return null; // No next host found
    }

    // Only the host can perform certain actions like sending heartbeats
    public bool CheckIfHost()
    {
        if (currentRoom != null && currentRoom.HostId == AuthenticationService.Instance.PlayerId)
        {
            return true;
        }
        return false;
    }
    #endregion
}
