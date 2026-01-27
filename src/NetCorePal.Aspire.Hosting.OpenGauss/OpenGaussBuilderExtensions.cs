using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.OpenGauss;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;
using System.Text;
using System.Text.Json;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding OpenGauss resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class OpenGaussBuilderExtensions
{
    private const int OpenGaussPortDefault = 5432;
    private const string DefaultOpenGaussUserName = "gaussdb";
    private const string DefaultDatabaseName = "postgres";

    /// <summary>
    /// Adds an OpenGauss resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="userName">The parameter used to provide the user name for the OpenGauss resource. If <see langword="null"/> a default value will be used.</param>
    /// <param name="password">The parameter used to provide the administrator password for the OpenGauss resource. If <see langword="null"/> a random password will be generated.</param>
    /// <param name="port">The host port used when launching the container. If null a random port will be assigned.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{OpenGaussServerResource}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This resource includes built-in health checks. When this resource is referenced as a dependency
    /// using the <see cref="ResourceBuilderExtensions.WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/>
    /// extension method then the dependent resource will wait until the OpenGauss resource is able to service
    /// requests.
    /// </para>
    /// This version of the package defaults to the <c>latest</c> tag of the <c>opengauss/opengauss</c> container image.
    /// </remarks>
    public static IResourceBuilder<OpenGaussServerResource> AddOpenGauss(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource>? userName = null,
        IResourceBuilder<ParameterResource>? password = null,
        int? port = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var passwordParameter = password?.Resource ??
                                ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder,
                                    $"{name}-password");

        var openGaussServer = new OpenGaussServerResource(name, userName?.Resource, passwordParameter);

        // Register a health check that can be associated with the resource so dependent resources can WaitFor() it.
        var serverHealthCheckKey = $"{name}-opengauss";
        builder.Services.AddHealthChecks().AddCheck(
            serverHealthCheckKey,
            new ServerConnectionHealthCheck(
                ct => openGaussServer.ConnectionStringExpression.GetValueAsync(ct)));

        return builder.AddResource(openGaussServer)
            .WithContainerRuntimeArgs("--privileged")
            .WithEndpoint(port: port, targetPort: OpenGaussPortDefault,
                name: OpenGaussServerResource.PrimaryEndpointName)
            .WithImage(OpenGaussContainerImageTags.Image, OpenGaussContainerImageTags.Tag)
            .WithImageRegistry(OpenGaussContainerImageTags.Registry)
            .WithEnvironment("GS_PASSWORD", openGaussServer.PasswordParameter)
            .WithEnvironment("PGPASSWORD", openGaussServer.PasswordParameter) // OpenGauss is PostgreSQL-compatible and uses PGPASSWORD for client authentication
            //.WithEnvironment("GS_DB", "postgres") // Default database
            .WithHealthCheck(serverHealthCheckKey)
            .PublishAsContainer();
    }

    /// <summary>
    /// Adds a pgAdmin 4 administration and development platform for OpenGauss/PostgreSQL-compatible servers to the application model.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <c>9.9.0</c> tag of the <c>dpage/pgadmin4</c> container image.
    /// </remarks>
    /// <param name="builder">The OpenGauss server resource builder.</param>
    /// <param name="configureContainer">Callback to configure pgAdmin container resource.</param>
    /// <param name="containerName">The name of the container (Optional). Defaults to <c>pgadmin</c>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithPgAdmin<T>(
        this IResourceBuilder<T> builder,
        Action<IResourceBuilder<PgAdminContainerResource>>? configureContainer = null,
        string? containerName = null)
        where T : OpenGaussServerResource
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.Resources.OfType<PgAdminContainerResource>().SingleOrDefault() is { } existingPgAdminResource)
        {
            var builderForExistingResource = builder.ApplicationBuilder.CreateResourceBuilder(existingPgAdminResource);
            configureContainer?.Invoke(builderForExistingResource);
            return builder;
        }

        containerName ??= "pgadmin";

        var pgAdminContainer = new PgAdminContainerResource(containerName);
        var pgAdminContainerBuilder = builder.ApplicationBuilder.AddResource(pgAdminContainer)
            .WithImage(OpenGaussContainerImageTags.PgAdminImage, OpenGaussContainerImageTags.PgAdminTag)
            .WithImageRegistry(OpenGaussContainerImageTags.PgAdminRegistry)
            .WithHttpEndpoint(targetPort: 80, name: "http")
            .WithEnvironment(SetPgAdminEnvironmentVariables)
            .WithHttpHealthCheck("/browser")
            .ExcludeFromManifest();

        pgAdminContainerBuilder.WithContainerFiles(
            destinationPath: "/pgadmin4",
            callback: async (context, cancellationToken) =>
            {
                var openGaussInstances = builder.ApplicationBuilder.Resources.OfType<OpenGaussServerResource>();

                return [
                    new ContainerFile
                    {
                        Name = "servers.json",
                        Contents = await WritePgAdminServerJson(openGaussInstances, cancellationToken).ConfigureAwait(false),
                    },
                ];
            });

        configureContainer?.Invoke(pgAdminContainerBuilder);
        pgAdminContainerBuilder.WithRelationship(builder.Resource, "PgAdmin");

        return builder;
    }

    /// <summary>
    /// Configures the host port that the pgAdmin resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for pgAdmin.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    /// <returns>The resource builder for pgAdmin.</returns>
    public static IResourceBuilder<PgAdminContainerResource> WithHostPort(this IResourceBuilder<PgAdminContainerResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Adds an administration and development platform for OpenGauss/PostgreSQL-compatible databases to the application model using pgweb.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <c>0.16.2</c> tag of the <c>sosedoff/pgweb</c> container image.
    /// </remarks>
    /// <param name="builder">The OpenGauss server resource builder.</param>
    /// <param name="configureContainer">Configuration callback for pgweb container resource.</param>
    /// <param name="containerName">The name of the container (Optional). Defaults to <c>pgweb</c>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{OpenGaussServerResource}"/>.</returns>
    public static IResourceBuilder<OpenGaussServerResource> WithPgWeb(
        this IResourceBuilder<OpenGaussServerResource> builder,
        Action<IResourceBuilder<PgWebContainerResource>>? configureContainer = null,
        string? containerName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.Resources.OfType<PgWebContainerResource>().SingleOrDefault() is { } existingPgWebResource)
        {
            var builderForExistingResource = builder.ApplicationBuilder.CreateResourceBuilder(existingPgWebResource);
            configureContainer?.Invoke(builderForExistingResource);
            return builder;
        }

        containerName ??= "pgweb";

        var pgwebContainer = new PgWebContainerResource(containerName);
        var pgwebContainerBuilder = builder.ApplicationBuilder.AddResource(pgwebContainer)
            .WithImage(OpenGaussContainerImageTags.PgWebImage, OpenGaussContainerImageTags.PgWebTag)
            .WithImageRegistry(OpenGaussContainerImageTags.PgWebRegistry)
            .WithHttpEndpoint(targetPort: 8081, name: "http")
            .WithArgs("--bookmarks-dir=/.pgweb/bookmarks")
            .WithArgs("--sessions")
            .ExcludeFromManifest();

        configureContainer?.Invoke(pgwebContainerBuilder);
        pgwebContainerBuilder.WithRelationship(builder.Resource, "PgWeb");
        pgwebContainerBuilder.WithHttpHealthCheck();

        pgwebContainerBuilder.WithContainerFiles(
            destinationPath: "/",
            callback: async (_, ct) =>
            {
                var databases = builder.ApplicationBuilder.Resources.OfType<OpenGaussDatabaseResource>();
                var servers = builder.ApplicationBuilder.Resources.OfType<OpenGaussServerResource>();

                return [
                    new ContainerDirectory
                    {
                        Name = ".pgweb",
                        Entries = [
                            new ContainerDirectory
                            {
                                Name = "bookmarks",
                                Entries = await WritePgWebBookmarks(databases, servers, ct).ConfigureAwait(false)
                            },
                        ],
                    },
                ];
            });

        return builder;
    }

    /// <summary>
    /// Configures the host port that the pgweb resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for pgweb.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    /// <returns>The resource builder for pgweb.</returns>
    public static IResourceBuilder<PgWebContainerResource> WithHostPort(this IResourceBuilder<PgWebContainerResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }

    private static async Task<IEnumerable<ContainerFileSystemItem>> WritePgWebBookmarks(
        IEnumerable<OpenGaussDatabaseResource> openGaussDatabases,
        IEnumerable<OpenGaussServerResource> openGaussServers,
        CancellationToken cancellationToken)
    {
        var bookmarkFiles = new List<ContainerFileSystemItem>();
        var bookmarkNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var openGaussDatabase in openGaussDatabases)
        {
            var user = openGaussDatabase.Parent.UserNameParameter is null
                ? DefaultOpenGaussUserName
                : await openGaussDatabase.Parent.UserNameParameter.GetValueAsync(cancellationToken).ConfigureAwait(false);

            var password = await openGaussDatabase.Parent.PasswordParameter.GetValueAsync(cancellationToken).ConfigureAwait(false) ?? "password";

            // pgweb assumes OpenGauss is being accessed over a default Aspire container network and hardcodes the resource address.
                var fileContent = $"""
                    host = "{openGaussDatabase.Parent.Name}"
                    port = {openGaussDatabase.Parent.PrimaryEndpoint.TargetPort}
                    user = "{user}"
                    password = "{password}"
                    database = "{openGaussDatabase.DatabaseName}"
                    sslmode = "disable"
                    """;

            bookmarkFiles.Add(new ContainerFile
            {
                Name = $"{openGaussDatabase.Name}.toml",
                Contents = fileContent,
            });

            bookmarkNames.Add($"{openGaussDatabase.Name}.toml");
        }

        // If there are no database resources, pgweb would start with an empty bookmarks directory.
        // Add a sensible default bookmark per server to improve the out-of-box experience.
        if (!bookmarkFiles.OfType<ContainerFile>().Any())
        {
            foreach (var openGaussServer in openGaussServers)
            {
                var bookmarkFileName = $"{openGaussServer.Name}.toml";
                if (!bookmarkNames.Add(bookmarkFileName))
                {
                    continue;
                }

                var user = openGaussServer.UserNameParameter is null
                    ? DefaultOpenGaussUserName
                    : await openGaussServer.UserNameParameter.GetValueAsync(cancellationToken).ConfigureAwait(false);

                var password = await openGaussServer.PasswordParameter.GetValueAsync(cancellationToken).ConfigureAwait(false) ?? "password";

                // Default database should be "postgres" when no database resources are defined.
                var databaseName = DefaultDatabaseName;

                var fileContent = $"""
                    host = "{openGaussServer.Name}"
                    port = {openGaussServer.PrimaryEndpoint.TargetPort}
                    user = "{user}"
                    password = "{password}"
                    database = "{databaseName}"
                    sslmode = "disable"
                    """;

                bookmarkFiles.Add(new ContainerFile
                {
                    Name = bookmarkFileName,
                    Contents = fileContent,
                });
            }
        }

        return bookmarkFiles;
    }

    private static async Task<string> WritePgAdminServerJson(
        IEnumerable<OpenGaussServerResource> openGaussServers,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteStartObject("Servers");

        var serverIndex = 1;
        foreach (var openGaussServer in openGaussServers)
        {
            var endpoint = openGaussServer.PrimaryEndpoint;
            var userName = openGaussServer.UserNameParameter is null
                ? DefaultOpenGaussUserName
                : await openGaussServer.UserNameParameter.GetValueAsync(cancellationToken).ConfigureAwait(false);

            var password = await openGaussServer.PasswordParameter.GetValueAsync(cancellationToken).ConfigureAwait(false);

            writer.WriteStartObject($"{serverIndex}");
            writer.WriteString("Name", openGaussServer.Name);
            writer.WriteString("Group", "Servers");

            // pgAdmin assumes OpenGauss is being accessed over a default Aspire container network and hardcodes the resource address.
            writer.WriteString("Host", endpoint.Resource.Name);
            writer.WriteNumber("Port", (int)endpoint.TargetPort!);
            writer.WriteString("Username", userName);
            writer.WriteString("SSLMode", "prefer");
            writer.WriteString("MaintenanceDB", "postgres");
            writer.WriteString("PasswordExecCommand", $"echo '{password}'");
            writer.WriteEndObject();

            serverIndex++;
        }

        writer.WriteEndObject();
        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void SetPgAdminEnvironmentVariables(EnvironmentCallbackContext context)
    {
        context.EnvironmentVariables["PGADMIN_CONFIG_MASTER_PASSWORD_REQUIRED"] = "False";
        context.EnvironmentVariables["PGADMIN_CONFIG_SERVER_MODE"] = "False";
        context.EnvironmentVariables["PGADMIN_DEFAULT_EMAIL"] = "admin@domain.com";
        context.EnvironmentVariables["PGADMIN_DEFAULT_PASSWORD"] = "admin";

        var config = context.ExecutionContext.ServiceProvider.GetRequiredService<IConfiguration>();
        if (context.ExecutionContext.IsRunMode && config.GetValue<bool>("CODESPACES", false))
        {
            context.EnvironmentVariables["PGADMIN_CONFIG_PROXY_X_HOST_COUNT"] = "1";
            context.EnvironmentVariables["PGADMIN_CONFIG_PROXY_X_PREFIX_COUNT"] = "1";
        }
    }

    /// <summary>
    /// Adds an OpenGauss database to the application model.
    /// </summary>
    /// <param name="builder">The OpenGauss server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <param name="dbcompatibility">The database compatibility mode. Defaults to 'PG'. Supported values:
    /// 'PG' → PostgreSQL, 'A' → Oracle, 'B' → MySQL.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{OpenGaussDatabaseResource}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This resource includes built-in health checks. When this resource is referenced as a dependency
    /// using the <see cref="ResourceBuilderExtensions.WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/>
    /// extension method then the dependent resource will wait until the OpenGauss database is available.
    /// </para>
    /// </remarks>
    public static IResourceBuilder<OpenGaussDatabaseResource> AddDatabase(
        this IResourceBuilder<OpenGaussServerResource> builder,
        string name,
        string? databaseName = null,
        string dbcompatibility = "PG")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.Databases.TryAdd(name, databaseName);
        var openGaussDatabase = new OpenGaussDatabaseResource(name, databaseName, builder.Resource, dbcompatibility);

        var databaseHealthCheckKey = $"{builder.Resource.Name}-{name}-opengaussdb";
        builder.ApplicationBuilder.Services.AddHealthChecks().AddCheck(
            databaseHealthCheckKey,
            new DatabaseConnectionHealthCheck(
                ct => openGaussDatabase.ConnectionStringExpression.GetValueAsync(ct),
                databaseName,
                dbcompatibility));

        return builder.ApplicationBuilder.AddResource(openGaussDatabase)
            .WithHealthCheck(databaseHealthCheckKey);
    }

    private sealed class ServerConnectionHealthCheck : IHealthCheck
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

        private readonly Func<CancellationToken, ValueTask<string?>> _getConnectionString;
        
        public ServerConnectionHealthCheck(Func<CancellationToken, ValueTask<string?>> getConnectionString)
        {
            _getConnectionString = getConnectionString;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            string? connectionString;
            try
            {
                connectionString = await _getConnectionString(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Failed to resolve OpenGauss connection string.", ex);
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return HealthCheckResult.Unhealthy("OpenGauss connection string is empty.");
            }

            // If the value provider hasn't resolved yet, the connection string may still contain
            // Aspire manifest placeholders like "{resource.bindings.tcp.host}".
            if (connectionString.Contains(".bindings.", StringComparison.Ordinal))
            {
                return HealthCheckResult.Unhealthy("OpenGauss connection string is not resolved yet.");
            }

            try
            {
                // Server-level: ping default database only.
                var csb = new NpgsqlConnectionStringBuilder(connectionString)
                {
                    Timeout = (int)DefaultTimeout.TotalSeconds,
                    CommandTimeout = (int)DefaultTimeout.TotalSeconds,
                    Database = DefaultDatabaseName,
                };

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(DefaultTimeout);

                await using var connection = new NpgsqlConnection(csb.ConnectionString);
                await connection.OpenAsync(timeoutCts.Token).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1;";
                var result = await command.ExecuteScalarAsync(timeoutCts.Token).ConfigureAwait(false);

                return result is null
                    ? HealthCheckResult.Unhealthy("OpenGauss ping returned null.")
                    : HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("OpenGauss is not ready.", ex);
            }
        }
    }

    /// <summary>
    /// Health check for a target database. Pings default database 'postgres', then ensures
    /// the target database exists, creating it with the specified DBCOMPATIBILITY if missing.
    /// </summary>
    private sealed class DatabaseConnectionHealthCheck : IHealthCheck
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

        private readonly Func<CancellationToken, ValueTask<string?>> _getConnectionString;
        private readonly string _databaseName;
        private readonly string _dbCompatibility;

        public DatabaseConnectionHealthCheck(Func<CancellationToken, ValueTask<string?>> getConnectionString, string databaseName, string dbCompatibility)
        {
            _getConnectionString = getConnectionString;
            _databaseName = databaseName;
            _dbCompatibility = dbCompatibility;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            string? connectionString;
            try
            {
                connectionString = await _getConnectionString(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Failed to resolve OpenGauss connection string.", ex);
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return HealthCheckResult.Unhealthy("OpenGauss connection string is empty.");
            }

            if (connectionString.Contains(".bindings.", StringComparison.Ordinal))
            {
                return HealthCheckResult.Unhealthy("OpenGauss connection string is not resolved yet.");
            }

            try
            {
                // Connect to default database first
                var csb = new NpgsqlConnectionStringBuilder(connectionString)
                {
                    Timeout = (int)DefaultTimeout.TotalSeconds,
                    CommandTimeout = (int)DefaultTimeout.TotalSeconds,
                    Database = DefaultDatabaseName,
                };

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(DefaultTimeout);

                await using var connection = new NpgsqlConnection(csb.ConnectionString);
                await connection.OpenAsync(timeoutCts.Token).ConfigureAwait(false);

                // 1) Ping default database
                await using (var ping = connection.CreateCommand())
                {
                    ping.CommandText = "SELECT 1;";
                    var pingResult = await ping.ExecuteScalarAsync(timeoutCts.Token).ConfigureAwait(false);
                    if (pingResult is null)
                    {
                        return HealthCheckResult.Unhealthy("OpenGauss ping returned null.");
                    }
                }

                // 2) Ensure target database exists, create if missing with compatibility
                if (!string.Equals(_databaseName, DefaultDatabaseName, StringComparison.OrdinalIgnoreCase))
                {
                    await using (var existsCmd = connection.CreateCommand())
                    {
                        existsCmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @dbname;";
                        existsCmd.Parameters.AddWithValue("@dbname", _databaseName);
                        var exists = await existsCmd.ExecuteScalarAsync(timeoutCts.Token).ConfigureAwait(false);
                        if (exists is null)
                        {
                            var safeCompat = (_dbCompatibility ?? "PG").Trim();
                            var dbIdent = QuoteIdentifier(_databaseName);
                            await using var createCmd = connection.CreateCommand();
                            createCmd.CommandText = $"CREATE DATABASE {dbIdent} DBCOMPATIBILITY = '{safeCompat}';";
                            await createCmd.ExecuteNonQueryAsync(timeoutCts.Token).ConfigureAwait(false);
                        }
                    }
                }

                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("OpenGauss is not ready.", ex);
            }
        }
    }

    private static string QuoteIdentifier(string identifier)
    {
        // Quote with double quotes and escape internal quotes
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>
    /// Adds a named volume for the data folder to an OpenGauss container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{OpenGaussServerResource}"/>.</returns>
    public static IResourceBuilder<OpenGaussServerResource> WithDataVolume(
        this IResourceBuilder<OpenGaussServerResource> builder,
        string? name = null,
        bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? $"{builder.Resource.Name}-data", "/var/lib/opengauss", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the data folder to an OpenGauss container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{OpenGaussServerResource}"/>.</returns>
    public static IResourceBuilder<OpenGaussServerResource> WithDataBindMount(
        this IResourceBuilder<OpenGaussServerResource> builder,
        string source,
        bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/var/lib/opengauss", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the init folder to an OpenGauss container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{OpenGaussServerResource}"/>.</returns>
    public static IResourceBuilder<OpenGaussServerResource> WithInitBindMount(
        this IResourceBuilder<OpenGaussServerResource> builder,
        string source,
        bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/docker-entrypoint-initdb.d", isReadOnly);
    }

    /// <summary>
    /// Configures the password that the OpenGauss resource uses.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="password">The parameter used to provide the password for the OpenGauss resource.</param>
    /// <returns>The <see cref="IResourceBuilder{OpenGaussServerResource}"/>.</returns>
    public static IResourceBuilder<OpenGaussServerResource> WithPassword(
        this IResourceBuilder<OpenGaussServerResource> builder,
        IResourceBuilder<ParameterResource> password)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(password);

        builder.Resource.PasswordParameter = password.Resource;
        builder.WithEnvironment("GS_PASSWORD", password.Resource)
            .WithEnvironment("PGPASSWORD",
                password.Resource); // OpenGauss is PostgreSQL-compatible and uses PGPASSWORD for client authentication   
        return builder;
    }

    /// <summary>
    /// Configures the user name that the OpenGauss resource uses.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="userName">The parameter used to provide the user name for the OpenGauss resource.</param>
    /// <returns>The <see cref="IResourceBuilder{OpenGaussServerResource}"/>.</returns>
    public static IResourceBuilder<OpenGaussServerResource> WithUserName(
        this IResourceBuilder<OpenGaussServerResource> builder,
        IResourceBuilder<ParameterResource> userName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(userName);

        builder.Resource.UserNameParameter = userName.Resource;
        return builder;
    }

    /// <summary>
    /// Configures the host port that the OpenGauss resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    /// <returns>The <see cref="IResourceBuilder{OpenGaussServerResource}"/>.</returns>
    public static IResourceBuilder<OpenGaussServerResource> WithHostPort(
        this IResourceBuilder<OpenGaussServerResource> builder,
        int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint(OpenGaussServerResource.PrimaryEndpointName, endpoint => { endpoint.Port = port; });
    }
}