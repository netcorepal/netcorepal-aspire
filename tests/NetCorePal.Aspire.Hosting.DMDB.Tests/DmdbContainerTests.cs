using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Dm;
using Projects;

namespace NetCorePal.Aspire.Hosting.DMDB.Tests;

/// <summary>
/// Integration tests that verify DMDB containers start correctly and accept SQL commands.
/// These tests follow the pattern from https://github.com/netcorepal/netcorepal-testcontainers
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

    [Fact]
    public async Task ConnectionStateReturnsOpen()
    {
        // Arrange
        var connectionString = await _fixture.GetConnectionStringAsync();

        // Act
        await using var connection = new DmConnection(connectionString);
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
        var connectionString = await _fixture.GetConnectionStringAsync();
        await using var connection = new DmConnection(connectionString);
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
        var connectionString = await _fixture.GetConnectionStringAsync();
        await using var connection = new DmConnection(connectionString);
        await connection.OpenAsync();

        // Act - Create table
        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = @"
            CREATE TABLE test_table (
                id INT PRIMARY KEY,
                name VARCHAR(100),
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )";
        await createCommand.ExecuteNonQueryAsync();

        // Act - Insert data
        await using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = "INSERT INTO test_table (id, name) VALUES (?, ?)";
        insertCommand.Parameters.Add(new DmParameter { Value = 1 });
        insertCommand.Parameters.Add(new DmParameter { Value = "test_data" });
        await insertCommand.ExecuteNonQueryAsync();

        // Act - Query data
        await using var selectCommand = connection.CreateCommand();
        selectCommand.CommandText = "SELECT name FROM test_table WHERE id = ?";
        selectCommand.Parameters.Add(new DmParameter { Value = 1 });
        var retrievedName = await selectCommand.ExecuteScalarAsync();

        // Assert
        Assert.Equal("test_data", retrievedName);

        // Cleanup
        await using var dropCommand = connection.CreateCommand();
        dropCommand.CommandText = "DROP TABLE test_table";
        await dropCommand.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task CanExecuteVersionQuery()
    {
        // Arrange
        var connectionString = await _fixture.GetConnectionStringAsync();
        await using var connection = new DmConnection(connectionString);
        await connection.OpenAsync();

        // Act
        await using var command = connection.CreateCommand();
        // Use a simple query that returns database version info
        command.CommandText = "SELECT BANNER FROM V$VERSION WHERE ROWNUM = 1";
        var version = await command.ExecuteScalarAsync();

        // Assert
        Assert.NotNull(version);
        var versionString = version?.ToString() ?? "";
        // DMDB version string should not be empty
        Assert.False(string.IsNullOrEmpty(versionString), "Version string should not be empty");
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
        await using var connection = new DmConnection(connectionString);
        await connection.OpenAsync();
        Assert.Equal(System.Data.ConnectionState.Open, connection.State);

        // Verify we can execute queries on the specific database
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1;";
        var result = await command.ExecuteScalarAsync();
        Assert.NotNull(result);
        Assert.Equal(1, Convert.ToInt32(result));
    }

    [Fact]
    public async Task CanExecuteMultipleQueriesOnSameConnection()
    {
        // Arrange
        var connectionString = await _fixture.GetConnectionStringAsync();
        await using var connection = new DmConnection(connectionString);
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
        var connectionString = await _fixture.GetConnectionStringAsync();

        // Act - Open and close multiple connections
        for (int i = 0; i < 3; i++)
        {
            await using var connection = new DmConnection(connectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            var result = await command.ExecuteScalarAsync();

            Assert.NotNull(result);
            Assert.Equal(1, Convert.ToInt32(result));
        }
    }
}

public class DmdbFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IResourceBuilder<DmdbServerResource>? _dmdbServer;
    private IResourceBuilder<DmdbDatabaseResource>? _dmdbDatabase;

    public async Task InitializeAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<NetCorePal_Aspire_Hosting_SharedAppHost>();

        _dmdbServer = builder.AddDmdb("dmdb");
        _dmdbDatabase = _dmdbServer.AddDatabase("testdb");

        _app = builder.Build();
        await _app.StartAsync();

        // Wait for the database resource to become healthy instead of sleeping.
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
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
}