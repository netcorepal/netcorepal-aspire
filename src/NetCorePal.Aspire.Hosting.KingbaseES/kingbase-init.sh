#!/bin/sh
# KingbaseES initialization script for Aspire
# This script handles the special initialization requirements for KingbaseES container

set -e

echo "Starting KingbaseES initialization..."

# Start SSH daemon if not running (required for cluster communication)
if ! pgrep -x sshd >/dev/null 2>&1; then
    echo "Starting SSH daemon..."
    if command -v ssh-keygen >/dev/null 2>&1; then
        ssh-keygen -A 2>/dev/null || true
    elif [ -f /usr/bin/ssh-keygen ]; then
        /usr/bin/ssh-keygen -A 2>/dev/null || true
    fi
    
    # Start sshd in background
    /usr/sbin/sshd -D -E /tmp/sshd.log >/dev/null 2>&1 &
    sleep 1
fi

# Run the docker entrypoint script to initialize and start the database
echo "Running KingbaseES docker-entrypoint.sh..."
HOSTNAME=$(hostname) /home/kingbase/cluster/bin/docker-entrypoint.sh >/tmp/kingbase-entrypoint.log 2>&1
exitcode=$?
echo $exitcode > /tmp/kingbase-entrypoint.exitcode

if [ $exitcode -ne 0 ]; then
    echo "ERROR: docker-entrypoint.sh failed with exit code $exitcode"
    cat /tmp/kingbase-entrypoint.log
    exit $exitcode
fi

echo "KingbaseES initialization completed successfully"

# Keep container running
tail -f /dev/null
