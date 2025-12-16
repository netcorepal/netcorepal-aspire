using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Npgsql;

namespace NetCorePal.Aspire.Hosting.OpenGauss.Tests;

/// <summary>
/// Integration tests that verify OpenGauss containers start correctly and accept SQL commands.
/// These tests follow the pattern from https://github.com/netcorepal/netcorepal-testcontainers
/// 
/// To run these tests:
/// 1. Ensure Docker is installed and running
/// 2. Remove the Skip attribute from the test methods
/// 3. Run: dotnet test --filter "FullyQualifiedName~OpenGaussContainerTests"
/// </summary>
public class OpenGaussContainerTests : IClassFixture<OpenGaussFixture>
{
    private readonly OpenGaussFixture _fixture;

    public OpenGaussContainerTests(OpenGaussFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Requires Docker - Remove Skip to run with Docker")]
    public async Task ConnectionStateReturnsOpen()
    {
        // Arrange
        var connectionString = await _fixture.GetConnectionStringAsync();
        
        // Act
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Assert
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }

    [Fact(Skip = "Requires Docker - Remove Skip to run with Docker")]
    public async Task CanExecuteSimpleQuery()
    {
        // Arrange
        var connectionString = await _fixture.GetConnectionStringAsync();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Act
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1;";
        var result = await command.ExecuteScalarAsync();

        // Assert
        Assert.Equal(1, result);
    }

    [Fact(Skip = "Requires Docker - Remove Skip to run with Docker")]
    public async Task CanCreateTableAndInsertData()
    {
        // Arrange
        var connectionString = await _fixture.GetConnectionStringAsync();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Act - Create table
        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = @"
            CREATE TABLE IF NOT EXISTS test_table (
                id SERIAL PRIMARY KEY,
                name VARCHAR(100),
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );";
        await createCommand.ExecuteNonQueryAsync();

        // Act - Insert data
        await using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO test_table (name) VALUES (@name) RETURNING id;";
        insertCommand.Parameters.AddWithValue("name", "test_data");
        var insertedId = await insertCommand.ExecuteScalarAsync();

        // Act - Query data
        await using var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = "SELECT name FROM test_table WHERE id = @id;";
        selectCommand.Parameters.AddWithValue("id", insertedId!);
        var retrievedName = await selectCommand.ExecuteScalarAsync();

        // Assert
        Assert.NotNull(insertedId);
        Assert.Equal("test_data", retrievedName);

        // Cleanup
        await using var dropCommand = connection.CreateCommand();
        dropCommand.CommandText = "DROP TABLE test_table;";
        await dropCommand.ExecuteNonQueryAsync();
    }

    [Fact(Skip = "Requires Docker - Remove Skip to run with Docker")]
    public async Task CanExecuteVersionQuery()
    {
        // Arrange
        var connectionString = await _fixture.GetConnectionStringAsync();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Act
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT version();";
        var version = await command.ExecuteScalarAsync();

        // Assert
        Assert.NotNull(version);
        var versionString = version?.ToString() ?? "";
        // OpenGauss is PostgreSQL-compatible, version string may contain either
        Assert.True(
            versionString.Contains("openGauss", StringComparison.OrdinalIgnoreCase) ||
            versionString.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase),
            $"Expected version string to contain 'openGauss' or 'PostgreSQL', but got: {versionString}");
    }

    [Fact(Skip = "Requires Docker - Remove Skip to run with Docker")]
    public async Task DatabaseResourceIncludesDatabaseName()
    {
        // Arrange
        var connectionString = await _fixture.GetDatabaseConnectionStringAsync();

        // Assert
        Assert.NotNull(connectionString);
        Assert.Contains("Database=testdb", connectionString);

        // Act - Verify connection works
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
    }
}

public class OpenGaussFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IResourceBuilder<OpenGaussServerResource>? _openGaussServer;
    private IResourceBuilder<OpenGaussDatabaseResource>? _openGaussDatabase;

    public async Task InitializeAsync()
    {
        // Skip initialization if Docker tests are disabled
        if (!IsDockerEnabled())
        {
            return;
        }

        var builder = DistributedApplication.CreateBuilder();
        
        _openGaussServer = builder.AddOpenGauss("opengauss");
        _openGaussDatabase = _openGaussServer.AddDatabase("testdb");

        _app = builder.Build();
        await _app.StartAsync();

        // Give the container time to fully start
        await Task.Delay(TimeSpan.FromSeconds(10));
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
        if (!IsDockerEnabled())
        {
            throw new InvalidOperationException(
                "Docker integration tests are disabled. " +
                "Remove the Skip attribute from tests or ensure Docker is running.");
        }

        if (_openGaussServer?.Resource is null)
        {
            throw new InvalidOperationException("OpenGauss server resource is not initialized.");
        }

        var connectionString = await _openGaussServer.Resource.GetConnectionStringAsync();
        return connectionString ?? throw new InvalidOperationException("Connection string is null.");
    }

    public async Task<string> GetDatabaseConnectionStringAsync()
    {
        if (!IsDockerEnabled())
        {
            throw new InvalidOperationException(
                "Docker integration tests are disabled. " +
                "Remove the Skip attribute from tests or ensure Docker is running.");
        }

        if (_openGaussDatabase?.Resource is null)
        {
            throw new InvalidOperationException("OpenGauss database resource is not initialized.");
        }

        var connectionString = await _openGaussDatabase.Resource.GetConnectionStringAsync();
        return connectionString ?? throw new InvalidOperationException("Connection string is null.");
    }

    private static bool IsDockerEnabled()
    {
        // Allow tests to be skipped via environment variable
        return !string.Equals(Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS"), "1", StringComparison.Ordinal) &&
               !string.Equals(Environment.GetEnvironmentVariable("RUN_DOCKER_TESTS"), "0", StringComparison.Ordinal);
    }
}
