#!/bin/sh

set -eu

dotnet ef database update --project SourceBase.Infrastructure --startup-project SourceBase.Api