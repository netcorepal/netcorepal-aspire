using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.KingbaseES;

/// <summary>
/// Represents a container resource for pgweb.
/// </summary>
/// <param name="name">The name of the container resource.</param>
public sealed class PgWebContainerResource(string name) : ContainerResource(name)
{
    internal const string PrimaryEndpointName = "http";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the pgweb.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);
}
