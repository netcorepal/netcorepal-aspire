using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace NetCorePal.Aspire.Hosting.KingbaseES.Tests;

public class KingbaseESBuilderExtensionsTests
{
    [Fact]
    public void AddKingbaseESAddsServerResource()
    {
        var builder = DistributedApplication.CreateBuilder();

        var kingbasees = builder.AddKingbaseES("kingbasees");

        Assert.NotNull(kingbasees);
        Assert.Equal("kingbasees", kingbasees.Resource.Name);
        Assert.IsType<KingbaseESServerResource>(kingbasees.Resource);
    }

    [Fact]
    public void AddKingbaseESWithDefaultParametersUsesDefaultPassword()
    {
        var builder = DistributedApplication.CreateBuilder();

        var kingbasees = builder.AddKingbaseES("kingbasees");

        Assert.NotNull(kingbasees.Resource.PasswordParameter);
        Assert.Equal("kingbasees-password", kingbasees.Resource.PasswordParameter.Name);
    }

    [Fact]
    public void AddKingbaseESWithCustomPasswordUsesProvidedPassword()
    {
        var builder = DistributedApplication.CreateBuilder();
        var password = builder.AddParameter("custom-password", secret: true);

        var kingbasees = builder.AddKingbaseES("kingbasees", password: password);

        Assert.NotNull(kingbasees.Resource.PasswordParameter);
        Assert.Equal("custom-password", kingbasees.Resource.PasswordParameter.Name);
    }

    [Fact]
    public void AddKingbaseESAddsHealthCheckAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");

        var annotation = kingbasees.Resource.Annotations.OfType<HealthCheckAnnotation>().FirstOrDefault();

        Assert.NotNull(annotation);
        Assert.False(string.IsNullOrWhiteSpace(annotation!.Key));
    }

    [Fact]
    public void AddDatabaseAddsHealthCheckAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");
        var database = kingbasees.AddDatabase("mydb");

        var annotation = database.Resource.Annotations.OfType<HealthCheckAnnotation>().FirstOrDefault();

        Assert.NotNull(annotation);
        Assert.False(string.IsNullOrWhiteSpace(annotation!.Key));
    }

    [Fact]
    public void AddKingbaseESWithCustomUserNameUsesProvidedUserName()
    {
        var builder = DistributedApplication.CreateBuilder();
        var userName = builder.AddParameter("custom-user");

        var kingbasees = builder.AddKingbaseES("kingbasees", userName: userName);

        Assert.NotNull(kingbasees.Resource.UserNameParameter);
        Assert.Equal("custom-user", kingbasees.Resource.UserNameParameter.Name);
    }

    [Fact]
    public void AddKingbaseESWithPortSetsHostPort()
    {
        var builder = DistributedApplication.CreateBuilder();

        var kingbasees = builder.AddKingbaseES("kingbasees", port: 54322);

        var endpoint = kingbasees.Resource.Annotations.OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.Name == "tcp");

        Assert.NotNull(endpoint);
        Assert.Equal(54322, endpoint.Port);
    }

    [Fact]
    public void AddKingbaseESAddsEnvironmentVariables()
    {
        var builder = DistributedApplication.CreateBuilder();

        var kingbasees = builder.AddKingbaseES("kingbasees");

        var config = kingbasees.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();
        Assert.NotEmpty(config);
    }

    [Fact]
    public void AddDatabaseAddsChildDatabaseResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");

        var database = kingbasees.AddDatabase("mydb");

        Assert.NotNull(database);
        Assert.Equal("mydb", database.Resource.Name);
        Assert.Equal("mydb", database.Resource.DatabaseName);
        Assert.IsType<KingbaseESDatabaseResource>(database.Resource);
        Assert.Same(kingbasees.Resource, database.Resource.Parent);
    }

    [Fact]
    public void AddDatabaseWithCustomNameUsesProvidedDatabaseName()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");

        var database = kingbasees.AddDatabase("resource-name", "custom-db-name");

        Assert.Equal("resource-name", database.Resource.Name);
        Assert.Equal("custom-db-name", database.Resource.DatabaseName);
    }

    [Fact]
    public void AddDatabaseAddsToDatabaseDictionary()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");

        var database = kingbasees.AddDatabase("mydb");

        Assert.True(kingbasees.Resource.Databases.ContainsKey("mydb"));
        Assert.Equal("mydb", kingbasees.Resource.Databases["mydb"]);
    }

    [Fact]
    public void WithDataVolumeAddsVolumeAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");

        kingbasees.WithDataVolume();

        var volume = kingbasees.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .FirstOrDefault(m => m.Target == "/home/kingbase/cluster/data");

        Assert.NotNull(volume);
        Assert.Equal(ContainerMountType.Volume, volume.Type);
        Assert.Equal("kingbasees-data", volume.Source);
        Assert.False(volume.IsReadOnly);
    }

    [Fact]
    public void WithDataVolumeWithCustomNameUsesProvidedName()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");

        kingbasees.WithDataVolume("custom-volume");

        var volume = kingbasees.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .FirstOrDefault(m => m.Target == "/home/kingbase/cluster/data");

        Assert.NotNull(volume);
        Assert.Equal("custom-volume", volume.Source);
    }

    [Fact]
    public void WithDataBindMountAddsBindMountAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");

        kingbasees.WithDataBindMount("/my/data/path");

        var mount = kingbasees.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .FirstOrDefault(m => m.Target == "/home/kingbase/cluster/data");

        Assert.NotNull(mount);
        Assert.Equal(ContainerMountType.BindMount, mount.Type);
        Assert.Equal("/my/data/path", mount.Source);
        Assert.False(mount.IsReadOnly);
    }

    [Fact]
    public void WithInitBindMountAddsBindMountAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");

        kingbasees.WithInitBindMount("/my/init/scripts");

        var mount = kingbasees.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .FirstOrDefault(m => m.Target == "/docker-entrypoint-initdb.d");

        Assert.NotNull(mount);
        Assert.Equal(ContainerMountType.BindMount, mount.Type);
        Assert.Equal("/my/init/scripts", mount.Source);
    }

    [Fact]
    public void WithPasswordUpdatesPasswordParameter()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");
        var newPassword = builder.AddParameter("new-password", secret: true);

        kingbasees.WithPassword(newPassword);

        Assert.Equal("new-password", kingbasees.Resource.PasswordParameter.Name);
    }

    [Fact]
    public void WithUserNameUpdatesUserNameParameter()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");
        var userName = builder.AddParameter("new-user");

        kingbasees.WithUserName(userName);

        Assert.NotNull(kingbasees.Resource.UserNameParameter);
        Assert.Equal("new-user", kingbasees.Resource.UserNameParameter.Name);
    }

    [Fact]
    public void WithHostPortUpdatesEndpointPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");

        kingbasees.WithHostPort(54322);

        var endpoint = kingbasees.Resource.Annotations.OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.Name == "tcp");

        Assert.NotNull(endpoint);
        Assert.Equal(54322, endpoint.Port);
    }

    [Fact]
    public void KingbaseESServerResourceExposesConnectionString()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");

        var connectionStringResource = kingbasees.Resource as IResourceWithConnectionString;

        Assert.NotNull(connectionStringResource);
        var connectionString = connectionStringResource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Host=", connectionString);
        Assert.Contains("Port=", connectionString);
        Assert.Contains("Username=", connectionString);
        Assert.Contains("Password=", connectionString);
    }

    [Fact]
    public void KingbaseESDatabaseResourceExposesConnectionString()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");
        var database = kingbasees.AddDatabase("mydb");

        var connectionStringResource = database.Resource as IResourceWithConnectionString;

        Assert.NotNull(connectionStringResource);
        var connectionString = connectionStringResource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Host=", connectionString);
        Assert.Contains("Port=", connectionString);
        Assert.Contains("Username=", connectionString);
        Assert.Contains("Password=", connectionString);
        Assert.Contains("Database=mydb", connectionString);
    }

    [Fact]
    public void KingbaseESUsesCorrectContainerImage()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");

        var containerAnnotation = kingbasees.Resource.Annotations.OfType<ContainerImageAnnotation>()
            .FirstOrDefault();

        Assert.NotNull(containerAnnotation);
        Assert.Equal("docker.io", containerAnnotation.Registry);
        Assert.Equal("apecloud/kingbase", containerAnnotation.Image);
        Assert.Equal("v008r006c009b0014-unit", containerAnnotation.Tag);
    }

    [Fact]
    public void KingbaseESUsesDefaultUserNameSystem()
    {
        var builder = DistributedApplication.CreateBuilder();
        var kingbasees = builder.AddKingbaseES("kingbasees");

        Assert.Null(kingbasees.Resource.UserNameParameter);
        var connectionString = kingbasees.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Username=system", connectionString);
    }

    [Fact]
    public void WithPgAdminAddsContainerOnce()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddKingbaseES("kb1").WithPgAdmin(pga => pga.WithHostPort(8081));
        builder.AddKingbaseES("kb2").WithPgAdmin(pga => pga.WithHostPort(8082));

        Assert.Single(builder.Resources, r => r.Name.Equals("pgadmin", StringComparison.OrdinalIgnoreCase));

        var container = builder.Resources.Single(r => r.Name.Equals("pgadmin", StringComparison.OrdinalIgnoreCase));
        var createFile = container.Annotations.OfType<ContainerFileSystemCallbackAnnotation>().Single();
        Assert.Equal("/pgadmin4", createFile.DestinationPath);

        var endpoint = container.Annotations.OfType<EndpointAnnotation>().Single(e => e.Name == "http");
        Assert.Equal(8082, endpoint.Port);
    }

    [Fact]
    public void WithPgWebAddsContainerOnce()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddKingbaseES("kb1").WithPgWeb(pgweb => pgweb.WithHostPort(1000));
        builder.AddKingbaseES("kb2").WithPgWeb(pgweb => pgweb.WithHostPort(2000));

        Assert.Single(builder.Resources.OfType<global::Aspire.Hosting.KingbaseES.PgWebContainerResource>());

        var pgwebResource = builder.Resources.Single(r => r.Name.Equals("pgweb", StringComparison.OrdinalIgnoreCase));
        var endpoint = pgwebResource.Annotations.OfType<EndpointAnnotation>().Single(e => e.Name == "http");
        Assert.Equal(2000, endpoint.Port);
    }

    [Fact]
    public async Task WithPgWebProducesDefaultBookmarkWhenNoDatabase()
    {
        var builder = DistributedApplication.CreateBuilder();

        builder.AddKingbaseES("kb").WithPgWeb();

        using var app = builder.Build();

        var pgweb = builder.Resources.Single(r => r.Name.Equals("pgweb", StringComparison.OrdinalIgnoreCase));
        var createBookmarks = pgweb.Annotations.OfType<ContainerFileSystemCallbackAnnotation>().Single();

        var entries = await createBookmarks.Callback(new() { Model = pgweb, ServiceProvider = app.Services }, CancellationToken.None);

        var pgWebDirectory = Assert.IsType<ContainerDirectory>(entries.First());
        Assert.Equal(".pgweb", pgWebDirectory.Name);

        var bookmarksDirectory = Assert.IsType<ContainerDirectory>(Assert.Single(pgWebDirectory.Entries));
        Assert.Equal("bookmarks", bookmarksDirectory.Name);

        var file = Assert.Single(bookmarksDirectory.Entries.OfType<ContainerFile>());
        Assert.Equal("kb.toml", file.Name);
        Assert.Contains("host = \"kb\"", file.Contents);
        Assert.Contains("database = \"test\"", file.Contents);
    }
}
