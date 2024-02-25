namespace Conway.Api.Services.Interfaces;

public interface IGameOfLifeService
{
    Task<Guid> UploadNewBoardAsync(bool[,] initialState);
    Task<bool[,]> GetNextStateAsync(Guid boardId);
    Task<bool[,]> GetStateAfterXGenerationsAsync(Guid boardId, int generations);
    Task<bool[,]> GetFinalStateAsync(Guid boardId, int maxAttempts);
}