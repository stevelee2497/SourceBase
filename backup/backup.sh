#!/bin/sh
set -eu

: "${POSTGRES_HOST:?missing}"
: "${POSTGRES_DB:?missing}"
: "${POSTGRES_USER:?missing}"
: "${POSTGRES_PASSWORD:?missing}"
: "${CF_ACCOUNT_ID:?missing}"
: "${CF_ACCESS_KEY_ID:?missing}"
: "${CF_SECRET_ACCESS_KEY:?missing}"
: "${R2_BUCKET:?missing}"
: "${R2_PREFIX:=sourcebase-postgres}"

# rclone reads its remote entirely from RCLONE_CONFIG_R2_* env vars — no config
# file, no secrets on disk.
export RCLONE_CONFIG_R2_TYPE=s3
export RCLONE_CONFIG_R2_PROVIDER=Cloudflare
export RCLONE_CONFIG_R2_ACCESS_KEY_ID="$CF_ACCESS_KEY_ID"
export RCLONE_CONFIG_R2_SECRET_ACCESS_KEY="$CF_SECRET_ACCESS_KEY"
export RCLONE_CONFIG_R2_ENDPOINT="https://${CF_ACCOUNT_ID}.r2.cloudflarestorage.com"
export RCLONE_CONFIG_R2_ACL=private
export RCLONE_CONFIG_R2_NO_CHECK_BUCKET=true

timestamp=$(date -u +%Y%m%dT%H%M%SZ)
dump_file="/tmp/${POSTGRES_DB}-${timestamp}.sql.gz"

echo "[backup] $(date -u +%FT%TZ) dumping ${POSTGRES_DB}@${POSTGRES_HOST}"
PGPASSWORD="$POSTGRES_PASSWORD" pg_dump -h "$POSTGRES_HOST" -U "$POSTGRES_USER" -d "$POSTGRES_DB" | gzip -9 > "$dump_file"

dest="r2:${R2_BUCKET}/${R2_PREFIX}/$(basename "$dump_file")"
echo "[backup] uploading to ${dest}"
rclone copyto "$dump_file" "$dest"

rm -f "$dump_file"
echo "[backup] $(date -u +%FT%TZ) done"
