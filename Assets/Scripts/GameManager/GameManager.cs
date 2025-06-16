using UnityEngine;


public enum TurnState
{
    XTurn,
    OTurn,
    GameOver
}

public class GameManager : Singleton<GameManager>
{
    public TurnState currentTurnState;

    #region Main Methods
    void Start()
    {
        currentTurnState = TurnState.XTurn;
    }
    #endregion

    #region Game Flow

    /// <summary>
    /// Checking which player Turn
    /// </summary>
    /// <param name="row">Row of current cell just click (0-based index)</param>
    /// <param name="column">Column of current cell just click (0-based index)</param>
    /// <param name="player">1 for X and 2 for O</param>
    public void ProcessTurn(int row, int column, int player)
    {
        if (CheckForWinner(row, column, player))
        {
            Debug.Log((player == 1 ? "Player X" : "Player 0") + " win!");
        }

        else
        {
            Debug.Log("No winner Found game continues");
            GameManager.Instance.currentTurnState =
                (player == 1) ? TurnState.OTurn : TurnState.XTurn;
        }
    }
    #endregion

    #region Win Condtion

    /// <summary>
    /// Logic to check if there is a winner.
    /// </summary>
    /// <remarks>
    /// This method will check all rows, columns, and diagonals
    /// to determine if a player has won the game.
    /// </remarks>
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

            if (count >= 3 && BoardManager.Instance.boardSize == 3) return true; // 3x3 win condition

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
        for (int i = 1; i < BoardManager.Instance.boardSize; i++)
        {
            int newRow = row + directionRow * i; // Calculate the new row index
            int newColumn = column + directionColumn * i; // Calculate the new column index 
            
            if (newRow < 0 || newRow >= BoardManager.Instance.boardSize || newColumn < 0 || newColumn >= BoardManager.Instance.boardSize)
                break; // Out of bounds check

            // Check if the cells at (newRow, newColumn) is beloning to Player 1 or 2
            // Eg: (!,2) currentPlayer == 1 -> count = 1 for Player 1 at cell (1,2) 
            if (BoardManager.Instance.boardSpaces[newRow, newColumn] == player)
                count++;
            else
                break;
        }

        // return Found
        return count;
    }
    #endregion
}
