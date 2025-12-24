using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Npgsql;
using Projects;

namespace NetCorePal.Aspire.Hosting.KingbaseES.Tests;

/// <summary>
/// Integration tests that verify KingbaseES containers start correctly and accept SQL commands.
/// These tests follow the pattern from https://github.com/netcorepal/netcorepal-testcontainers
/// 
/// To run these tests:
/// 1. Ensure Docker is installed and running
/// 2. Run: dotnet test --filter "FullyQualifiedName~KingbaseESContainerTests"
/// 
/// Note: KingbaseES container requires manual initialization after startup.
/// See the package README for initialization requirements.
/// </summary>
public class KingbaseESContainerTests : IClassFixture<KingbaseESFixture>
{
    private readonly KingbaseESFixture _fixture;

    public KingbaseESContainerTests(KingbaseESFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ConnectionStateReturnsOpen()
    {
        // Arrange
        var connectionString = await _fixture.GetDatabaseConnectionStringAsync();

        // Act
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Assert
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);

        // Also verify we can execute a simple query
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1;";
        var result = await command.ExecuteScalarAsync();
        Assert.NotNull(result);
        Assert.Equal(1, Convert.ToInt32(result));
    }

    [Fact]
    public async Task CanExecuteSimpleQuery()
    {
        // Arrange
        var connectionString = await _fixture.GetDatabaseConnectionStringAsync();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Act
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1;";
        var result = await command.ExecuteScalarAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, Convert.ToInt32(result));
    }

    [Fact]
    public async Task CanCreateTableAndInsertData()
    {
        // Arrange
        var connectionString = await _fixture.GetDatabaseConnectionStringAsync();
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

    [Fact]
    public async Task CanExecuteVersionQuery()
    {
        // Arrange
        var connectionString = await _fixture.GetDatabaseConnectionStringAsync();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Act
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT version();";
        var version = await command.ExecuteScalarAsync();

        // Assert
        Assert.NotNull(version);
        var versionString = version?.ToString() ?? "";
        // KingbaseES is PostgreSQL-compatible, version string may contain either
        Assert.True(
            versionString.Contains("Kingbase", StringComparison.OrdinalIgnoreCase) ||
            versionString.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase),
            $"Expected version string to contain 'Kingbase' or 'PostgreSQL', but got: {versionString}");
    }

    [Fact]
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

        // Verify we can execute queries on the specific database
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT current_database();";
        var result = await command.ExecuteScalarAsync();
        Assert.NotNull(result);
        Assert.Equal("testdb", result.ToString());
    }

    [Fact]
    public async Task CanExecuteMultipleQueriesOnSameConnection()
    {
        // Arrange
        var connectionString = await _fixture.GetDatabaseConnectionStringAsync();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // Act & Assert - Execute multiple queries
        for (int i = 1; i <= 5; i++)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT {i};";
            var result = await command.ExecuteScalarAsync();
            Assert.NotNull(result);
            Assert.Equal(i, Convert.ToInt32(result));
        }
    }

    [Fact]
    public async Task ConnectionPoolingWorks()
    {
        // Arrange
        var connectionString = await _fixture.GetDatabaseConnectionStringAsync();

        // Act - Open and close multiple connections
        for (int i = 0; i < 3; i++)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            var result = await command.ExecuteScalarAsync();

            Assert.NotNull(result);
            Assert.Equal(1, Convert.ToInt32(result));
        }
    }
}

public class KingbaseESFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IResourceBuilder<KingbaseESServerResource>? _kingbaseESServer;
    private IResourceBuilder<KingbaseESDatabaseResource>? _kingbaseESDatabase;

    public async Task InitializeAsync()
    {
        // Skip initialization if Docker/Aspire orchestration is unavailable
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<NetCorePal_Aspire_Hosting_SharedAppHost>();
        var password = builder.AddParameter("database-password", value: "Test@1234", secret: true);
        _kingbaseESServer = builder.AddKingbaseES("kingbasees").WithPassword(password);
        _kingbaseESDatabase = _kingbaseESServer.AddDatabase("testdb");
        _app = builder.Build();
        await _app.StartAsync();
        // Wait for the database resource to become healthy instead of sleeping.
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await _app.ResourceNotifications.WaitForResourceHealthyAsync(_kingbaseESDatabase.Resource.Name, cts.Token);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }


    public async Task<string> GetDatabaseConnectionStringAsync()
    {
        if (_kingbaseESDatabase?.Resource is null)
        {
            throw new InvalidOperationException("KingbaseES database resource is not initialized.");
        }

        var connectionString = await _kingbaseESDatabase.Resource.GetConnectionStringAsync();
        if (connectionString is null)
        {
            throw new InvalidOperationException("Connection string is null.");
        }

        // Add connection pooling parameter to match testcontainers implementation
        return connectionString + ";No Reset On Close=true;";
    }

    // Docker detection is centralized in DockerTestEnvironment.
}
