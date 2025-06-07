using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Authentication;
using System.Collections.Generic;

public class GameLobbyManager : MonoBehaviour
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

    private CreateLobbyOptions createOptions;
    private JoinLobbyByIdOptions joinOptions;
    private Lobby currentRoom;
    private float heartbeatTimer = 15f;
    private float roomUpdateTimer = 2f;
    private string roomName;
    private int maxPlayers = 2; // max player alway 2

    #endregion

    #region Main Method
    void Start()
    {
        ButtonClickListener();
        PlayerName();
    }

    void Update()
    {
        HandleRoomHeartbeat();
        HandleRoomUpadate();
    }

    #endregion

    #region At Lobby Panel

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

    private async void JoinRoom(string roomID)
    {
        try
        {
            JoinLobbyByIDOptions();
            currentRoom = await LobbyService.Instance.JoinLobbyByIdAsync(roomID, joinOptions);
            EnterRoom();
            Debug.Log("Player in the room: " + currentRoom.Players.Count);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }

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

            // ưhen the player clicks on the room info, it will join the room
            // We use the room ID to join the room
            newRoomInfo.GetComponentInChildren<Button>().onClick.AddListener(() => JoinRoom(room.Id));
        }
    }

    private void VisualizePlayerInRoom()
    {
        for(int i = 0; i < playerInfoContent.transform.childCount; i++)
        {
            // Destroy all the previous player info UI elements
            Destroy(playerInfoContent.transform.GetChild(i).gameObject);
        }

        foreach (Player player in currentRoom.Players)
        {
            // Create a new player info UI element for each player...
            GameObject newPlayerInfo = Instantiate(playerInfoPrefab, playerInfoContent.transform);
            // ...and set its parent to the player info content
            // Because the player info prefab is a child of the player info content, it will be displayed in the player info content
            var playerDetailsText = newPlayerInfo.GetComponentInChildren<TextMeshProUGUI>();
            // ...and set the player details text (name)
            playerDetailsText.text = player.Data["Name"].Value;
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

    // Call when a button click
    // We could use Unity's UI Button component to call this method also
    private void ButtonClickListener()
    {
        createRoomBtn.onClick.AddListener(CreateRoom);
        refreshLobbiesBtn.onClick.AddListener(ListOfRooms);

    }

    private void CreateLobbyOptions()
    {
        createOptions = new CreateLobbyOptions
        {
            Player = GetPlayerInfo()
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
    // If the client is sent heartbeats, it will cause an error (increseased bandwidth cost)
    private async void HandleRoomHeartbeat()
    {
        if (currentRoom != null && IsHost())
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
    private async void HandleRoomUpadate()
    {
        if (currentRoom != null)
        {
            roomUpdateTimer -= Time.deltaTime;
            if (roomUpdateTimer <= 0f)
            {
                roomUpdateTimer = 2f;
                try
                {
                    currentRoom = await LobbyService.Instance.GetLobbyAsync(currentRoom.Id);
                    VisualizePlayerInRoom();
                }
                catch (LobbyServiceException e)
                {
                    Debug.Log(e);
                }
            }
        }
    }

    // Only the host can perform certain actions like sending heartbeats
    private bool IsHost()
    {
        if (currentRoom != null && currentRoom.HostId == AuthenticationService.Instance.PlayerId)
        {
            return true;
        }
        return false;
    }
    #endregion
}
