using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents an OpenGauss database. This is a child resource of a <see cref="OpenGaussServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="openGaussParentResource">The OpenGauss parent resource associated with this database.</param>
public class OpenGaussDatabaseResource(string name, string databaseName, OpenGaussServerResource openGaussParentResource) : Resource(name), IResourceWithParent<OpenGaussServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent OpenGauss container resource.
    /// </summary>
    public OpenGaussServerResource Parent { get; } = openGaussParentResource;

    /// <summary>
    /// Gets the connection string expression for the OpenGauss database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"Host={Parent.PrimaryEndpoint.Property(EndpointProperty.Host)};Port={Parent.PrimaryEndpoint.Property(EndpointProperty.Port)};Username={Parent.UserNameReference};Password={Parent.PasswordParameter};Database={DatabaseName}");

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = databaseName;

    /// <summary>
    /// Gets the connection string for the OpenGauss database.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A connection string for the OpenGauss database.</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionStringAsync(cancellationToken);
        }

        // Return the evaluated connection string so placeholders are resolved.
        return ConnectionStringExpression.GetValueAsync(cancellationToken);
    }
}
