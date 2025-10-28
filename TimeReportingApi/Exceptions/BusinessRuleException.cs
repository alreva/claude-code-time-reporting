namespace TimeReportingApi.Exceptions;

/// <summary>
/// Exception thrown when business rule validation fails.
/// Maps to GraphQL BUSINESS_RULE_VIOLATION code.
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }

    public BusinessRuleException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
