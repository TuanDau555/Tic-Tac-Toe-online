using System;
using Unity.Netcode;
using UnityEngine;


public enum TurnState
{
    XTurn,
    OTurn,
    GameOver
}

public class GameManager : SingletonNetwork<GameManager>
{
    public TurnState currentTurnState;

    // // Ex: 3x3 has 9 turn, if 9 turn have been played and no one win so it Draw 
    private int turnCount;  
    private string resultText;


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
        // Every turn need to increase the turnCount;
        turnCount++;

        // Win
        if (CheckForWinner(row, column, player))
        {
            AnnounceWinnerClientRpc(player);
            UpdateTurnStateClientRpc(TurnState.GameOver);
        }

        // Draw
        // board size = 3 --> 3x3 = 9 
        else if (turnCount >= BoardManager.Instance.boardSize * BoardManager.Instance.boardSize)
        {
            AnnounceDrawClientRpc();
            UpdateTurnStateClientRpc(TurnState.GameOver);
        }

        // Lose
        else
        {
            Debug.Log("No winner Found game continues");
            TurnState nextTurn = (player == 1) ? TurnState.OTurn : TurnState.XTurn;
            currentTurnState = nextTurn;
            UpdateTurnStateClientRpc(nextTurn);
        }
    }


    /// <summary>
    /// Update state to all client
    /// </summary>
    /// <remarks>
    /// When we change state is just update locally
    /// So we need to update to all client also
    /// </remarks>
    /// <param name="newState">Stored state that has changed</param>
    [ClientRpc]
    private void UpdateTurnStateClientRpc(TurnState newState)
    {
        currentTurnState = newState;
    }

    [ClientRpc]
    private void AnnounceWinnerClientRpc(int winnerPlayerId)
    {
        Debug.Log("Is Found winner");
        // Get player Id of current client
        // LocalClientId alway set to 0 for Host
        // we set it to 1 or 2 for easily checking
        int localPlayerId = NetworkManager.Singleton.LocalClientId == 0 ? 1 : 2;

        if (localPlayerId == winnerPlayerId)
        {
            resultText = "You Win!";
            GameOverUI.Instance.ShowGameOver(resultText, Color.green);
        }
        else
        {
            resultText = "You Lose!";
            GameOverUI.Instance.ShowGameOver(resultText, Color.red);
        }
    }

    [ClientRpc]
    private void AnnounceDrawClientRpc()
    {
        resultText = "Tie";
        GameOverUI.Instance.ShowGameOver(resultText, Color.yellow);
    }

    /// <summary>
    /// This is call when player click rematch button
    /// </summary>
    public void RequestRematch()
    {
        // if that is host so easily rematch and send logic to client
        if (IsServer)
        {
            StartRematch();
        }
        // if not client need to send request and host will handle it
        else
        {
            RequestRematchServerRpc();
        }
    }

    /// <summary>
    /// Client send request to server(host) to handle the logic
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void RequestRematchServerRpc()
    {
        StartRematch(); // host will handle and send back to client
    }

    private void StartRematch()
    {
        
        // Reset the logic
        for (int row = 0; row < BoardManager.Instance.boardSize; row++)
        {
            for (int col = 0; col < BoardManager.Instance.boardSize; col++)
            {
                // Reset the board state
                // 0 means empty cell not occupied or belonging to any player
                BoardManager.Instance.boardSpaces[row, col] = 0;
            }
        }

        // After we reset the logic we reset the UI
        // And send it to client also 
        ResetBoardClientRpc();

         // X turn again
        currentTurnState = TurnState.XTurn;
        UpdateTurnStateClientRpc(TurnState.XTurn);
        turnCount = 0; // remember to reset the turn count 
        
        // Remember to Hide GameOver UI
        GameOverUI.Instance.Hide();
    }

    [ClientRpc]
    private void ResetBoardClientRpc()
    {
        foreach (var cell in FindObjectsOfType<Cell>())
        {
            cell.ResetCell();
        }
        

        GameOverUI.Instance.Hide();
    }

    public bool IsMyTurn()
    {
        // if is host only host play
        // else only client play 
        return (currentTurnState == TurnState.XTurn && IsServer) ||
               (currentTurnState == TurnState.OTurn && !IsServer);
    }
    #endregion

    #region Win Condition

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
    ///  Count the consecutive cells in specific direction form the position (row, column)
    /// </summary>
    /// <param name="row">Start Row</param>
    /// <param name="column">Start Column</param>
    /// <param name="directionRow">Step Belonging to Row (eg 1,-1,0)</param>
    /// <param name="directionColumn">Step Belonging to Column (eg 1,-1,0)</param>
    /// <param name="player">1 for X and 2 for O</param>
    /// <returns>Consecutive cells that found 
    /// <returns>                                                                                                                                                                 
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

            // Check if the cells at (newRow, newColumn) is belonging to Player 1 or 2
            // Eg: (1,2) currentPlayer == 1 -> count = 1 for Player 1 at cell (1,2) 
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
