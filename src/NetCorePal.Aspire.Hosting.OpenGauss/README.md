# NetCorePal.Aspire.Hosting.OpenGauss

OpenGauss support for .NET Aspire applications.

## Overview

This package provides extension methods for adding OpenGauss database resources to your .NET Aspire applications. OpenGauss is an open-source relational database management system based on PostgreSQL.

## Installation

```bash
dotnet add package NetCorePal.Aspire.Hosting.OpenGauss
```

## Usage

### Basic Usage

Add an OpenGauss server resource to your Aspire application:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var opengauss = builder.AddOpenGauss("opengauss");
var db = opengauss.AddDatabase("mydb");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(db);

builder.Build().Run();
```

### Database Compatibility Modes

`AddDatabase` 支持指定 OpenGauss 兼容模式参数 `dbcompatibility`，用于按不同方言创建目标库：

- PG：PostgreSQL 兼容（默认）
- A：Oracle 兼容
- B：MySQL 兼容

示例：

```csharp
var opengauss = builder.AddOpenGauss("opengauss");

// 按 PostgreSQL 兼容创建数据库（等同默认）
var dbPg = opengauss.AddDatabase("mydb_pg", dbcompatibility: "PG");

// 按 Oracle 兼容创建数据库
var dbOracle = opengauss.AddDatabase("mydb_oracle", dbcompatibility: "A");

// 按 MySQL 兼容创建数据库
var dbMySql = opengauss.AddDatabase("mydb_mysql", dbcompatibility: "B");
```

健康检查行为：
- 先连接默认库 `postgres` 执行 `SELECT 1;`
- 探活成功后，若目标库不存在则执行 `CREATE DATABASE <db> DBCOMPATIBILITY = '<mode>';`

### Custom Port and Credentials

Specify a custom port and credentials:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter("opengauss-password", secret: true);
var opengauss = builder.AddOpenGauss("opengauss", password: password, port: 5432);

builder.Build().Run();
```

### Data Persistence

Use a named volume for data persistence:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var opengauss = builder.AddOpenGauss("opengauss")
    .WithDataVolume();

builder.Build().Run();
```

Or use a bind mount:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var opengauss = builder.AddOpenGauss("opengauss")
    .WithDataBindMount("./opengauss-data");

builder.Build().Run();
```

### Initialization Scripts

Mount initialization scripts to the container:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var opengauss = builder.AddOpenGauss("opengauss")
    .WithInitBindMount("./init-scripts");

builder.Build().Run();
```

### Privileged Mode

OpenGauss may require privileged mode for certain operations. Enable it using:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var opengauss = builder.AddOpenGauss("opengauss")
    .RunAsPrivileged();

builder.Build().Run();
```

**Note**: Use privileged mode with caution as it grants extended privileges to the container.

## Features

- **Container-based deployment**: Uses the official `opengauss/opengauss` Docker image
- **Built-in health checks**: Automatically waits for the database to be ready
    - Server 级健康检查：默认库 `postgres` 探活
    - Database 级健康检查：默认库探活后按 `dbcompatibility` 创建目标库（若不存在）
- **Connection string management**: Automatically generates connection strings for dependent resources
- **Data persistence**: Support for volumes and bind mounts
- **Initialization scripts**: Easy mounting of initialization scripts
- **Parameter support**: Configurable username and password using Aspire parameters

## Connection String Format

The connection string follows the standard PostgreSQL/Npgsql format:

```
Host={host};Port={port};Username={username};Password={password};Database={database}
```

Default values:
- Port: `5432`
- Username: `gaussdb`
- Password: Auto-generated or provided via parameter
- Database: Specified when calling `AddDatabase()`

## Requirements

- .NET 9.0 or later
- Docker (for local development)
- .NET Aspire 13.0.2 or later

## License

This project follows the same license as the repository it belongs to.
