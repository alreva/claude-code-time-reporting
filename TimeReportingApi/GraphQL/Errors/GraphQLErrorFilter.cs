using TimeReportingApi.Exceptions;

namespace TimeReportingApi.GraphQL.Errors;

/// <summary>
/// HotChocolate error filter that converts custom exceptions to GraphQL errors with appropriate error codes.
/// Implements the error handling strategy from ADR 0008.
/// </summary>
public class GraphQLErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        // Handle ValidationException
        if (error.Exception is Exceptions.ValidationException validationEx)
        {
            var builder = ErrorBuilder.New()
                .SetMessage(validationEx.Message)
                .SetCode("VALIDATION_ERROR")
                .SetPath(error.Path);

            if (!string.IsNullOrEmpty(validationEx.Field))
            {
                builder.SetExtension("field", validationEx.Field);
            }

            if (error.Locations != null)
            {
                foreach (var location in error.Locations)
                {
                    builder.AddLocation(location);
                }
            }

            return builder.Build();
        }

        // Handle BusinessRuleException
        if (error.Exception is BusinessRuleException businessEx)
        {
            var builder = ErrorBuilder.New()
                .SetMessage(businessEx.Message)
                .SetCode("BUSINESS_RULE_VIOLATION")
                .SetPath(error.Path);

            if (error.Locations != null)
            {
                foreach (var location in error.Locations)
                {
                    builder.AddLocation(location);
                }
            }

            return builder.Build();
        }

        // Handle generic exceptions (don't expose internal details)
        if (error.Exception != null)
        {
            var builder = ErrorBuilder.New()
                .SetMessage("An unexpected error occurred")
                .SetCode("INTERNAL_ERROR")
                .SetPath(error.Path);

            if (error.Locations != null)
            {
                foreach (var location in error.Locations)
                {
                    builder.AddLocation(location);
                }
            }

            return builder.Build();
        }

        // Return error as-is if no custom handling needed
        return error;
    }
}
