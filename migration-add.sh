#!/bin/sh

set -eu

if [ "$#" -lt 1 ]; then
    echo "Usage: sh migration-add.sh <MigrationName> [extra dotnet ef args]" >&2
    exit 1
fi

migration_name="$1"
shift

dotnet ef migrations add "$migration_name" --project SourceBase.Api --startup-project SourceBase.Api --output-dir Migrations "$@"