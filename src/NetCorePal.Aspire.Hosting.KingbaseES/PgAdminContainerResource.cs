using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.KingbaseES;

/// <summary>
/// Represents a container resource for pgAdmin.
/// </summary>
/// <param name="name">The name of the container resource.</param>
public sealed class PgAdminContainerResource(string name) : ContainerResource(name);
