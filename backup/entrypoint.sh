#!/bin/sh
set -eu

: "${BACKUP_SCHEDULE:=0 3 * * *}"

# busybox crond forks each job through a minimal shell, so the container's own
# env vars aren't guaranteed to reach it — snapshot the ones backup.sh needs
# and have the cron job source them explicitly before running.
env | grep -E '^(POSTGRES_|R2_|CF_ACCOUNT_ID)=' | sed 's/^/export /' > /etc/backup.env

echo "${BACKUP_SCHEDULE} . /etc/backup.env && /usr/local/bin/backup.sh >> /proc/1/fd/1 2>&1" > /etc/crontabs/root

echo "[backup] scheduled '${BACKUP_SCHEDULE}' -> r2://${R2_BUCKET:-backup}/${R2_PREFIX:-sourcebase-postgres}"
exec crond -f -d 8
