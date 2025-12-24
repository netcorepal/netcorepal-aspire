using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a KingbaseES database. This is a child resource of a <see cref="KingbaseESServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="databaseName">The database name.</param>
/// <param name="kingbaseESParentResource">The KingbaseES parent resource associated with this database.</param>
public class KingbaseESDatabaseResource(string name, string databaseName, KingbaseESServerResource kingbaseESParentResource) : Resource(name), IResourceWithParent<KingbaseESServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent KingbaseES container resource.
    /// </summary>
    public KingbaseESServerResource Parent { get; } = kingbaseESParentResource;

    /// <summary>
    /// Gets the connection string expression for the KingbaseES database.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create(
            $"Host={Parent.PrimaryEndpoint.Property(EndpointProperty.Host)};Port={Parent.PrimaryEndpoint.Property(EndpointProperty.Port)};Username={Parent.UserNameReference};Password={Parent.PasswordParameter};Database={DatabaseName}");

    /// <summary>
    /// Gets the database name.
    /// </summary>
    public string DatabaseName { get; } = databaseName;

    /// <summary>
    /// Gets the connection string for the KingbaseES database.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A connection string for the KingbaseES database.</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionStringAsync(cancellationToken);
        }

        return new ValueTask<string?>(ConnectionStringExpression.ValueExpression);
    }
}
