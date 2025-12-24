using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents an OpenGauss container.
/// </summary>
public class OpenGaussServerResource : ContainerResource, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenGaussServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="userName">A parameter that contains the OpenGauss server user name, or <see langword="null"/> to use a default value.</param>
    /// <param name="password">A parameter that contains the OpenGauss server password. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="password"/> is null.</exception>
    public OpenGaussServerResource(string name, ParameterResource? userName, ParameterResource password) : base(name)
    {
        ArgumentNullException.ThrowIfNull(password);

        PrimaryEndpoint = new EndpointReference(this, PrimaryEndpointName);
        UserNameParameter = userName;
        PasswordParameter = password;
    }

    /// <summary>
    /// Gets the primary endpoint for the OpenGauss server.
    /// </summary>
    public EndpointReference PrimaryEndpoint { get; }

    /// <summary>
    /// Gets or sets the parameter that contains the OpenGauss server user name.
    /// </summary>
    public ParameterResource? UserNameParameter { get; set; }

    /// <summary>
    /// Gets a reference to the user name for the OpenGauss server.
    /// </summary>
    /// <remarks>
    /// Returns the user name parameter if specified, otherwise returns the default user name "gaussdb".
    /// </remarks>
    internal ReferenceExpression UserNameReference =>
        UserNameParameter is not null ?
            ReferenceExpression.Create($"{UserNameParameter}") :
            ReferenceExpression.Create($"gaussdb");

    /// <summary>
    /// Gets or sets the parameter that contains the OpenGauss server password.
    /// </summary>
    public ParameterResource PasswordParameter { get; set; }

    /// <summary>
    /// Gets the connection string expression for the OpenGauss server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"Host={PrimaryEndpoint.Property(EndpointProperty.Host)};Port={PrimaryEndpoint.Property(EndpointProperty.Port)};Username={UserNameReference};Password={PasswordParameter}");

    /// <summary>
    /// Gets the connection string for the OpenGauss server.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A connection string for the OpenGauss server in the form "Host=host;Port=port;Username=gaussdb;Password=password".</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionStringAsync(cancellationToken);
        }

        // Return the evaluated connection string so any reference placeholders
        // (bindings, parameter values, secrets) are resolved at runtime.
        return ConnectionStringExpression.GetValueAsync(cancellationToken);
    }

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    public Dictionary<string, string> Databases { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
