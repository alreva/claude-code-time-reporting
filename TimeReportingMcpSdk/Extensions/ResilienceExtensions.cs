using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using StrawberryShake;

namespace TimeReportingMcpSdk.Extensions;

/// <summary>
/// Extension methods for registering GraphQL resilience policies with Polly v8.
/// </summary>
public static class ResilienceExtensions
{
    /// <summary>
    /// Registers a resilience pipeline for GraphQL operations that automatically retries
    /// when AUTH_NOT_AUTHENTICATED errors are detected.
    /// </summary>
    public static IServiceCollection AddGraphQLResilience(this IServiceCollection services)
    {
        services.AddResiliencePipeline<string, IOperationResult>(
            "graphql-auth",
            builder =>
            {
                builder.AddRetry(new RetryStrategyOptions<IOperationResult>
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    ShouldHandle = args =>
                    {
                        var result = args.Outcome.Result;
                        var shouldHandle =
                            result?
                                .Errors?
                                .Any(err =>
                                    (err.Extensions?.TryGetValue("code", out var code) ?? false)
                                    && (code ?? "").ToString() == "AUTH_NOT_AUTHENTICATED")
                            ?? false;
                        return new ValueTask<bool>(shouldHandle);
                    }
                });
            });

        return services;
    }
}
