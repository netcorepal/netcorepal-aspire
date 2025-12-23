using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dmdb;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net.Sockets;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding DMDB (达梦数据库) resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class DmdbBuilderExtensions
{
    private const int DmdbPortDefault = 5236;
    private const string DefaultDmdbUserName = "SYSDBA";
    private const string DefaultDatabaseName = "testdb";

    /// <summary>
    /// Adds a DMDB resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="userName">The parameter used to provide the user name for the DMDB resource. If <see langword="null"/> a default value will be used.</param>
    /// <param name="password">The parameter used to provide the user password for the DMDB resource. If <see langword="null"/> a random password will be generated.</param>
    /// <param name="dbaPassword">The parameter used to provide the DBA password for the DMDB resource. If <see langword="null"/> a random password will be generated.</param>
    /// <param name="port">The host port used when launching the container. If null a random port will be assigned.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{DmdbServerResource}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This resource includes built-in health checks. When this resource is referenced as a dependency
    /// using the <see cref="ResourceBuilderExtensions.WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/>
    /// extension method then the dependent resource will wait until the DMDB resource is able to service
    /// requests.
    /// </para>
    /// This version of the package defaults to the <c>20250423-kylin</c> tag of the <c>cnxc/dm8</c> container image.
    /// </remarks>
    public static IResourceBuilder<DmdbServerResource> AddDmdb(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource>? userName = null,
        IResourceBuilder<ParameterResource>? password = null,
        IResourceBuilder<ParameterResource>? dbaPassword = null,
        int? port = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        var dbaPasswordParameter = dbaPassword?.Resource ??
                                   ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder,
                                       $"{name}-dba-password", special: false);

        var dmdbServer = new DmdbServerResource(name, userName?.Resource, dbaPasswordParameter,
            dbaPasswordParameter);

        // Register a health check that can be associated with the resource so dependent resources can WaitFor() it.
        var serverHealthCheckKey = $"{name}-dmdb";
        builder.Services.AddHealthChecks().AddCheck(
            serverHealthCheckKey,
            new DmdbConnectionHealthCheck(
                ct => dmdbServer.ConnectionStringExpression.GetValueAsync(ct),
                DefaultDatabaseName));

        return builder.AddResource(dmdbServer)
            .WithContainerRuntimeArgs("--privileged")
            .WithEndpoint(port: port, targetPort: DmdbPortDefault,
                name: DmdbServerResource.PrimaryEndpointName)
            .WithImage(DmdbContainerImageTags.Image, DmdbContainerImageTags.Tag)
            .WithImageRegistry(DmdbContainerImageTags.Registry)
            .WithEnvironment("DM_USER_PWD", dmdbServer.PasswordParameter)
            .WithEnvironment("SYSDBA_PWD", dmdbServer.DbaPasswordParameter)
            .WithEnvironment("SYSAUDITOR_PWD", dmdbServer.DbaPasswordParameter)
            .WithHealthCheck(serverHealthCheckKey)
            .PublishAsContainer();
    }

    /// <summary>
    /// Adds a DMDB database to the application model.
    /// </summary>
    /// <param name="builder">The DMDB server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{DmdbDatabaseResource}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This resource includes built-in health checks. When this resource is referenced as a dependency
    /// using the <see cref="ResourceBuilderExtensions.WaitFor{T}(IResourceBuilder{T}, IResourceBuilder{IResource})"/>
    /// extension method then the dependent resource will wait until the DMDB database is available.
    /// </para>
    /// </remarks>
    public static IResourceBuilder<DmdbDatabaseResource> AddDatabase(
        this IResourceBuilder<DmdbServerResource> builder,
        string name,
        string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.Databases.TryAdd(name, databaseName);
        var dmdbDatabase = new DmdbDatabaseResource(name, databaseName, builder.Resource);

        var databaseHealthCheckKey = $"{builder.Resource.Name}-{name}-dmdbdb";
        builder.ApplicationBuilder.Services.AddHealthChecks().AddCheck(
            databaseHealthCheckKey,
            new DmdbConnectionHealthCheck(
                ct => dmdbDatabase.ConnectionStringExpression.GetValueAsync(ct),
                databaseName));

        return builder.ApplicationBuilder.AddResource(dmdbDatabase)
            .WithHealthCheck(databaseHealthCheckKey);
    }

    /// <summary>
    /// Adds a named volume for the data folder to a DMDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{DmdbServerResource}"/>.</returns>
    public static IResourceBuilder<DmdbServerResource> WithDataVolume(
        this IResourceBuilder<DmdbServerResource> builder,
        string? name = null,
        bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? $"{builder.Resource.Name}-data", "/opt/dmdbms/data", isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a DMDB container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{DmdbServerResource}"/>.</returns>
    public static IResourceBuilder<DmdbServerResource> WithDataBindMount(
        this IResourceBuilder<DmdbServerResource> builder,
        string source,
        bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        return builder.WithBindMount(source, "/opt/dmdbms/data", isReadOnly);
    }

    /// <summary>
    /// Configures the user password that the DMDB resource uses.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="password">The parameter used to provide the user password for the DMDB resource.</param>
    /// <returns>The <see cref="IResourceBuilder{DmdbServerResource}"/>.</returns>
    public static IResourceBuilder<DmdbServerResource> WithPassword(
        this IResourceBuilder<DmdbServerResource> builder,
        IResourceBuilder<ParameterResource> password)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(password);

        builder.Resource.PasswordParameter = password.Resource;
        builder.WithEnvironment("DM_USER_PWD", password.Resource);
        return builder;
    }

    /// <summary>
    /// Configures the DBA password that the DMDB resource uses.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="dbaPassword">The parameter used to provide the DBA password for the DMDB resource.</param>
    /// <returns>The <see cref="IResourceBuilder{DmdbServerResource}"/>.</returns>
    public static IResourceBuilder<DmdbServerResource> WithDbaPassword(
        this IResourceBuilder<DmdbServerResource> builder,
        IResourceBuilder<ParameterResource> dbaPassword)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(dbaPassword);

        builder.Resource.DbaPasswordParameter = dbaPassword.Resource;
        builder.WithEnvironment("SYSDBA_PWD", dbaPassword.Resource)
            .WithEnvironment("SYSAUDITOR_PWD", dbaPassword.Resource);
        return builder;
    }

    /// <summary>
    /// Configures the user name that the DMDB resource uses.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="userName">The parameter used to provide the user name for the DMDB resource.</param>
    /// <returns>The <see cref="IResourceBuilder{DmdbServerResource}"/>.</returns>
    public static IResourceBuilder<DmdbServerResource> WithUserName(
        this IResourceBuilder<DmdbServerResource> builder,
        IResourceBuilder<ParameterResource> userName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(userName);

        builder.Resource.UserNameParameter = userName.Resource;
        return builder;
    }

    /// <summary>
    /// Configures the host port that the DMDB resource is exposed on instead of using randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used random port will be assigned.</param>
    /// <returns>The <see cref="IResourceBuilder{DmdbServerResource}"/>.</returns>
    public static IResourceBuilder<DmdbServerResource> WithHostPort(
        this IResourceBuilder<DmdbServerResource> builder,
        int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint(DmdbServerResource.PrimaryEndpointName, endpoint => { endpoint.Port = port; });
    }

    private sealed class DmdbConnectionHealthCheck : IHealthCheck
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

        private readonly Func<CancellationToken, ValueTask<string?>> _getConnectionString;
        private readonly string _databaseName;

        public DmdbConnectionHealthCheck(Func<CancellationToken, ValueTask<string?>> getConnectionString,
            string databaseName)
        {
            _getConnectionString = getConnectionString;
            _databaseName = databaseName;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            string? connectionString;
            try
            {
                connectionString = await _getConnectionString(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Failed to resolve DMDB connection string.", ex);
            }

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return HealthCheckResult.Unhealthy("DMDB connection string is empty.");
            }

            // If the value provider hasn't resolved yet, the connection string may still contain
            // Aspire manifest placeholders like "{resource.bindings.tcp.host}".
            if (connectionString.Contains(".bindings.", StringComparison.Ordinal))
            {
                return HealthCheckResult.Unhealthy("DMDB connection string is not resolved yet.");
            }

            try
            {
                await using var connection = new Dm.DmConnection(connectionString);
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1;";
                var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                // Parse the connection string to extract host and port
                // Connection string format: "Server=host:port;User Id=username;Password=password;Database=database"
                var parts = connectionString.Split(';');
                string? host = null;
                int port = DmdbPortDefault;

                foreach (var part in parts)
                {
                    var trimmedPart = part.Trim();
                    if (trimmedPart.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
                    {
                        var serverValue = trimmedPart["Server=".Length..];
                        // Handle IPv6 addresses by finding the last colon for port separator
                        var lastColonIndex = serverValue.LastIndexOf(':');
                        if (lastColonIndex > 0)
                        {
                            host = serverValue[..lastColonIndex];
                            if (int.TryParse(serverValue[(lastColonIndex + 1)..], out var parsedPort))
                            {
                                port = parsedPort;
                            }
                        }
                        else
                        {
                            host = serverValue;
                        }
                        break;
                    }
                }

                return result is null
                    ? HealthCheckResult.Unhealthy("OpenGauss ping returned null.")
                    : HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("DMDB is not ready.", ex);
            }
        }
    }
}