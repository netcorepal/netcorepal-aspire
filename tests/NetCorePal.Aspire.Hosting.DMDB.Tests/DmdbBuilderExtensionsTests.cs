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

        var envAnnotations = dmdb.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        Assert.NotEmpty(envAnnotations);
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
    public void AddDatabaseAddsResourceWithCorrectName()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        var database = dmdb.AddDatabase("mydb");

        Assert.NotNull(database);
        Assert.Equal("mydb", database.Resource.Name);
        Assert.IsType<DmdbDatabaseResource>(database.Resource);
        Assert.Equal("mydb", database.Resource.DatabaseName);
    }

    [Fact]
    public void AddDatabaseWithCustomDatabaseName()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        var database = dmdb.AddDatabase("resource-name", "custom-db-name");

        Assert.Equal("resource-name", database.Resource.Name);
        Assert.Equal("custom-db-name", database.Resource.DatabaseName);
    }

    [Fact]
    public void AddDatabaseAddsToParentResourceDatabases()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        var database = dmdb.AddDatabase("mydb");

        Assert.True(dmdb.Resource.Databases.ContainsKey("mydb"));
        Assert.Equal("mydb", dmdb.Resource.Databases["mydb"]);
    }

    [Fact]
    public void WithPasswordSetsPasswordParameter()
    {
        var builder = DistributedApplication.CreateBuilder();
        var password = builder.AddParameter("custom-password", secret: true);
        var dmdb = builder.AddDmdb("dmdb");

        dmdb.WithPassword(password);

        Assert.Equal("custom-password", dmdb.Resource.PasswordParameter.Name);
    }

    [Fact]
    public void WithDbaPasswordSetsDbaPasswordParameter()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dbaPassword = builder.AddParameter("custom-dba-password", secret: true);
        var dmdb = builder.AddDmdb("dmdb");

        dmdb.WithDbaPassword(dbaPassword);

        Assert.Equal("custom-dba-password", dmdb.Resource.DbaPasswordParameter.Name);
    }

    [Fact]
    public void WithUserNameSetsUserNameParameter()
    {
        var builder = DistributedApplication.CreateBuilder();
        var userName = builder.AddParameter("custom-user");
        var dmdb = builder.AddDmdb("dmdb");

        dmdb.WithUserName(userName);

        Assert.NotNull(dmdb.Resource.UserNameParameter);
        Assert.Equal("custom-user", dmdb.Resource.UserNameParameter.Name);
    }

    [Fact]
    public void WithHostPortSetsPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        dmdb.WithHostPort(5238);

        var endpoint = dmdb.Resource.Annotations.OfType<EndpointAnnotation>()
            .FirstOrDefault(e => e.Name == "tcp");

        Assert.NotNull(endpoint);
        Assert.Equal(5238, endpoint.Port);
    }

    [Fact]
    public void WithDataVolumeAddsVolumeAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        dmdb.WithDataVolume();

        var volumeAnnotation = dmdb.Resource.Annotations.OfType<ContainerMountAnnotation>().FirstOrDefault();

        Assert.NotNull(volumeAnnotation);
        Assert.Equal("/opt/dmdbms/data", volumeAnnotation.Target);
    }

    [Fact]
    public void WithDataBindMountAddsBindMountAnnotation()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        dmdb.WithDataBindMount("/data");

        var mountAnnotation = dmdb.Resource.Annotations.OfType<ContainerMountAnnotation>().FirstOrDefault();

        Assert.NotNull(mountAnnotation);
        Assert.Equal("/opt/dmdbms/data", mountAnnotation.Target);
        Assert.Equal("/data", mountAnnotation.Source);
    }

    [Fact]
    public void ConnectionStringExpressionContainsServerAndPort()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");

        var connectionString = dmdb.Resource.ConnectionStringExpression.ValueExpression;

        Assert.Contains("Server=", connectionString);
        Assert.Contains("User Id=", connectionString);
        Assert.Contains("Password=", connectionString);
    }

    [Fact]
    public void DatabaseConnectionStringExpressionContainsDatabase()
    {
        var builder = DistributedApplication.CreateBuilder();
        var dmdb = builder.AddDmdb("dmdb");
        var database = dmdb.AddDatabase("mydb");

        var connectionString = database.Resource.ConnectionStringExpression.ValueExpression;

        Assert.Contains("Server=", connectionString);
        Assert.Contains("User Id=", connectionString);
        Assert.Contains("Password=", connectionString);
        Assert.Contains("Database=mydb", connectionString);
    }
}
