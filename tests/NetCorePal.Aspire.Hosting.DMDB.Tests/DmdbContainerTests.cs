using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using System.Net.Sockets;

namespace NetCorePal.Aspire.Hosting.DMDB.Tests;

/// <summary>
/// Integration tests that verify DMDB containers start correctly and accept connections.
/// These tests follow the pattern from https://github.com/netcorepal/netcorepal-testcontainers
/// 
/// Note: These tests verify TCP connectivity. For full SQL integration tests, a DMDB .NET client library would be required.
/// 
/// To run these tests:
/// 1. Ensure Docker is installed and running
/// 2. Run: dotnet test --filter "FullyQualifiedName~DmdbContainerTests"
/// </summary>
public class DmdbContainerTests : IClassFixture<DmdbFixture>
{
    private readonly DmdbFixture _fixture;

    public DmdbContainerTests(DmdbFixture fixture)
    {
        _fixture = fixture;
    }

    [RequiresDockerFact]
    public async Task CanConnectToDmdbServerViaTcp()
    {
        // Arrange
        var connectionInfo = await _fixture.GetConnectionInfoAsync();
        
        // Act - Try to establish TCP connection
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(connectionInfo.Host, connectionInfo.Port);

        // Assert
        Assert.True(tcpClient.Connected, "Should be able to connect to DMDB server via TCP");
    }

    [RequiresDockerFact]
    public async Task ConnectionStringContainsRequiredComponents()
    {
        // Arrange
        var connectionString = await _fixture.GetConnectionStringAsync();

        // Assert
        Assert.NotNull(connectionString);
        Assert.Contains("Host=", connectionString);
        Assert.Contains("Username=", connectionString);
        Assert.Contains("Password=", connectionString);
    }

    [RequiresDockerFact]
    public async Task DatabaseResourceIncludesDatabaseName()
    {
        // Arrange
        var connectionString = await _fixture.GetDatabaseConnectionStringAsync();

        // Assert
        Assert.NotNull(connectionString);
        Assert.Contains("Database=testdb", connectionString);
    }

    [RequiresDockerFact]
    public async Task CanEstablishMultipleTcpConnections()
    {
        // Arrange
        var connectionInfo = await _fixture.GetConnectionInfoAsync();

        // Act & Assert - Establish multiple connections
        for (int i = 0; i < 3; i++)
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(connectionInfo.Host, connectionInfo.Port);
            Assert.True(tcpClient.Connected, $"Connection {i + 1} should succeed");
        }
    }

    [RequiresDockerFact]
    public async Task ServerResourceStartsSuccessfully()
    {
        // Arrange
        var connectionInfo = await _fixture.GetConnectionInfoAsync();

        // Assert - Verify server resource is initialized
        Assert.NotNull(connectionInfo);
        Assert.NotNull(connectionInfo.Host);
        Assert.True(connectionInfo.Port > 0);
        Assert.Equal(5236, connectionInfo.Port); // Default DMDB port
    }

    [RequiresDockerFact]
    public async Task HealthCheckPassesWhenDmdbIsReady()
    {
        // The fixture waits for the resource to become healthy before tests run
        // If we reach this point, the health check has passed
        var connectionInfo = await _fixture.GetConnectionInfoAsync();
        
        Assert.NotNull(connectionInfo);
        
        // Verify we can connect
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(connectionInfo.Host, connectionInfo.Port);
        Assert.True(tcpClient.Connected);
    }
}

public class DmdbFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IResourceBuilder<DmdbServerResource>? _dmdbServer;
    private IResourceBuilder<DmdbDatabaseResource>? _dmdbDatabase;

    public async Task InitializeAsync()
    {
        // Skip initialization if Docker/Aspire orchestration is unavailable
        if (!DockerTestEnvironment.IsContainerIntegrationTestAvailable())
        {
            return;
        }

        var builder = DistributedApplication.CreateBuilder();
        
        _dmdbServer = builder.AddDmdb("dmdb");
        _dmdbDatabase = _dmdbServer.AddDatabase("testdb");

        _app = builder.Build();
        await _app.StartAsync();

        // Wait for the database resource to become healthy
        // DMDB containers may take longer to start than PostgreSQL-based databases due to initialization overhead
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await _app.ResourceNotifications.WaitForResourceHealthyAsync(_dmdbDatabase.Resource.Name, cts.Token);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    public async Task<string> GetConnectionStringAsync()
    {
        if (!DockerTestEnvironment.IsContainerIntegrationTestAvailable())
        {
            throw new InvalidOperationException(
                "Container integration tests are disabled or unavailable. " +
                "Ensure Docker is installed/running and Aspire orchestration is configured.");
        }

        if (_dmdbServer?.Resource is null)
        {
            throw new InvalidOperationException("DMDB server resource is not initialized.");
        }

        var connectionString = await _dmdbServer.Resource.GetConnectionStringAsync();
        if (connectionString is null)
        {
            throw new InvalidOperationException("Connection string is null.");
        }
        
        return connectionString;
    }

    public async Task<string> GetDatabaseConnectionStringAsync()
    {
        if (!DockerTestEnvironment.IsContainerIntegrationTestAvailable())
        {
            throw new InvalidOperationException(
                "Container integration tests are disabled or unavailable. " +
                "Ensure Docker is installed/running and Aspire orchestration is configured.");
        }

        if (_dmdbDatabase?.Resource is null)
        {
            throw new InvalidOperationException("DMDB database resource is not initialized.");
        }

        var connectionString = await _dmdbDatabase.Resource.GetConnectionStringAsync();
        if (connectionString is null)
        {
            throw new InvalidOperationException("Connection string is null.");
        }
        
        return connectionString;
    }

    public async Task<DmdbConnectionInfo> GetConnectionInfoAsync()
    {
        if (!DockerTestEnvironment.IsContainerIntegrationTestAvailable())
        {
            throw new InvalidOperationException(
                "Container integration tests are disabled or unavailable. " +
                "Ensure Docker is installed/running and Aspire orchestration is configured.");
        }

        if (_dmdbServer?.Resource is null)
        {
            throw new InvalidOperationException("DMDB server resource is not initialized.");
        }

        var connectionString = await GetConnectionStringAsync();
        
        // Parse connection string to extract host and port
        // Format: "Host=host;Port=port;Username=username;Password=password;DBAPassword=password"
        var parts = connectionString.Split(';');
        string? host = null;
        int port = 5236; // Default

        foreach (var part in parts)
        {
            var trimmedPart = part.Trim();
            if (trimmedPart.StartsWith("Host=", StringComparison.OrdinalIgnoreCase))
            {
                host = trimmedPart["Host=".Length..];
            }
            else if (trimmedPart.StartsWith("Port=", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(trimmedPart["Port=".Length..], out var parsedPort))
                {
                    port = parsedPort;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("Unable to parse host from connection string.");
        }

        return new DmdbConnectionInfo(host, port);
    }
}

public record DmdbConnectionInfo(string Host, int Port);
