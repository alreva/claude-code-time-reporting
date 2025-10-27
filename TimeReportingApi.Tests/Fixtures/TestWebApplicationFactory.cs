using Microsoft.AspNetCore.Hosting;

namespace TimeReportingApi.Tests.Fixtures;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public string BearerToken { get; set; } = "test-bearer-token-12345";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Authentication:BearerToken", BearerToken);

        base.ConfigureWebHost(builder);
    }
}
