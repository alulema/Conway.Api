using Conway.Api.DataAccess;
using Conway.Api.Exceptions;
using Conway.Api.Models;
using Conway.Api.Services.Interfaces;
using Conway.Api.Utils;
using Newtonsoft.Json;

namespace Conway.Api.Services;

public class GameOfLifeService : IGameOfLifeService
{
    private readonly GameOfLifeContext _context;
    private readonly ILogger<GameOfLifeService> _logger;

    public GameOfLifeService(GameOfLifeContext context, ILogger<GameOfLifeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Guid> UploadNewBoardAsync(bool[,] initialState)
    {
        _logger.LogInformation("Uploading a new board");

        try
        {
            BoardValidator.ValidateBoardState(initialState);
        }
        catch (InvalidBoardStateException ex)
        {
            _logger.LogWarning(ex, "Invalid board state during upload");
            throw;
        }

        var board = new GameBoard { Id = Guid.NewGuid(), State = initialState };
        await _context.Boards.AddAsync(board);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Board {BoardId} uploaded successfully", board.Id);

        return board.Id;
    }
    
    public async Task<bool[,]> GetNextStateAsync(Guid boardId)
    {
        _logger.LogInformation("Fetching next state for board {BoardId}.", boardId);
        
        var board = await _context.Boards.FindAsync(boardId);
        
        if (board is null)
        {
            _logger.LogWarning("Board {BoardId} not found.", boardId);
            throw new BoardNotFoundException(boardId);
        }

        var nextState = await CalculateNextStateAsync(board.State);
        board.State = nextState;
        board.Generation++;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Next state for board {BoardId} fetched successfully.", boardId);

        return nextState;
    }
    
    public async Task<bool[,]> GetStateAfterXGenerationsAsync(Guid boardId, int generations)
    {
        _logger.LogInformation("Calculating state after {Generations} generations for board {BoardId}.", generations, boardId);

        var currentState = await GetBoardStateByIdAsync(boardId);
        for (int i = 0; i < generations; i++)
        {
            currentState = await CalculateNextStateAsync(currentState);
        }

        _logger.LogInformation("State after {Generations} generations for board {BoardId} calculated successfully.", generations, boardId);

        return currentState;
    }
    
    public async Task<bool[,]> GetFinalStateAsync(Guid boardId, int maxAttempts)
    {
        _logger.LogInformation("Attempting to find final state for board {BoardId} with a maximum of {MaxAttempts} attempts.", boardId, maxAttempts);

        var currentState = await GetBoardStateByIdAsync(boardId);
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var nextState = await CalculateNextStateAsync(currentState);
            if (CheckForStableState(currentState, nextState))
            {
                await UpdateBoardStateAsync(boardId, nextState);
                _logger.LogInformation("Final state for board {BoardId} found successfully after {Attempt} attempts.", boardId, attempt + 1);
                return nextState;
            }
            currentState = nextState;
        }

        _logger.LogWarning("Unable to find a final state for board {BoardId} after {MaxAttempts} attempts.", boardId, maxAttempts);
        throw new Exception($"Unable to find a final state for board {boardId} after {maxAttempts} attempts.");
    }
    
    private async Task<bool[,]> CalculateNextStateAsync(bool[,] currentState)
    {
        _logger.LogDebug("Calculating the next state for the board.");
        var activeCells = GetActiveCells(currentState);
        bool[,] nextState = (bool[,])currentState.Clone();

        foreach (var (x, y) in activeCells)
        {
            int liveNeighbors = CountLiveNeighbors(currentState, x, y);
            bool isAlive = currentState[x, y];

            bool shouldLive = isAlive ? liveNeighbors == 2 || liveNeighbors == 3 : liveNeighbors == 3;
            nextState[x, y] = shouldLive;
        }

        _logger.LogDebug("Next state calculation completed.");
        return nextState;
    }

    private HashSet<(int x, int y)> GetActiveCells(bool[,] currentState)
    {
        _logger.LogDebug("Identifying active cells in the current board state.");
        int width = currentState.GetLength(0);
        int height = currentState.GetLength(1);
        var activeCells = new HashSet<(int, int)>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (currentState[x, y])
                {
                    // Add the cell and its neighbors to the active set
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int neighborX = x + i;
                            int neighborY = y + j;
                            // Check bounds
                            if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                            {
                                activeCells.Add((neighborX, neighborY));
                            }
                        }
                    }
                }
            }
        }

        _logger.LogDebug("Active cells identified.");
        return activeCells;
    }

    private int CountLiveNeighbors(bool[,] currentState, int x, int y)
    {
        _logger.LogTrace("Counting live neighbors for cell at ({X},{Y}).", x, y);
        int width = currentState.GetLength(0);
        int height = currentState.GetLength(1);
        int liveNeighbors = 0;

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                // Skip the cell itself
                if (i == 0 && j == 0) continue;

                int neighborX = x + i;
                int neighborY = y + j;

                // Check bounds before accessing the array to avoid IndexOutOfRangeException
                if (neighborX >= 0 && neighborX < width && neighborY >= 0 && neighborY < height)
                {
                    liveNeighbors += currentState[neighborX, neighborY] ? 1 : 0;
                }
            }
        }

        _logger.LogTrace("Cell at ({X},{Y}) has {LiveNeighbors} live neighbors.", x, y, liveNeighbors);
        return liveNeighbors;
    }
    
    private bool CheckForStableState(bool[,] currentState, bool[,] nextState)
    {
        _logger.LogDebug("Checking for a stable state between the current and next state of the board.");
        int width = currentState.GetLength(0);
        int height = currentState.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (currentState[x, y] != nextState[x, y])
                {
                    _logger.LogDebug("Board is not in a stable state.");
                    return false; // A change has been detected
                }
            }
        }

        _logger.LogInformation("Board has reached a stable state.");
        return true; // No changes detected, indicating a stable state
    }
    
    private async Task<bool[,]> GetBoardStateByIdAsync(Guid boardId)
    {
        _logger.LogInformation("Retrieving board state for Board ID: {BoardId}", boardId);
    
        var board = await _context.Boards.FindAsync(boardId);
    
        if (board == null)
        {
            _logger.LogWarning("Board not found for Board ID: {BoardId}", boardId);
            throw new BoardNotFoundException(boardId);
        }

        _logger.LogDebug("Board state retrieved successfully for Board ID: {BoardId}", board.Id);
        return board.State;
    }
    
    private async Task UpdateBoardStateAsync(Guid boardId, bool[,] newState)
    {
        _logger.LogInformation("Updating board state for Board ID: {BoardId}", boardId);
    
        var board = await _context.Boards.FindAsync(boardId);
        if (board == null)
        {
            _logger.LogWarning("Attempted to update a non-existing board. Board ID: {BoardId}", boardId);
            throw new BoardNotFoundException(boardId);
        }

        board.State = newState;
        board.Generation++;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Board state updated successfully for Board ID: {BoardId}. New generation: {Generation}", board.Id, board.Generation);
    }
}