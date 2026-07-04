# Use .dockerignore to exclude unnecessary files for smaller context and faster build
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Copy only csproj files and restore as distinct layers for better build caching
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Packages.props .  
COPY Directory.Build.props .
COPY SourceBase.Api/SourceBase.Api.csproj SourceBase.Api/
RUN dotnet restore SourceBase.Api/SourceBase.Api.csproj

# Copy the rest of the source code
COPY . .

# Build and publish in a single step to reduce layers and intermediate output
RUN dotnet publish SourceBase.Api/SourceBase.Api.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
# test-report.html is produced by CI before `docker build` runs (see api-publish.yml);
# a placeholder is checked into the repo root so local `docker build .` still works.
COPY test-report.html ./wwwroot/test-report.html
ENTRYPOINT ["dotnet", "SourceBase.Api.dll"]