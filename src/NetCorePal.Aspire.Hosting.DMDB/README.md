# NetCorePal.Aspire.Hosting.DMDB

DMDB (达梦数据库) support for .NET Aspire applications.

## Overview

This package provides support for hosting DMDB database containers in .NET Aspire applications. DMDB is a Chinese domestic database management system.

## Installation

```bash
dotnet add package NetCorePal.Aspire.Hosting.DMDB
```

## Usage

### Basic Usage

```csharp
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add DMDB server
var dmdb = builder.AddDmdb("dmdb");

// Add a database
var database = dmdb.AddDatabase("mydb");

builder.Build().Run();
```

### Custom Configuration

```csharp
var password = builder.AddParameter("user-password", secret: true);
var dbaPassword = builder.AddParameter("dba-password", secret: true);

var dmdb = builder.AddDmdb("dmdb")
    .WithPassword(password)
    .WithDbaPassword(dbaPassword)
    .WithHostPort(5236)
    .WithDataVolume();

var database = dmdb.AddDatabase("mydb");
```

## Features

- **Container-based deployment**: Uses the `cnxc/dm8` Docker image
- **Password management**: Support for both user and DBA passwords
- **Volume support**: Persist data using volumes or bind mounts
- **Custom port mapping**: Configure host port for DMDB access
- **Database management**: Easy creation of multiple databases

## Default Configuration

- **Image**: `cnxc/dm8:20250423-kylin`
- **Port**: 5236
- **Default User**: SYSDBA
- **Default Database**: testdb
- **Privileged Mode**: Enabled (required by DMDB)

## Connection String Format

The connection string for DMDB follows this format:

```
Server=host:port;User Id=username;Password=password;Database=database
```

## License

This project is licensed under the same terms as the parent repository.
