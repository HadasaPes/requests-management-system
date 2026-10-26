namespace RequestsBL.Exceptions;

/// <summary>Thrown when an operation conflicts with the current state (e.g. a concurrent update).</summary>
public class ConflictException : Exception
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
