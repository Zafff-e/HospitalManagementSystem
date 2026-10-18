namespace Hms.Domain.Exceptions;

/// <summary>
/// Exception thrown when a resource conflict occurs (e.g. concurrent slot booking).
/// Mapped to HTTP 409 Conflict.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
    public ConflictException(string message, Exception innerException) : base(message, innerException) { }
}
