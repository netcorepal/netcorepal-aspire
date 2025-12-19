using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dmdb;

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

        var passwordParameter = password?.Resource ??
                                ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder,
                                    $"{name}-password");

        var dbaPasswordParameter = dbaPassword?.Resource ??
                                   ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder,
                                       $"{name}-dba-password");

        var dmdbServer = new DmdbServerResource(name, userName?.Resource, passwordParameter, dbaPasswordParameter);

        return builder.AddResource(dmdbServer)
            .WithContainerRuntimeArgs("--privileged")
            .WithEndpoint(port: port, targetPort: DmdbPortDefault,
                name: DmdbServerResource.PrimaryEndpointName)
            .WithImage(DmdbContainerImageTags.Image, DmdbContainerImageTags.Tag)
            .WithImageRegistry(DmdbContainerImageTags.Registry)
            .WithEnvironment("DM_USER_PWD", dmdbServer.PasswordParameter)
            .WithEnvironment("SYSDBA_PWD", dmdbServer.DbaPasswordParameter)
            .WithEnvironment("SYSAUDITOR_PWD", dmdbServer.DbaPasswordParameter)
            .PublishAsContainer();
    }

    /// <summary>
    /// Adds a DMDB database to the application model.
    /// </summary>
    /// <param name="builder">The DMDB server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{DmdbDatabaseResource}"/>.</returns>
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

        return builder.ApplicationBuilder.AddResource(dmdbDatabase);
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
}
