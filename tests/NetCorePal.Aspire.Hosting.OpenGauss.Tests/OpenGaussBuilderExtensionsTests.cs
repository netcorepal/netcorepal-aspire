using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace NetCorePal.Aspire.Hosting.OpenGauss.Tests;

public class OpenGaussBuilderExtensionsTests
{
    [Fact]
    public void AddOpenGaussAddsServerResource()
    {
        var builder = DistributedApplication.CreateBuilder();

        var opengauss = builder.AddOpenGauss("opengauss");

        Assert.NotNull(opengauss);
        Assert.Equal("opengauss", opengauss.Resource.Name);
        Assert.IsType<OpenGaussServerResource>(opengauss.Resource);
    }

    [Fact]
    public void AddOpenGaussWithDefaultParametersUsesDefaultPassword()
    {
        var builder = DistributedApplication.CreateBuilder();

        var opengauss = builder.AddOpenGauss("opengauss");

        Assert.NotNull(opengauss.Resource.PasswordParameter);
        Assert.Equal("opengauss-password", opengauss.Resource.PasswordParameter.Name);
    }

    [Fact]
    public void AddOpenGaussWithCustomPasswordUsesProvidedPassword()
    {
        var builder = DistributedApplication.CreateBuilder();
        var password = builder.AddParameter("custom-password", secret: true);

        var opengauss = builder.AddOpenGauss("opengauss", password: password);

        Assert.NotNull(opengauss.Resource.PasswordParameter);
        Assert.Equal("custom-password", opengauss.Resource.PasswordParameter.Name);
    }

    [Fact]
    public void AddOpenGaussWithCustomUserNameUsesProvidedUserName()
    {
        var builder = DistributedApplication.CreateBuilder();
        var userName = builder.AddParameter("custom-user");

        var opengauss = builder.AddOpenGauss("opengauss", userName: userName);

        Assert.NotNull(opengauss.Resource.UserNameParameter);
        Assert.Equal("custom-user", opengauss.Resource.UserNameParameter.Name);
    }

    [Fact]
    public void AddOpenGaussWithPortSetsHostPort()
    {
        var builder = DistributedApplication.CreateBuilder();

        var opengauss = builder.AddOpenGauss("opengauss", port: 5433);

        var endpoint = opengauss.Resource.Annotations.OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.Name == "tcp");

        Assert.NotNull(endpoint);
        Assert.Equal(5433, endpoint.Port);
    }

    [Fact]
    public void AddOpenGaussAddsEnvironmentVariables()
    {
        var builder = DistributedApplication.CreateBuilder();

        var opengauss = builder.AddOpenGauss("opengauss");

        var config = opengauss.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();
        Assert.NotEmpty(config);
    }

    [Fact]
    public void AddDatabaseAddsChildDatabaseResource()
    {
        var builder = DistributedApplication.CreateBuilder();
        var opengauss = builder.AddOpenGauss("opengauss");

        var database = opengauss.AddDatabase("mydb");

        Assert.NotNull(database);
        Assert.Equal("mydb", database.Resource.Name);
        Assert.Equal("mydb", database.Resource.DatabaseName);
        Assert.IsType<OpenGaussDatabaseResource>(database.Resource);
        Assert.Same(opengauss.Resource, database.Resource.Parent);
    }

    [Fact]
    public void AddDatabaseWithCustomNameUsesProvidedDatabaseName()
    {
        var builder = DistributedApplication.CreateBuilder();
        var opengauss = builder.AddOpenGauss("opengauss");

        var database = opengauss.AddDatabase("resource-name", "custom-db-name");

        Assert.Equal("resource-name", database.Resource.Name);
        Assert.Equal("custom-db-name", database.Resource.DatabaseName);
    }

    [Fact]
    public void AddDatabaseAddsToDatabaseDictionary()
    {
        var builder = DistributedApplication.CreateBuilder();
        var opengauss = builder.AddOpenGauss("opengauss");

        var database = opengauss.AddDatabase("mydb");

        Assert.True(opengauss.Resource.Databases.ContainsKey("mydb"));
        Assert.Equal("mydb", opengauss.Resource.Databases["mydb"]);
    }

    [Fact]
    public void WithDataVolumeAddsVolumeAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var opengauss = builder.AddOpenGauss("opengauss");

        opengauss.WithDataVolume();

        var volume = opengauss.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .FirstOrDefault(m => m.Target == "/var/lib/opengauss");

        Assert.NotNull(volume);
        Assert.Equal(ContainerMountType.Volume, volume.Type);
        Assert.Equal("opengauss-data", volume.Source);
        Assert.False(volume.IsReadOnly);
    }

    [Fact]
    public void WithDataVolumeWithCustomNameUsesProvidedName()
    {
        var builder = DistributedApplication.CreateBuilder();
        var opengauss = builder.AddOpenGauss("opengauss");

        opengauss.WithDataVolume("custom-volume");

        var volume = opengauss.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .FirstOrDefault(m => m.Target == "/var/lib/opengauss");

        Assert.NotNull(volume);
        Assert.Equal("custom-volume", volume.Source);
    }

    [Fact]
    public void WithDataBindMountAddsBindMountAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var opengauss = builder.AddOpenGauss("opengauss");

        opengauss.WithDataBindMount("/my/data/path");

        var mount = opengauss.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .FirstOrDefault(m => m.Target == "/var/lib/opengauss");

        Assert.NotNull(mount);
        Assert.Equal(ContainerMountType.BindMount, mount.Type);
        Assert.Equal("/my/data/path", mount.Source);
        Assert.False(mount.IsReadOnly);
    }

    [Fact]
    public void WithInitBindMountAddsBindMountAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var opengauss = builder.AddOpenGauss("opengauss");

        opengauss.WithInitBindMount("/my/init/scripts");

        var mount = opengauss.Resource.Annotations.OfType<ContainerMountAnnotation>()
            .FirstOrDefault(m => m.Target == "/docker-entrypoint-initdb.d");

        Assert.NotNull(mount);
        Assert.Equal(ContainerMountType.BindMount, mount.Type);
        Assert.Equal("/my/init/scripts", mount.Source);
    }

    [Fact]
    public void WithPasswordUpdatesPasswordParameter()
    {
        var builder = DistributedApplication.CreateBuilder();
        var opengauss = builder.AddOpenGauss("opengauss");
        var newPassword = builder.AddParameter("new-password", secret: true);

        opengauss.WithPassword(newPassword);

        Assert.Equal("new-password", opengauss.Resource.PasswordParameter.Name);
    }

    [Fact]
    public void WithUserNameUpdatesUserNameParameter()
    {
        var builder = DistributedApplication.CreateBuilder();
        var opengauss = builder.AddOpenGauss("opengauss");
        var userName = builder.AddParameter("new-user");

        opengauss.WithUserName(userName);

        Assert.NotNull(opengauss.Resource.UserNameParameter);
        Assert.Equal("new-user", opengauss.Resource.UserNameParameter.Name);
    }

    [Fact]
    public void WithHostPortUpdatesEndpointPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        var opengauss = builder.AddOpenGauss("opengauss");

        opengauss.WithHostPort(6543);

        var endpoint = opengauss.Resource.Annotations.OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.Name == "tcp");

        Assert.NotNull(endpoint);
        Assert.Equal(6543, endpoint.Port);
    }

    [Fact]
    public void RunAsPrivilegedAddsCommandLineArgsAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var opengauss = builder.AddOpenGauss("opengauss");

        opengauss.RunAsPrivileged();

        var annotation = opengauss.Resource.Annotations.OfType<CommandLineArgsCallbackAnnotation>()
            .FirstOrDefault();

        Assert.NotNull(annotation);
    }

    [Fact]
    public void OpenGaussServerResourceExposesConnectionString()
    {
        var builder = DistributedApplication.CreateBuilder();
        var opengauss = builder.AddOpenGauss("opengauss");

        var connectionStringResource = opengauss.Resource as IResourceWithConnectionString;

        Assert.NotNull(connectionStringResource);
        var connectionString = connectionStringResource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Host=", connectionString);
        Assert.Contains("Port=", connectionString);
        Assert.Contains("Username=", connectionString);
        Assert.Contains("Password=", connectionString);
    }

    [Fact]
    public void OpenGaussDatabaseResourceExposesConnectionString()
    {
        var builder = DistributedApplication.CreateBuilder();
        var opengauss = builder.AddOpenGauss("opengauss");
        var database = opengauss.AddDatabase("mydb");

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
    public void OpenGaussUsesCorrectContainerImage()
    {
        var builder = DistributedApplication.CreateBuilder();
        var opengauss = builder.AddOpenGauss("opengauss");

        var containerAnnotation = opengauss.Resource.Annotations.OfType<ContainerImageAnnotation>()
            .FirstOrDefault();

        Assert.NotNull(containerAnnotation);
        Assert.Equal("docker.io", containerAnnotation.Registry);
        Assert.Equal("opengauss/opengauss", containerAnnotation.Image);
        Assert.Equal("6.0.0", containerAnnotation.Tag);
    }

    [Fact]
    public void OpenGaussUsesDefaultUserNameGaussdb()
    {
        var builder = DistributedApplication.CreateBuilder();
        var opengauss = builder.AddOpenGauss("opengauss");

        Assert.Null(opengauss.Resource.UserNameParameter);
        var connectionString = opengauss.Resource.ConnectionStringExpression.ValueExpression;
        Assert.Contains("Username=gaussdb", connectionString);
    }
}
