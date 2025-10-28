namespace TimeReportingApi.Exceptions;

/// <summary>
/// Exception thrown when input validation fails.
/// Maps to GraphQL VALIDATION_ERROR code.
/// </summary>
public class ValidationException : Exception
{
    public string? Field { get; }

    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, string field) : base(message)
    {
        Field = field;
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
