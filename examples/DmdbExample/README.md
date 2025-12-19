# DMDB Example

This example demonstrates how to use NetCorePal.Aspire.Hosting.DMDB to add DMDB (达梦数据库) support to a .NET Aspire application.

## Overview

DMDB is a Chinese domestic database management system. This example shows both basic and advanced configuration options.

## Usage

### Basic Setup

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add DMDB server with default settings
var dmdb = builder.AddDmdb("dmdb");

// Add a database
var database = dmdb.AddDatabase("testdb");

builder.Build().Run();
```

### Advanced Setup

```csharp
var password = builder.AddParameter("user-password", value: "Test@1234", secret: true);
var dbaPassword = builder.AddParameter("dba-password", value: "SYSDBA_abc123", secret: true);

var dmdb = builder.AddDmdb("dmdb-custom")
    .WithPassword(password)
    .WithDbaPassword(dbaPassword)
    .WithHostPort(5236)
    .WithDataVolume();

var database = dmdb.AddDatabase("mydb");
```

## Running the Example

To run this example, uncomment the `builder.Build().Run();` line in `Program.cs` and then:

```bash
dotnet run
```

Note: This requires Docker to be running on your machine as it will start a DMDB container.

## Configuration

- **Default Port**: 5236
- **Default User**: SYSDBA
- **Default Database**: testdb
- **Container Image**: cnxc/dm8:20250423-kylin
- **Privileged Mode**: Required (enabled by default)

## Connection String

The generated connection string follows this format:

```
Server=host:port;User Id=username;Password=password;Database=database
```
