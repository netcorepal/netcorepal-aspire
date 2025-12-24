# KingbaseES Example

This is a sample .NET Aspire application that demonstrates how to use the NetCorePal.Aspire.Hosting.KingbaseES package.

## Running the Example

```bash
dotnet run
```

This will start a KingbaseES server container along with pgAdmin and pgweb for database management.

## Features Demonstrated

- Adding a KingbaseES server resource
- Creating a database
- Using password parameters
- Adding pgAdmin for database administration
- Adding pgweb for lightweight database browsing
- Optional data volume support (commented out)

## Accessing the Services

After running the application, you can access:

- **KingbaseES**: Default port 54321 (random port assigned by Aspire)
- **pgAdmin**: Web interface for database administration
- **pgweb**: Lightweight web-based database browser

Check the Aspire Dashboard for the actual assigned ports and connection strings.

## Important: Container Initialization

**Note:** The KingbaseES container requires manual initialization after startup. The container does not automatically start the database service.

To initialize the container after it starts:

```bash
# Find the container name from Docker
docker ps | grep kingbase

# Execute initialization commands
docker exec <container-name> sh -c "pgrep -x sshd >/dev/null 2>&1 || { ssh-keygen -A && /usr/sbin/sshd -D &; sleep 1; }"
docker exec <container-name> sh -c "HOSTNAME=\$(hostname) /home/kingbase/cluster/bin/docker-entrypoint.sh"
```

For automatic initialization, consider creating a custom Docker image. See the package README for more details.
