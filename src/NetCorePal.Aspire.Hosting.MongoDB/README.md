````markdown
# NetCorePal.Aspire.Hosting.MongoDB

MongoDB support for .NET Aspire applications.

## Overview

This package provides extension methods for adding MongoDB resources to your .NET Aspire applications. It sets up the official MongoDB container image with sensible defaults, connection string handling, and built-in health checks.

## Installation

```bash
dotnet add package NetCorePal.Aspire.Hosting.MongoDB
```

## Usage

### Basic Usage

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var mongo = builder.AddMongoDB("mongo");
var database = mongo.AddDatabase("appdb");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(database);

builder.Build().Run();
```

### Custom Port and Credentials

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var rootUser = builder.AddParameter("mongo-user");
var rootPassword = builder.AddParameter("mongo-password", secret: true);

var mongo = builder.AddMongoDB(
        name: "mongo",
        userName: rootUser,
        password: rootPassword,
        databaseName: "admin",
        port: 27018)
    .WithDataVolume();

builder.Build().Run();
```

### Initialization Scripts

Mount initialization scripts that should run on container start:

```csharp
var mongo = builder.AddMongoDB("mongo")
    .WithInitBindMount("./init-scripts");
```

### Data Persistence

Use a named volume or bind mount for `/data/db`:

```csharp
var mongo = builder.AddMongoDB("mongo")
    .WithDataVolume();

// or

var mongo = builder.AddMongoDB("mongo")
    .WithDataBindMount("./mongo-data");
```

## Defaults

- Image: `mongo:7.0` (registry: `docker.io`)
- Port: `27017`
- Root user: `root`
- Default database: `admin`
- Health checks: server and per-database health checks using MongoDB ping

## Connection String Format

```
mongodb://{username}:{password}@{host}:{port}/{database}?authSource=admin
```

When using `AddDatabase`, the `{database}` placeholder comes from the resource name unless overridden.

## License

This project follows the same license as the repository it belongs to.
````

## 参考资料

https://github.com/joewashington75/AspireMongoDBReplicaSet
