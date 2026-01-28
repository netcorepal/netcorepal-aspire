using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Npgsql;
using Projects;

namespace NetCorePal.Aspire.Hosting.OpenGauss.Tests;

/// <summary>
/// Integration tests that verify OpenGauss containers start correctly and accept SQL commands.
/// These tests follow the pattern from https://github.com/netcorepal/netcorepal-testcontainers
/// 
/// To run these tests:
/// 1. Ensure Docker is installed and running
/// 2. Run: dotnet test --filter "FullyQualifiedName~OpenGaussContainerTests"
/// </summary>
public abstract class OpenGaussContainerTestsBase
{
    private readonly OpenGaussFixtureBase _fixture;

    protected OpenGaussContainerTestsBase(OpenGaussFixtureBase fixture)
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
    public async Task Database_DbCompatibility_Should_Equal()
    {
        var connectionString = await _fixture.GetDatabaseConnectionStringAsync();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command2 = connection.CreateCommand();
        command2.CommandText = """
                                   SELECT name, setting
                                   FROM pg_settings
                                   WHERE name ~* 'sql_compatibility';
                               """;
        await using var reader = await command2.ExecuteReaderAsync();
        var compatibilitySettings = new List<(string Name, string Setting)>();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(0);
            var setting = reader.GetString(1);
            compatibilitySettings.Add((name, setting));
        }
        Assert.NotEmpty(compatibilitySettings);
        Assert.Equal(_fixture.Database!.Resource.DbCompatibility, compatibilitySettings[0].Setting);
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
        // OpenGauss is PostgreSQL-compatible, version string may contain either
        Assert.True(
            versionString.Contains("openGauss", StringComparison.OrdinalIgnoreCase) ||
            versionString.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase),
            $"Expected version string to contain 'openGauss' or 'PostgreSQL', but got: {versionString}");
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

public sealed class OpenGaussContainerTests : OpenGaussContainerTestsBase, IClassFixture<OpenGaussDefaultFixture>
{
    public OpenGaussContainerTests(OpenGaussDefaultFixture fixture)
        : base(fixture)
    {
    }
}

public sealed class OpenGaussDifferentPasswordContainerTests : OpenGaussContainerTestsBase, IClassFixture<OpenGaussDifferentPasswordFixture>
{
    public OpenGaussDifferentPasswordContainerTests(OpenGaussDifferentPasswordFixture fixture)
        : base(fixture)
    {
    }
}

public sealed class OpenGaussWithPgAdminContainerTests : OpenGaussContainerTestsBase, IClassFixture<OpenGaussWithPgAdminFixture>
{
    public OpenGaussWithPgAdminContainerTests(OpenGaussWithPgAdminFixture fixture)
        : base(fixture)
    {
    }
}

public sealed class OpenGaussWithPgWebContainerTests : OpenGaussContainerTestsBase, IClassFixture<OpenGaussWithPgWebFixture>
{
    public OpenGaussWithPgWebContainerTests(OpenGaussWithPgWebFixture fixture)
        : base(fixture)
    {
    }
}

public sealed class OpenGaussAddDatabaseNameOnlyContainerTests : OpenGaussContainerTestsBase, IClassFixture<OpenGaussAddDatabaseNameOnlyFixture>
{
    public OpenGaussAddDatabaseNameOnlyContainerTests(OpenGaussAddDatabaseNameOnlyFixture fixture)
        : base(fixture)
    {
    }
}

public abstract class OpenGaussFixtureBase : IAsyncLifetime
{
    private DistributedApplication? _app;
    private IResourceBuilder<OpenGaussServerResource>? _openGaussServer;
    private IResourceBuilder<OpenGaussDatabaseResource>? _openGaussDatabase;
    
    public IResourceBuilder<OpenGaussServerResource>? Server => _openGaussServer;
    public IResourceBuilder<OpenGaussDatabaseResource>? Database => _openGaussDatabase;

    protected virtual string ServerName => $"opengauss-{GetType().Name.ToLowerInvariant()}";

    protected virtual string DatabaseName => "testdb";

    protected virtual string DatabaseResourceName => $"{ServerName}-db";

    protected virtual string DbCompatibility => "PG";

    protected virtual string Password => "Test@1234";

    protected virtual string? UserName => null;

    protected virtual IResourceBuilder<OpenGaussServerResource> ConfigureServer(
        IResourceBuilder<OpenGaussServerResource> server,
        IDistributedApplicationTestingBuilder builder)
    {
        return server;
    }

    protected virtual IResourceBuilder<OpenGaussDatabaseResource> ConfigureDatabase(
        IResourceBuilder<OpenGaussDatabaseResource> database,
        IDistributedApplicationTestingBuilder builder)
    {
        return database;
    }

    protected virtual IResourceBuilder<OpenGaussDatabaseResource> AddDatabase(
        IResourceBuilder<OpenGaussServerResource> server,
        IDistributedApplicationTestingBuilder builder)
    {
        var database = server.AddDatabase(DatabaseResourceName, DatabaseName, DbCompatibility);
        return ConfigureDatabase(database, builder);
    }

    public async Task InitializeAsync()
    {
        // Skip initialization if Docker/Aspire orchestration is unavailable
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<NetCorePal_Aspire_Hosting_SharedAppHost>();
        var password = builder.AddParameter($"{ServerName}-password", value: Password, secret: true);
        var server = builder.AddOpenGauss(ServerName).WithPassword(password);

        if (!string.IsNullOrWhiteSpace(UserName))
        {
            var userName = builder.AddParameter($"{ServerName}-username", value: UserName!, secret: false);
            server = server.WithUserName(userName);
        }

        server = ConfigureServer(server, builder);

        var database = AddDatabase(server, builder);

        _openGaussServer = server;
        _openGaussDatabase = database;
        _app = builder.Build();
        await _app.StartAsync();
        // Wait for the database resource to become healthy instead of sleeping.
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await _app.ResourceNotifications.WaitForResourceHealthyAsync(_openGaussDatabase.Resource.Name, cts.Token);
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
        if (_openGaussDatabase?.Resource is null)
        {
            throw new InvalidOperationException("OpenGauss database resource is not initialized.");
        }

        var connectionString = await _openGaussDatabase.Resource.GetConnectionStringAsync();
        if (connectionString is null)
        {
            throw new InvalidOperationException("Connection string is null.");
        }

        // Add connection pooling parameter to match testcontainers implementation
        return connectionString;
    }

    // Docker detection is centralized in DockerTestEnvironment.
}

public sealed class OpenGaussDefaultFixture : OpenGaussFixtureBase
{
}

public sealed class OpenGaussDifferentPasswordFixture : OpenGaussFixtureBase
{
    protected override string Password => "Test@456";
}

public sealed class OpenGaussWithPgAdminFixture : OpenGaussFixtureBase
{
    protected override IResourceBuilder<OpenGaussServerResource> ConfigureServer(
        IResourceBuilder<OpenGaussServerResource> server,
        IDistributedApplicationTestingBuilder builder)
    {
        return server.WithPgAdmin();
    }
}

public sealed class OpenGaussWithPgWebFixture : OpenGaussFixtureBase
{
    protected override IResourceBuilder<OpenGaussServerResource> ConfigureServer(
        IResourceBuilder<OpenGaussServerResource> server,
        IDistributedApplicationTestingBuilder builder)
    {
        return server.WithPgWeb();
    }
}

public sealed class OpenGaussAddDatabaseNameOnlyFixture : OpenGaussFixtureBase
{
    protected override string DatabaseResourceName => "testdb";

    protected override IResourceBuilder<OpenGaussDatabaseResource> AddDatabase(
        IResourceBuilder<OpenGaussServerResource> server,
        IDistributedApplicationTestingBuilder builder)
    {
        var database = server.AddDatabase(DatabaseResourceName);
        return ConfigureDatabase(database, builder);
    }
}