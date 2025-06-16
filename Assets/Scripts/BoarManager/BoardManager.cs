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

    private Image btnImage;
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
        if (!IsOwner) return; // only local can interact with their button

        // 1 represents player space, 2 represents AI space
        // this parameter used to track easier which space is occupied by whom
        int currentPlayer = GameManager.Instance.currentTurnState == TurnState.XTurn ? 1 : 2;

        // Send player's request to the server
        SetPlayerSpaceServerRpc(row, column, currentPlayer);
    }


    [Rpc(SendTo.Server)]
    public void SetPlayerSpaceServerRpc(int row, int column, int currentPlayer)
    {
        // Do not override availble space
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

    [ClientRpc]
    private void SetPlayerSpaceClientRpc(int row, int column, int currentPlayer)
    {
        Sprite sprite = (currentPlayer == 1) ? XSprite : OSprite;

        foreach(Cell cell in FindObjectsOfType<Cell>())
        {
            if (cell.row == row && cell.column == column)
            {
                cell.SetSprite(sprite); // Set the sprite for the clicked cell
                break; // Exit loop once the correct cell is found
            }
        }
    }
    
}
