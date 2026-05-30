#!/bin/sh

pkill -f "dotnet watch" || true
pkill -f "dotnet-watch.dll" || true
pkill -f "dotnet run.*SourceBase" || true
pkill -f "MSBuild.dll.*nodemode" || true

echo "All watchers and MSBuild nodes killed."
