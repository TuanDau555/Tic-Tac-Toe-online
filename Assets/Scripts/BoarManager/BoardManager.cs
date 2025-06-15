using UnityEngine;
using UnityEngine.UI;

public class BoardManager : Singleton<BoardManager>
{
    #region Parameters
    [Header("Board Settings")]
    [Tooltip("The size of the board, e.g., 3 for a 3x3 grid.")]
    [SerializeField] private int boardSize = 3; // We can change this to support larger boards in the future.
    [SerializeField] private int[,] boardSpaces; // 2D array to represent the board spaces.

    [Space(10)]
    [Header("UI Elements")]
    [SerializeField] private Sprite playerSpaceSprite;
    [SerializeField] private Sprite AISpaceSprite;

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
    /// /// When player clicks on a button
    /// this method is called to set the player's space.
    /// If it's the player's turn, it sets the player's sprite,
    /// otherwise it sets the AI's sprite.
    /// Disables the button to prevent multiple clicks.
    /// </summary>
    /// <param name="placingButton">Clicked Button to placing in Cell</param>
    /// <param name="row">Row's position of button</param>
    /// <param name="column">Column's position of button</param>
    public void SetPlayerSpace(Button placingButton, int row, int column)
    {
        // 1 represents player space, 2 represents AI space
        // this parameter used to track easier which space is occupied by whom
        int currentPlayer = GameManager.Instance.currentTurnState == TurnState.XTurn ? 1 : 2;
        Sprite currentSprite = (currentPlayer == 1) ? playerSpaceSprite : AISpaceSprite;

        btnImage = placingButton.image;
        placingButton.interactable = false;
        btnImage.sprite = currentSprite;
        btnImage.enabled = true;

        boardSpaces[row, column] = currentPlayer; // Update the board state

        if (CheckForWinner(row, column, currentPlayer))
        {
            Debug.Log((currentPlayer == 1 ? "Player X" : "Player 0") + " win!");
        }

        else
        {
            Debug.Log("No winner Found game continues");
            GameManager.Instance.currentTurnState =
                (currentPlayer == 1) ? TurnState.OTurn : TurnState.XTurn;
        }
    }

    private void SetAISpace()
    {

    }
    #endregion

    #region Win Condtion

    /// <summary>
    /// Logic to check if there is a winner.
    /// This method will check all rows, columns, and diagonals
    /// to determine if a player has won the game.
    /// </summary>
    /// <param name="row">Row of current cell just click (0-based index)</param>
    /// <param name="column">Column of current cell just click (0-based index)</param>
    /// <param name="player">1 for X and 2 for O</param>
    /// <returns>True if Someone Win, else None win</returns>
    private bool CheckForWinner(int row, int column, int player)
    {
        Debug.Log("Is Checking Winner");


        int[][] directions = new int[][]
        {
            new int[] {0, 1}, // Vertical
            new int[] {1, 0}, // Horizontal
            new int[] {1, 1}, // Main Diagonal \
            new int[] {1, -1} // Anti Diagonal /
        };

        // Check each direction
        foreach (var direction in directions)
        {
            int count = 1;
            
            // Count the number of consecutive cells in the positive direction
            count += CountDirection(row, column, direction[0], direction[1], player);
            // Count the the number of consecutive cells in the positive direction
            count += CountDirection(row, column, -direction[0], -direction[1], player);

            if (count >= 3 && boardSize == 3) return true; // 3x3 win condition

        }


        // if not winner found, game continues
        return false;
    }


    /// <summary>
    ///  Count the consecutive cells in specific direction form the postion (row, column)
    /// </summary>
    /// <param name="row">Start Row</param>
    /// <param name="column">Start Column</param>
    /// <param name="directionRow">Step Beloning to Row (eg 1,-1,0)</param>
    /// <param name="directionColumn">Step Beloning to Column (eg 1,-1,0)</param>
    /// <param name="player">1 for X and 2 for O</param>
    /// <returns>Consecutive cells that found</returns>
    private int CountDirection(int row, int column, int directionRow, int directionColumn, int player)
    {
        Debug.Log("Is Counting direction");

        int count = 0;
        for (int i = 1; i < boardSize; i++)
        {
            int newRow = row + directionRow * i; // Calculate the new row index
            int newColumn = column + directionColumn * i; // Calculate the new column index 
            
            if (newRow < 0 || newRow >= boardSize || newColumn < 0 || newColumn >= boardSize)
                break; // Out of bounds check

            // Check if the cells at (newRow, newColumn) is beloning to Player 1 or 2
            // Eg: (!,2) currentPlayer == 1 -> count = 1 for Player 1 at cell (1,2) 
            if (boardSpaces[newRow, newColumn] == player)
                count++;
            else
                break;
        }

        // return Found
        return count;
    }
    #endregion
}
