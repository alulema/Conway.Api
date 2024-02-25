namespace Conway.Api.Exceptions;

public class InvalidBoardStateException : Exception
{
    public InvalidBoardStateException() { }

    public InvalidBoardStateException(string message) 
        : base(message) { }

    public InvalidBoardStateException(string message, Exception inner) 
        : base(message, inner) { }
}
