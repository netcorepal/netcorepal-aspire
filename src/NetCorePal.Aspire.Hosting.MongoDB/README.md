# NetCorePal.Aspire.Hosting.MongoDB

为 Aspire 提供创建 MongoDB 副本集的能力。

## 扩展方法

使用以下扩展方法将副本集添加到 MongoDB，可以指定内容路径、Dockerfile 以覆盖密钥文件名的位置：

```csharp
IResourceBuilder<MongoDBServerResource> WithReplicaSet(
    this IResourceBuilder<MongoDBServerResource> builder, 
    string contextPath, 
    string dockerFile, 
    string keyFileName = "/etc/mongo-keyfile")
```

或者，如果您想使用此 NuGet 包中包含的 `Mongo.Dockerfile`，可以使用以下扩展方法。此方法将尝试在执行程序集的同一目录中定位 `Mongo.Dockerfile`：

```csharp
IResourceBuilder<MongoDBServerResource> WithReplicaSet(
    this IResourceBuilder<MongoDBServerResource> builder)
```

使用扩展方法 `AddMongoReplicaSet()` 将 MongoDB 副本集资源添加到分布式应用程序构建器：

```csharp
IResourceBuilder<MongoReplicaSetResource> AddMongoReplicaSet(
    this IDistributedApplicationBuilder builder, 
    string name, 
    IResourceWithConnectionString mongoDbResource)
```

## 完整示例

使用提供的 `Mongo.Dockerfile` 和示例 API 项目的完整示例：

```csharp
var builder = DistributedApplication.CreateBuilder(args);

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

builder.AddProject<Projects.Api>("api")
    .WithReference(mongodb)
    .WithReference(mongoReplicaSet)
    .WaitFor(mongodb)
    .WaitFor(mongoReplicaSet);

await builder
    .Build()
    .RunAsync();
```

## 参考资料

https://github.com/joewashington75/AspireMongoDBReplicaSet
