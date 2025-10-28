namespace TimeReportingApi.Middleware;

/// <summary>
/// Middleware to validate Bearer token authentication for API requests
/// </summary>
public class BearerAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public BearerAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip authentication for health check endpoint
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        // Allow GET requests to /graphql for Nitro IDE interface
        // Only POST requests (GraphQL operations) require authentication
        if (context.Request.Path.StartsWithSegments("/graphql") &&
            context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Get expected token from configuration
        var expectedToken = _configuration["Authentication:BearerToken"];
        if (string.IsNullOrEmpty(expectedToken))
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Bearer token not configured");
            return;
        }

        // Get Authorization header
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing Authorization header");
            return;
        }

        // Parse Bearer token
        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid Authorization header format. Expected: Bearer <token>");
            return;
        }

        var token = headerValue.Substring("Bearer ".Length).Trim();

        // Validate token
        if (string.IsNullOrEmpty(token) || token != expectedToken)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Invalid or missing bearer token");
            return;
        }

        // Token is valid, continue to next middleware
        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering BearerAuthMiddleware
/// </summary>
public static class BearerAuthMiddlewareExtensions
{
    public static IApplicationBuilder UseBearerAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<BearerAuthMiddleware>();
    }
}
