using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class BoardManager : SingletonNetwork<BoardManager>
{
    #region Parameters
    [Header("Board Settings")]
    [Tooltip("The size of the board, e.g., 3 for a 3x3 grid.")]
    public int boardSize = 3; // We can change this to support larger boards in the future.
    public int[,] boardSpaces; // 2D array to represent the board spaces.

    [Space(10)]
    [Header("UI Elements")]
    [SerializeField] private Sprite XSprite;
    [SerializeField] private Sprite OSprite;

    #endregion

    #region Main Methods
    void Start()
    {
        // Initialize the board spaces array based on the board size
        boardSpaces = new int[boardSize, boardSize];
    }

    #endregion

    #region Placing X and O 
    /// <summary>
    /// When player clicks on a button
    /// </summary>
    /// <remarks>
    /// this method is called to set the player's space.
    /// If it's the X turn, it sets the X sprite,
    /// otherwise it sets the X sprite.
    /// Disables the button to prevent multiple clicks.
    /// </remarks>
    /// <param name="placingButton">Clicked Button to placing in Cell</param>
    /// <param name="row">Row's position of button</param>
    /// <param name="column">Column's position of button</param>
    public void SetPlayerSpace(int row, int column)
    {

        // 1 represents X player space, 2 represents O space
        // this parameter used to track easier which space is occupied by who
        int currentPlayer = GameManager.Instance.currentTurnState == TurnState.XTurn ? 1 : 2;

        // Send player's request to the server
        SetPlayerSpaceServerRpc(row, column, currentPlayer);
    }

    /// <summary>
    /// Sets the player's space on the server.
    /// </summary>
    /// <remarks>
    /// This method is called when a player clicks on a cell.
    /// It checks if the cell is already occupied. If not, it updates the board state
    /// One Note that this method don't overload Transform, Button, etc. 
    /// </remarks>
    /// <param name="row">Row's position of button</param>
    /// <param name="column">Column's position of button</param>
    /// <param name="currentPlayer">The player which is playing</param> 
    /// <summary>
    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerSpaceServerRpc(int row, int column, int currentPlayer)
    {
        // Do not override available space
        if (boardSpaces[row, column] != 0)
        {
            Debug.Log("This cell is already click");
            return;
        }

        boardSpaces[row, column] = currentPlayer; // Update the board state on the server

        SetPlayerSpaceClientRpc(row, column, currentPlayer);

        GameManager.Instance.ProcessTurn(row, column, currentPlayer);
    }
    #endregion

    /// <summary>
    /// Sets the player's space on all clients.
    /// <remarks>
    /// This method is called by the server to update all clients
    /// It find the cell at the given row and column...
    /// ...Then sets its sprite to the current player (X or O)
    /// This ensure that all clients see the same move on their game boards
    /// </remarks>
    /// <param name="row">Row's position of button</param>
    /// <param name="column">Column's position of button</param>
    /// <param name="currentPlayer">The player which is playing</param> 
    /// </summary>
    [ClientRpc]
    private void SetPlayerSpaceClientRpc(int row, int column, int currentPlayer)
    {
        Debug.Log($"[ClientRpc] SetPlayerSpaceClientRpc: {row}, {column}, player: {currentPlayer}");
        Sprite sprite = (currentPlayer == 1) ? XSprite : OSprite;

        foreach (Cell cell in FindObjectsOfType<Cell>())
        {
            if (cell.row == row && cell.column == column)
            {
                cell.SetSprite(sprite); // Set the sprite for the clicked cell
                break; // Exit loop once the correct cell is found
            }
        }
    }
    
}
