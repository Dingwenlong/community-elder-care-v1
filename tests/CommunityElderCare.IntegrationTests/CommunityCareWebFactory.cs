using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using CommunityElderCare.Core.Identity;

namespace CommunityElderCare.IntegrationTests;

public sealed class CommunityCareWebFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"community-elder-care-{Guid.NewGuid():N}.db");

    public CommunityCareWebFactory()
    {
        using var databaseFile = File.Create(_databasePath);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CommunityCare"] = $"Data Source={_databasePath};Pooling=False",
            }));
    }

    public HttpClient CreateAuthenticatedClient(
        DemoRole role,
        string? areaCode = null,
        Guid? familyFor = null,
        Guid? elderId = null,
        Guid? assignedTaskId = null)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Demo-Role", role.ToString());
        if (!string.IsNullOrWhiteSpace(areaCode))
        {
            client.DefaultRequestHeaders.Add("X-Demo-Area-Code", areaCode);
        }
        if (familyFor.HasValue)
        {
            client.DefaultRequestHeaders.Add("X-Demo-Family-For", familyFor.Value.ToString());
        }
        if (elderId.HasValue)
        {
            client.DefaultRequestHeaders.Add("X-Demo-Elder-Id", elderId.Value.ToString());
        }
        if (assignedTaskId.HasValue)
        {
            client.DefaultRequestHeaders.Add("X-Demo-Assigned-Task", assignedTaskId.Value.ToString());
        }

        return client;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        DeleteDatabaseFiles();
        GC.SuppressFinalize(this);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            DeleteDatabaseFiles();
        }
    }

    private void DeleteDatabaseFiles()
    {
        foreach (var path in new[] { _databasePath, $"{_databasePath}-shm", $"{_databasePath}-wal" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
