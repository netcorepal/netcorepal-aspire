# netcorepal-aspire

[![Release Build](https://img.shields.io/github/actions/workflow/status/netcorepal/netcorepal-aspire/release.yml?label=release%20build)](https://github.com/netcorepal/netcorepal-aspire/actions/workflows/release.yml)
[![Preview Build](https://img.shields.io/github/actions/workflow/status/netcorepal/netcorepal-aspire/dotnet.yml?label=preview%20build)](https://github.com/netcorepal/netcorepal-aspire/actions/workflows/dotnet.yml)
[![NuGet](https://img.shields.io/nuget/v/NetCorePal.Aspire.Hosting.OpenGauss.svg)](https://www.nuget.org/packages/NetCorePal.Aspire.Hosting.OpenGauss)
[![NuGet Version](https://img.shields.io/nuget/vpre/NetCorePal.Aspire.Hosting.OpenGauss?label=nuget-pre)](https://www.nuget.org/packages/NetCorePal.Aspire.Hosting.OpenGauss)
[![MyGet Version](https://img.shields.io/myget/netcorepal/vpre/NetCorePal.Aspire.Hosting.OpenGauss?label=myget-nightly)](https://www.myget.org/feed/netcorepal/package/nuget/NetCorePal.Aspire.Hosting.OpenGauss)
[![GitHub license](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/netcorepal/netcorepal-aspire/blob/main/LICENSE)

面向 .NET Aspire 的托管扩展包集合：为常用基础设施提供 Aspire 托管封装，便于本地开发和容器编排。

## 已实现的 Aspire 托管扩展

| 包名 | Release版本 | Preview版本 |
| --- | --- | --- |
| [NetCorePal.Aspire.Hosting.OpenGauss](https://www.nuget.org/packages/NetCorePal.Aspire.Hosting.OpenGauss)（[文档](./src/NetCorePal.Aspire.Hosting.OpenGauss/README.md)） | [![NuGet](https://img.shields.io/nuget/v/NetCorePal.Aspire.Hosting.OpenGauss.svg)](https://www.nuget.org/packages/NetCorePal.Aspire.Hosting.OpenGauss) | [![NuGet Version](https://img.shields.io/nuget/vpre/NetCorePal.Aspire.Hosting.OpenGauss?label=nuget-pre)](https://www.nuget.org/packages/NetCorePal.Aspire.Hosting.OpenGauss) |
| [NetCorePal.Aspire.Hosting.DMDB](https://www.nuget.org/packages/NetCorePal.Aspire.Hosting.DMDB)（[文档](./src/NetCorePal.Aspire.Hosting.DMDB/README.md)） | [![NuGet](https://img.shields.io/nuget/v/NetCorePal.Aspire.Hosting.DMDB.svg)](https://www.nuget.org/packages/NetCorePal.Aspire.Hosting.DMDB) | [![NuGet Version](https://img.shields.io/nuget/vpre/NetCorePal.Aspire.Hosting.DMDB?label=nuget-pre)](https://www.nuget.org/packages/NetCorePal.Aspire.Hosting.DMDB) |
| [NetCorePal.Aspire.Hosting.MongoDB](https://www.nuget.org/packages/NetCorePal.Aspire.Hosting.MongoDB)（[文档](./src/NetCorePal.Aspire.Hosting.MongoDB/README.md)） | [![NuGet](https://img.shields.io/nuget/v/NetCorePal.Aspire.Hosting.MongoDB.svg)](https://www.nuget.org/packages/NetCorePal.Aspire.Hosting.MongoDB) | [![NuGet Version](https://img.shields.io/nuget/vpre/NetCorePal.Aspire.Hosting.MongoDB?label=nuget-pre)](https://www.nuget.org/packages/NetCorePal.Aspire.Hosting.MongoDB) |

## 本机快速验证

前置要求：已安装 .NET SDK；如需运行 Docker 集成测试，请先安装并启动 Docker（Docker Desktop / Linux Docker）。

### Windows（PowerShell）

```powershell
dotnet restore
dotnet build -c Release

# 运行全部测试（需要 Docker）
dotnet test -c Release

# 仅运行非 Docker 测试（跳过所有 [DockerFact]）
$env:SKIP_DOCKER_TESTS = "1"
dotnet test -c Release
Remove-Item Env:SKIP_DOCKER_TESTS
```

### macOS / Linux（bash）

```bash
dotnet restore
dotnet build -c Release

# 运行全部测试（需要 Docker）
dotnet test -c Release

# 仅运行非 Docker 测试（跳过所有 [DockerFact]）
SKIP_DOCKER_TESTS=1 dotnet test -c Release
```

## 预览版源

```text
https://www.myget.org/F/netcorepal/api/v3/index.json
```

## 贡献

欢迎贡献！请随时提交问题和拉取请求。
