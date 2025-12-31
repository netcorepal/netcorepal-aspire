using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Projects;

namespace NetCorePal.Aspire.Hosting.MongoDB.Tests;

public class MongoReplicaSetBuilderExtensionsTests
{
    [Fact]
    public async Task Test_MongoReplicaSetBuilder_Extensions()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<NetCorePal_Aspire_Hosting_SharedAppHost>();

        var username = builder.AddParameter("mongo-user", "admin");
        var password = builder.AddParameter("mongo-password", "admin");
        const int port = 27017;

        var mongo = builder
            .AddMongoDB("mongo", port, username, password)
            .WithLifetime(ContainerLifetime.Persistent)
            .WithReplicaSet();

        var mongodb = mongo
            .AddDatabase("mongoDatabase");

        var mongoReplicaSet = builder
            .AddMongoReplicaSet("mongoDb", mongodb.Resource);


        var app = builder.Build();

        await app.ResourceNotifications
            .WaitForResourceHealthyAsync(mongoReplicaSet.Resource.Name, CancellationToken.None)
            .WaitAsync(TimeSpan.FromMinutes(2));
    }
}