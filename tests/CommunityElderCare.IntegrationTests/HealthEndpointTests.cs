using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace CommunityElderCare.IntegrationTests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task Live_health_returns_stable_payload()
    {
        await using var app = new CommunityCareWebFactory();
        using var client = app.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"status\":\"live\"}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ready_health_checks_sqlite_and_returns_stable_payload()
    {
        await using var app = new CommunityCareWebFactory();
        using var client = app.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ready", payload.GetProperty("status").GetString());
        Assert.Equal(
            ["database", "backgroundJobs", "ai", "deviceGateway", "localNetwork"],
            payload.GetProperty("components")
                .EnumerateArray()
                .Select(component => component.GetProperty("name").GetString()!)
                .ToArray());
        Assert.All(
            payload.GetProperty("components").EnumerateArray(),
            component => Assert.Contains(
                component.GetProperty("status").GetString(),
                new[] { "ready", "degraded", "unavailable" }));
    }

    [Fact]
    public async Task Sqlite_can_write_temp_database()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            $"community-elder-care-write-{Guid.NewGuid():N}.db");

        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                Pooling = false,
            }.ToString();
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE PreflightCanary (Id INTEGER PRIMARY KEY, Value TEXT NOT NULL);
                INSERT INTO PreflightCanary (Value) VALUES ('ok');
                SELECT COUNT(*) FROM PreflightCanary;
                """;

            Assert.Equal(1L, await command.ExecuteScalarAsync());
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}
