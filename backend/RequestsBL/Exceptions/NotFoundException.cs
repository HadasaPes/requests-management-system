namespace RequestsBL.Exceptions;

/// <summary>Thrown when a requested item does not exist.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }
}
