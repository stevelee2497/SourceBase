#!/bin/sh

set -eu

dotnet run --project SourceBase.Api &
API_PID=$!

dotnet run --project SourceBase.Web &
WEB_PID=$!

trap 'kill $API_PID $WEB_PID 2>/dev/null' INT TERM

wait $API_PID $WEB_PID
