using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.OpenGauss;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding OpenGauss resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class OpenGaussBuilderExtensions
{
    private const int OpenGaussPortDefault = 5432;

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
    /// This version of the package defaults to the <c>6.0.0</c> tag of the <c>opengauss/opengauss</c> container image.
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

        var passwordParameter = password?.Resource ?? ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(builder, $"{name}-password");

        var openGaussServer = new OpenGaussServerResource(name, userName?.Resource, passwordParameter);

        return builder.AddResource(openGaussServer)
                      .WithEndpoint(port: port, targetPort: OpenGaussPortDefault, name: OpenGaussServerResource.PrimaryEndpointName)
                      .WithImage(OpenGaussContainerImageTags.Image, OpenGaussContainerImageTags.Tag)
                      .WithImageRegistry(OpenGaussContainerImageTags.Registry)
                      .WithEnvironment("GS_PASSWORD", openGaussServer.PasswordParameter)
                      .WithEnvironment("PGPASSWORD", openGaussServer.PasswordParameter)
                      .PublishAsContainer();
    }

    /// <summary>
    /// Adds an OpenGauss database to the application model.
    /// </summary>
    /// <param name="builder">The OpenGauss server resource builder.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="databaseName">The name of the database. If not provided, this defaults to the same value as <paramref name="name"/>.</param>
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
        string? databaseName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);

        // Use the resource name as the database name if it's not provided
        databaseName ??= name;

        builder.Resource.Databases.TryAdd(name, databaseName);

        var openGaussDatabase = new OpenGaussDatabaseResource(name, databaseName, builder.Resource);

        return builder.ApplicationBuilder.AddResource(openGaussDatabase);
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

        return builder.WithEndpoint(OpenGaussServerResource.PrimaryEndpointName, endpoint =>
        {
            endpoint.Port = port;
        });
    }

    /// <summary>
    /// Runs the OpenGauss container in privileged mode.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The <see cref="IResourceBuilder{OpenGaussServerResource}"/>.</returns>
    /// <remarks>
    /// OpenGauss may require privileged mode for certain operations. Use this method with caution as it grants extended privileges to the container.
    /// </remarks>
    public static IResourceBuilder<OpenGaussServerResource> RunAsPrivileged(
        this IResourceBuilder<OpenGaussServerResource> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithAnnotation(new CommandLineArgsCallbackAnnotation(args =>
        {
            args.Add("--privileged");
        }));
    }
}
