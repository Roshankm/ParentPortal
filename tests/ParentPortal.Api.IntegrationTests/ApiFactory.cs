using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

using ParentPortal.Contracts;

namespace ParentPortal.Api.IntegrationTests;

/// <summary>Boots the real API against a throw-away SQLite database seeded with the demo schools.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"parentportal-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ParentPortal", $"Data Source={_databasePath}");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Reminders:Enabled", "false");
        builder.UseSetting("AzureAd:ClientId", string.Empty);
    }

    public HttpClient ClientFor(string demoUserId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(DemoAuth.Header, demoUserId);
        return client;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }
}
