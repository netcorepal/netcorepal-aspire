using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace NetCorePal.Aspire.Hosting.DMDB.Tests;

public class DmdbBuilderExtensionsTests
{
    [Fact]
    public void AddDmdbAddsServerResource()
    {
        var builder = DistributedApplication.CreateBuilder();

        var dmdb = builder.AddDmdb("dmdb");

        Assert.NotNull(dmdb);
        Assert.Equal("dmdb", dmdb.Resource.Name);
        Assert.IsType<DmdbServerResource>(dmdb.Resource);
    }

    [Fact]
    public void AddDmdbWithDefaultParametersUsesDefaultPasswords()
    {
        var builder = DistributedApplication.CreateBuilder();

        var dmdb = builder.AddDmdb("dmdb");

        Assert.NotNull(dmdb.Resource.PasswordParameter);
        Assert.Equal("dmdb-password", dmdb.Resource.PasswordParameter.Name);
        Assert.NotNull(dmdb.Resource.DbaPasswordParameter);
        Assert.Equal("dmdb-dba-password", dmdb.Resource.DbaPasswordParameter.Name);
    }

    [Fact]
    public void AddDmdbWithCustomPasswordsUsesProvidedPasswords()
    {
        var builder = DistributedApplication.CreateBuilder();
        var password = builder.AddParameter("custom-password", secret: true);
        var dbaPassword = builder.AddParameter("custom-dba-password", secret: true);

        var dmdb = builder.AddDmdb("dmdb", password: password, dbaPassword: dbaPassword);

        Assert.NotNull(dmdb.Resource.PasswordParameter);
        Assert.Equal("custom-password", dmdb.Resource.PasswordParameter.Name);
        Assert.NotNull(dmdb.Resource.DbaPasswordParameter);
        Assert.Equal("custom-dba-password", dmdb.Resource.DbaPasswordParameter.Name);
    }

    [Fact]
    public void AddDmdbWithCustomUserNameUsesProvidedUserName()
    {
        var builder = DistributedApplication.CreateBuilder();
        var userName = builder.AddParameter("custom-user");

        var dmdb = builder.AddDmdb("dmdb", userName: userName);

        Assert.NotNull(dmdb.Resource.UserNameParameter);
        Assert.Equal("custom-user", dmdb.Resource.UserNameParameter.Name);
    }

    [Fact]
    public void AddDmdbAddsHealthCheckAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        var annotation = dmdb.Resource.Annotations.OfType<HealthCheckAnnotation>().FirstOrDefault();

        Assert.NotNull(annotation);
        Assert.False(string.IsNullOrWhiteSpace(annotation!.Key));
    }

    [Fact]
    public void AddDatabaseAddsHealthCheckAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");
        var database = dmdb.AddDatabase("mydb");

        var annotation = database.Resource.Annotations.OfType<HealthCheckAnnotation>().FirstOrDefault();

        Assert.NotNull(annotation);
        Assert.False(string.IsNullOrWhiteSpace(annotation!.Key));
    }

    [Fact]
    public void AddDmdbWithPortSetsHostPort()
    {
        var builder = DistributedApplication.CreateBuilder();

        var dmdb = builder.AddDmdb("dmdb", port: 5237);

        var endpoint = dmdb.Resource.Annotations.OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.Name == "tcp");

        Assert.NotNull(endpoint);
        Assert.Equal(5237, endpoint.Port);
    }

    [Fact]
    public void AddDmdbAddsEnvironmentVariables()
    {
        var builder = DistributedApplication.CreateBuilder();

        var dmdb = builder.AddDmdb("dmdb");

        var config = dmdb.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();
        Assert.NotEmpty(config);
    }

    [Fact]
    public void AddDmdbAddsContainerRuntimeArgs()
    {
        var builder = DistributedApplication.CreateBuilder();

        var dmdb = builder.AddDmdb("dmdb");

        var argsAnnotation = dmdb.Resource.Annotations.OfType<ContainerRuntimeArgsCallbackAnnotation>().FirstOrDefault();

        Assert.NotNull(argsAnnotation);
    }

    [Fact]
    public void AddDatabaseAddsChildDatabaseResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        var database = dmdb.AddDatabase("mydb");

        Assert.NotNull(database);
        Assert.Equal("mydb", database.Resource.Name);
        Assert.Equal("mydb", database.Resource.DatabaseName);
        Assert.IsType<DmdbDatabaseResource>(database.Resource);
        Assert.Same(dmdb.Resource, database.Resource.Parent);
    }

    [Fact]
    public void AddDatabaseWithCustomNameUsesProvidedDatabaseName()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        var database = dmdb.AddDatabase("resource-name", "custom-db-name");

        Assert.Equal("resource-name", database.Resource.Name);
        Assert.Equal("custom-db-name", database.Resource.DatabaseName);
    }

    [Fact]
    public void AddDatabaseAddsToDatabaseDictionary()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        var database = dmdb.AddDatabase("mydb");

        Assert.True(dmdb.Resource.Databases.ContainsKey("mydb"));
        Assert.Equal("mydb", dmdb.Resource.Databases["mydb"]);
    }

    [Fact]
    public void WithDataVolumeAddsVolumeAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        dmdb.WithDataVolume();

        var volume = dmdb.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .FirstOrDefault(m => m.Target == "/opt/dmdbms/data");

        Assert.NotNull(volume);
        Assert.Equal(ContainerMountType.Volume, volume.Type);
        Assert.Equal("dmdb-data", volume.Source);
        Assert.False(volume.IsReadOnly);
    }

    [Fact]
    public void WithDataVolumeWithCustomNameUsesProvidedName()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        dmdb.WithDataVolume("custom-volume");

        var volume = dmdb.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .FirstOrDefault(m => m.Target == "/opt/dmdbms/data");

        Assert.NotNull(volume);
        Assert.Equal("custom-volume", volume.Source);
    }

    [Fact]
    public void WithDataBindMountAddsBindMountAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        dmdb.WithDataBindMount("/my/data/path");

        var mount = dmdb.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .FirstOrDefault(m => m.Target == "/opt/dmdbms/data");

        Assert.NotNull(mount);
        Assert.Equal(ContainerMountType.BindMount, mount.Type);
        Assert.Equal("/my/data/path", mount.Source);
        Assert.False(mount.IsReadOnly);
    }

    [Fact]
    public void WithPasswordUpdatesPasswordParameter()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");
        var newPassword = builder.AddParameter("new-password", secret: true);

        dmdb.WithPassword(newPassword);

        Assert.Equal("new-password", dmdb.Resource.PasswordParameter.Name);
    }

    [Fact]
    public void WithDbaPasswordUpdatesDbaPasswordParameter()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");
        var newDbaPassword = builder.AddParameter("new-dba-password", secret: true);

        dmdb.WithDbaPassword(newDbaPassword);

        Assert.Equal("new-dba-password", dmdb.Resource.DbaPasswordParameter.Name);
    }

    [Fact]
    public void WithUserNameUpdatesUserNameParameter()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");
        var userName = builder.AddParameter("new-user");

        dmdb.WithUserName(userName);

        Assert.NotNull(dmdb.Resource.UserNameParameter);
        Assert.Equal("new-user", dmdb.Resource.UserNameParameter.Name);
    }

    [Fact]
    public void WithHostPortUpdatesEndpointPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        dmdb.WithHostPort(6543);

        var endpoint = dmdb.Resource.Annotations.OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.Name == "tcp");

        Assert.NotNull(endpoint);
        Assert.Equal(6543, endpoint.Port);
    }

    [Fact]
    public void DmdbServerResourceExposesConnectionString()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        var connectionStringResource = dmdb.Resource as IResourceWithConnectionString;

        Assert.NotNull(connectionStringResource);
        var connectionString = connectionStringResource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Host=", connectionString);
        Assert.Contains("Port=", connectionString);
        Assert.Contains("Username=", connectionString);
        Assert.Contains("Password=", connectionString);
        Assert.Contains("DBAPassword=", connectionString);
    }

    [Fact]
    public void DmdbDatabaseResourceExposesConnectionString()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");
        var database = dmdb.AddDatabase("mydb");

        var connectionStringResource = database.Resource as IResourceWithConnectionString;

        Assert.NotNull(connectionStringResource);
        var connectionString = connectionStringResource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Host=", connectionString);
        Assert.Contains("Port=", connectionString);
        Assert.Contains("Username=", connectionString);
        Assert.Contains("Password=", connectionString);
        Assert.Contains("DBAPassword=", connectionString);
        Assert.Contains("Database=mydb", connectionString);
    }

    [Fact]
    public void DmdbUsesCorrectContainerImage()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        var containerAnnotation = dmdb.Resource.Annotations.OfType<ContainerImageAnnotation>()
            .FirstOrDefault();

        Assert.NotNull(containerAnnotation);
        Assert.Equal("docker.io", containerAnnotation.Registry);
        Assert.Equal("cnxc/dm8", containerAnnotation.Image);
        Assert.Equal("20250423-kylin", containerAnnotation.Tag);
    }

    [Fact]
    public void DmdbUsesDefaultUserNameSYSDBA()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        Assert.Null(dmdb.Resource.UserNameParameter);
        var connectionString = dmdb.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Username=SYSDBA", connectionString);
    }
}
