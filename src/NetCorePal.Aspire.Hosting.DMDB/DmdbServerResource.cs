using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a DMDB (达梦数据库) container.
/// </summary>
public class DmdbServerResource : ContainerResource, IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "tcp";

    /// <summary>
    /// Initializes a new instance of the <see cref="DmdbServerResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="userName">A parameter that contains the DMDB server user name, or <see langword="null"/> to use a default value.</param>
    /// <param name="password">A parameter that contains the DMDB server password. Must not be null.</param>
    /// <param name="dbaPassword">A parameter that contains the DMDB DBA password. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="password"/> or <paramref name="dbaPassword"/> is null.</exception>
    public DmdbServerResource(string name, ParameterResource? userName, ParameterResource password,
        ParameterResource dbaPassword) : base(name)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(dbaPassword);

        PrimaryEndpoint = new EndpointReference(this, PrimaryEndpointName);
        UserNameParameter = userName;
        PasswordParameter = password;
        DbaPasswordParameter = dbaPassword;
    }

    /// <summary>
    /// Gets the primary endpoint for the DMDB server.
    /// </summary>
    public EndpointReference PrimaryEndpoint { get; }

    /// <summary>
    /// Gets or sets the parameter that contains the DMDB server user name.
    /// </summary>
    public ParameterResource? UserNameParameter { get; set; }

    /// <summary>
    /// Gets a reference to the user name for the DMDB server.
    /// </summary>
    /// <remarks>
    /// Returns the user name parameter if specified, otherwise returns the default user name "SYSDBA".
    /// </remarks>
    internal ReferenceExpression UserNameReference =>
        UserNameParameter is not null
            ? ReferenceExpression.Create($"{UserNameParameter}")
            : ReferenceExpression.Create($"SYSDBA");

    /// <summary>
    /// Gets or sets the parameter that contains the DMDB server password.
    /// </summary>
    public ParameterResource PasswordParameter { get; set; }

    /// <summary>
    /// Gets or sets the parameter that contains the DMDB DBA password.
    /// </summary>
    public ParameterResource DbaPasswordParameter { get; set; }

    /// <summary>
    /// Gets the connection string expression for the DMDB server.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"Host={PrimaryEndpoint.Property(EndpointProperty.Host)};Port={PrimaryEndpoint.Property(EndpointProperty.Port)};Username={UserNameReference};Password={PasswordParameter};DBAPassword={DbaPasswordParameter};");

    /// <summary>
    /// Gets the connection string for the DMDB server.
    /// </summary>
    /// <param name="cancellationToken"> A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A connection string for the DMDB server in the form "Server=host:port;User Id=username;Password=password".</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionStringAsync(cancellationToken);
        }

        // Return the evaluated connection string so placeholders are resolved.
        return ConnectionStringExpression.GetValueAsync(cancellationToken);
    }

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the database name.
    /// </summary>
    public Dictionary<string, string> Databases { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}