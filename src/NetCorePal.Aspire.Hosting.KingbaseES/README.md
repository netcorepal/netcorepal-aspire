# NetCorePal.Aspire.Hosting.KingbaseES

KingbaseES support for .NET Aspire applications.

## Overview

This package provides extension methods for adding KingbaseES database resources to .NET Aspire applications.

KingbaseES is a PostgreSQL-compatible database that can be used as a drop-in replacement for PostgreSQL in many scenarios.

## Getting Started

### Installation

```bash
dotnet add package NetCorePal.Aspire.Hosting.KingbaseES
```

### Usage

Add a KingbaseES resource to your application:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var kingbasees = builder.AddKingbaseES("kingbasees");

var database = kingbasees.AddDatabase("mydb");

builder.Build().Run();
```

### Configuration Options

#### Custom Password

```csharp
var password = builder.AddParameter("db-password", secret: true);
var kingbasees = builder.AddKingbaseES("kingbasees", password: password);
```

#### Custom Port

```csharp
var kingbasees = builder.AddKingbaseES("kingbasees", port: 54321);
```

#### Custom Username

```csharp
var userName = builder.AddParameter("db-user");
var kingbasees = builder.AddKingbaseES("kingbasees", userName: userName);
```

#### Data Volume

```csharp
var kingbasees = builder.AddKingbaseES("kingbasees")
    .WithDataVolume();
```

#### With pgAdmin

```csharp
var kingbasees = builder.AddKingbaseES("kingbasees")
    .WithPgAdmin();
```

#### With pgweb

```csharp
var kingbasees = builder.AddKingbaseES("kingbasees")
    .WithPgWeb();
```

## Connection String

The connection string for KingbaseES follows the PostgreSQL format:

```
Host=localhost;Port=54321;Username=system;Password=yourpassword;Database=test
```

## Default Values

- **Port**: 54321
- **Username**: system
- **Default Database**: test
- **Container Image**: apecloud/kingbase:v008r006c009b0014-unit

## Health Checks

This resource includes built-in health checks. When referenced as a dependency using `WaitFor()`, dependent resources will wait until the KingbaseES instance is ready.

## Notes

- KingbaseES is PostgreSQL-compatible and uses the Npgsql driver for connectivity
- The container runs in privileged mode by default, which is required for the KingbaseES systemd-based image
- The default database is created automatically on first startup

## License

See the repository LICENSE file for details.
