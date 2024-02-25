namespace Conway.Api.Exceptions;

public class BoardNotFoundException : Exception
{
    public Guid BoardId { get; }

    public BoardNotFoundException(Guid boardId)
        : base($"Board with ID {boardId} not found.")
    {
        BoardId = boardId;
    }

    public BoardNotFoundException(Guid boardId, Exception innerException)
        : base($"Board with ID {boardId} not found.", innerException)
    {
        BoardId = boardId;
    }
}
