using Conway.Api.Exceptions;

namespace Conway.Api.Utils;

public static class BoardValidator
{
    public static void ValidateBoardState(bool[,] boardState)
    {
        if (boardState == null)
        {
            throw new InvalidBoardStateException("The board state cannot be null.");
        }

        // Validate board size
        int rows = boardState.GetLength(0);
        int cols = boardState.GetLength(1);

        if (rows == 0 || cols == 0 || rows > 100 || cols > 100) // Example limits
        {
            throw new InvalidBoardStateException("The board size is invalid. Board must be between 1x1 and 100x100.");
        }
        
        // Validate minimum or maximum population
        int liveCells = 0;
        for (int i = 0; i < boardState.GetLength(0); i++)
        {
            for (int j = 0; j < boardState.GetLength(1); j++)
            {
                if (boardState[i, j]) liveCells++;
            }
        }

        if (liveCells < 1) // Example minimum
        {
            throw new InvalidBoardStateException("The board must have at least one live cell.");
        }

        if (liveCells > 1000) // Example maximum
        {
            throw new InvalidBoardStateException("The board cannot have more than 1000 live cells.");
        }
    }
}