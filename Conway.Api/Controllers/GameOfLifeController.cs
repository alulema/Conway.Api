using Conway.Api.Exceptions;
using Conway.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Conway.Api.Controllers;

/// <summary>
/// The board will be limited to 100x100
/// </summary>
[ApiController]
[Route("[controller]")]
public class GameOfLifeController : ControllerBase
{
    private readonly IGameOfLifeService _gameOfLifeService;
    private readonly ILogger<GameOfLifeController> _logger;

    public GameOfLifeController(IGameOfLifeService gameOfLifeService, ILogger<GameOfLifeController> logger)
    {
        _gameOfLifeService = gameOfLifeService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> UploadBoard([FromBody] bool[,] board)
    {
        _logger.LogInformation("Received request to upload new board.");
        try
        {
            var boardId = await _gameOfLifeService.UploadNewBoardAsync(board);
            _logger.LogInformation("Board uploaded successfully with ID: {BoardId}.", boardId);
            return Ok(boardId);
        }
        catch (InvalidBoardStateException ex)
        {
            _logger.LogWarning(ex, "Failed to upload board due to invalid state.");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while uploading new board.");
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    [HttpGet("next/{boardId}")]
    public async Task<IActionResult> GetNextState(Guid boardId)
    {
        _logger.LogInformation("Received request for next state of board ID: {BoardId}.", boardId);
        try
        {
            var nextState = await _gameOfLifeService.GetNextStateAsync(boardId);
            return Ok(nextState);
        }
        catch (BoardNotFoundException ex)
        {
            _logger.LogWarning(ex, "Board with ID: {BoardId} not found.", boardId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching next state for board ID: {BoardId}.", boardId);
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    [HttpGet("{boardId}/generations/{generations}")]
    public async Task<IActionResult> GetStateAfterXGenerations(Guid boardId, int generations)
    {
        _logger.LogInformation("Received request for state after {Generations} generations for board ID: {BoardId}.", generations, boardId);
        try
        {
            var state = await _gameOfLifeService.GetStateAfterXGenerationsAsync(boardId, generations);
            return Ok(state);
        }
        catch (BoardNotFoundException ex)
        {
            _logger.LogWarning(ex, "Board with ID: {BoardId} not found.", boardId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while calculating state after {Generations} generations for board ID: {BoardId}.", generations, boardId);
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }

    [HttpGet("{boardId}/final/{maxAttempts}")]
    public async Task<IActionResult> GetFinalState(Guid boardId, int maxAttempts)
    {
        _logger.LogInformation("Received request for final state of board ID: {BoardId} with max attempts: {MaxAttempts}.", boardId, maxAttempts);
        try
        {
            var finalState = await _gameOfLifeService.GetFinalStateAsync(boardId, maxAttempts);
            return Ok(finalState);
        }
        catch (BoardNotFoundException ex)
        {
            _logger.LogWarning(ex, "Board with ID: {BoardId} not found.", boardId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching final state for board ID: {BoardId} with max attempts: {MaxAttempts}.", boardId, maxAttempts);
            return StatusCode(500, "An error occurred while processing your request.");
        }
    }
}
