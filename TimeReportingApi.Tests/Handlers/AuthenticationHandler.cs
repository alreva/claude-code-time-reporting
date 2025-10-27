using System.Net.Http.Headers;

namespace TimeReportingApi.Tests.Handlers;

public class AuthenticationHandler : DelegatingHandler
{
    private readonly string _token;

    public AuthenticationHandler(string token)
    {
        _token = token;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return await base.SendAsync(request, cancellationToken);
    }
}
