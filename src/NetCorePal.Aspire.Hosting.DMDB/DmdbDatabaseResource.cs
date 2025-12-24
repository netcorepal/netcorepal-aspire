using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a DMDB database. This is a child resource of a <see cref="DmdbServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="dmdbParentResource">The DMDB parent resource associated with this database.</param>
public class DmdbDatabaseResource(string name, string databaseName, DmdbServerResource dmdbParentResource) : Resource(name), IResourceWithParent<DmdbServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent DMDB container resource.
    /// </summary>
    public DmdbServerResource Parent { get; } = dmdbParentResource;

    /// <summary>
    /// Gets the connection string expression for the DMDB database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"Host={Parent.PrimaryEndpoint.Property(EndpointProperty.Host)};Port={Parent.PrimaryEndpoint.Property(EndpointProperty.Port)};Username={Parent.UserNameReference};Password={Parent.PasswordParameter};Database={DatabaseName};DBAPassword={Parent.DbaPasswordParameter};");

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = databaseName;

    /// <summary>
    /// Gets the connection string for the DMDB database.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A connection string for the DMDB database.</returns>
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
