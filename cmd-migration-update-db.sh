#!/bin/sh

set -eu

dotnet ef database update --project SourceBase.Api --startup-project SourceBase.Api