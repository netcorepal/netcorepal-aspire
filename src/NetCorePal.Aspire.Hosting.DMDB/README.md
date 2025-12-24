
# NetCorePal.Aspire.Hosting.DMDB

DMDB（达梦数据库）对 .NET Aspire 应用的支持。

## 概述

本包为 .NET Aspire 应用提供 DMDB 数据库容器的托管支持。DMDB 是国产数据库管理系统。

## 安装

```bash
dotnet add package NetCorePal.Aspire.Hosting.DMDB
```

## 用法


### 基本用法

```csharp
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// 添加 DMDB 服务器
var dmdb = builder.AddDmdb("dmdb");

// 添加数据库
var database = dmdb.AddDatabase("mydb");

builder.Build().Run();
```

> ⚠️ 用户名可自定义，默认值为 SYSDBA。

### 自定义配置


```csharp
var password = builder.AddParameter("dmdb-password", secret: true);
var dbaPassword = builder.AddParameter("dmdb-dba-password", secret: true);
var userName = builder.AddParameter("custom-user"); // 可选，默认 SYSDBA

var dmdb = builder.AddDmdb("dmdb", userName: userName, password: password, dbaPassword: dbaPassword)
    .WithHostPort(5236)
    .WithDataVolume();

var database = dmdb.AddDatabase("mydb");
```

### 配合 WaitFor 使用

DMDB 资源内置健康检查。当资源被 `WaitFor` 扩展方法作为依赖引用时，依赖资源会等待 DMDB 资源可用后再启动：

```csharp
var dmdb = builder.AddDmdb("dmdb");
var database = dmdb.AddDatabase("mydb");

// API 会等待数据库就绪后再启动
var api = builder.AddProject<Projects.MyApi>("api")
    .WaitFor(database);
```

## 功能特性

- **基于容器的部署**：使用 `cnxc/dm8` Docker 镜像
- **密码管理**：支持用户密码和 DBA 密码
- **数据卷支持**：可通过卷或绑定挂载持久化数据
- **自定义端口映射**：可配置主机端口访问 DMDB
- **数据库管理**：便捷创建多个数据库
- **健康检查**：服务器和数据库资源内置健康检查

## 默认配置

- **镜像**：`cnxc/dm8:20250423-kylin`
- **端口**：5236
- **默认用户**：SYSDBA

> **注意：** 目前 DMDB 仅支持使用 `SYSDBA` 作为数据库用户，且用户密码和 DBA 密码必须设置为相同的值，否则容器无法正常启动。
- **默认数据库**：testdb
- **特权模式**：已启用（DMDB 运行所需）

## 连接字符串格式

DMDB 的连接字符串格式如下（与实际实现一致）：

```
Host=主机;Port=端口;Username=用户名;Password=用户密码;Database=数据库名;DBAPassword=DBA密码;
```

例如：

```
Host=127.0.0.1;Port=5236;Username=SYSDBA;Password=Test@1234;Database=testdb;DBAPassword=Test@1234;
```

## 许可证

本项目遵循父仓库的许可协议。
