# Standalone cron container: pg_dump the sourcebase-postgres service on a
# schedule and ship the gzipped dump to a Cloudflare R2 bucket via rclone.
# Built FROM the same postgres image/version as the main service so pg_dump
# always matches the server exactly (avoids version-skew dump issues).
FROM postgres:17-alpine

RUN apk add --no-cache rclone

COPY backup.sh /usr/local/bin/backup.sh
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /usr/local/bin/backup.sh /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
