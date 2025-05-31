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
    [SerializeField] private Button refreshLobbiesBtn;
    [SerializeField] private GameObject roomInfoPrefab;
    [SerializeField] private GameObject roomInfoContent;

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


    private Lobby currentRoom;
    private float heartbeatTimer = 15f;
    #endregion

    #region Main Method
    void Start()
    {
        ButtonClickListener();
    }

    void Update()
    {
        HandleRoomHeartbeat();
    }

    #endregion

    #region At Lobby Panel

    private async void CreateRoom()
    {
        try
        {
            string lobbyName = roomNameIF.text;
            int maxPlayers = 2; // max player alway 2

            // Check if the lobby name is empty

            // Check if the lobby name is current exist

            // Create a lobby with the specified name and max players
            // after the lobby is created, the host will enter the room
            currentRoom = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);
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
    }

    private async void JoinRoom(string roomID)
    {
        try
        {
            currentRoom = await LobbyService.Instance.JoinLobbyByCodeAsync(roomID);
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
            roomDetailsText[0].text = room.Name;
            roomDetailsText[1].text = (room.MaxPlayers - room.AvailableSlots).ToString() + " / " + room.MaxPlayers.ToString();

            // ưhen the player clicks on the room info, it will join the room
            // We use the room ID to join the room
            newRoomInfo.GetComponentInChildren<Button>().onClick.AddListener(() => JoinRoom(room.Id));
        }
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
