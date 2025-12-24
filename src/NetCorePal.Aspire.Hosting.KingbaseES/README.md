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

## Important Notes

### Container Initialization

The KingbaseES container image (`apecloud/kingbase`) uses systemd and requires special initialization steps after the container starts. The container does not automatically start the database service.

**Required initialization commands:**
```bash
# 1. Start SSH daemon (required for cluster communication)
pgrep -x sshd >/dev/null 2>&1 || { \
  ssh-keygen -A; \
  /usr/sbin/sshd -D -E /tmp/sshd.log >/dev/null 2>&1 &; \
  sleep 1; \
}

# 2. Run the docker-entrypoint script to initialize and start the database
HOSTNAME=$(hostname) /home/kingbase/cluster/bin/docker-entrypoint.sh
```

**Workarounds for Aspire:**

Since Aspire doesn't support executing commands inside containers after startup (unlike Testcontainers), you have these options:

1. **Use a Custom Dockerfile** (Recommended):
   Create a wrapper Dockerfile that handles initialization. An example initialization script `kingbase-init.sh` is included in the package:
   ```dockerfile
   FROM apecloud/kingbase:v008r006c009b0014-unit
   
   COPY kingbase-init.sh /usr/local/bin/
   RUN chmod +x /usr/local/bin/kingbase-init.sh
   
   ENTRYPOINT ["/usr/local/bin/kingbase-init.sh"]
   ```
   
   The `kingbase-init.sh` script included in this package handles:
   - Starting the SSH daemon (required for cluster communication)
   - Running the docker-entrypoint.sh script to initialize the database
   - Keeping the container running

2. **Manual Container Interaction**:
   After the container starts, manually execute the initialization commands:
   ```bash
   docker exec <container-name> sh -c "ssh-keygen -A && /usr/sbin/sshd &"
   docker exec <container-name> sh -c "HOSTNAME=\$(hostname) /home/kingbase/cluster/bin/docker-entrypoint.sh"
   ```

3. **Use InitBindMount with a startup script**:
   If the image supports init scripts in `/docker-entrypoint-initdb.d/`, you can mount a custom initialization script.

### Additional Notes

- KingbaseES is PostgreSQL-compatible and uses the Npgsql driver for connectivity
- The container runs in privileged mode by default, which is required for the KingbaseES systemd-based image
- The health check will wait for the database to be ready, but initialization must happen first
- For production use, consider creating a custom container image with automatic initialization

## License

See the repository LICENSE file for details.
