# Source Base

A .NET 10 API starter built on **Vertical Slice Architecture** and **REPR Pattern** — each feature lives in a single file, from the HTTP endpoint down to the database call. No layers, no abstractions for their own sake.

## Architecture

One project. One folder per concern.

```
SourceBase.Api/
├── Features/          # One file per use case (endpoint + handler + request/response records)
│   ├── Auth/          # Login, Register, ForgotPassword, ...
│   ├── Todo/          # CreateTodo, GetTodos, UpdateTodo, ...
│   └── Data/          # GetAudits, GetRoles
├── Entities/          # Pure POCO domain entities (BaseEntity, TodoItemEntity, ...)
├── Infrastructure/    # DbContext, Identity, migrations, email helper
├── Shared/            # Exceptions, AppSettings, interfaces, constants
├── Middlewares/       # Error response middleware
└── Program.cs         # Composition root — all DI wiring lives here
```

### Key Design Decisions

- **Minimal API slices via `IEndpoint`**: Every feature implements `IEndpoint` and registers its own route in `MapEndpoint`. No controllers.
- **Single-file features**: Endpoint, request/response records, and handler are colocated in one `.cs` file per use case.
- **Direct DI injection**: Route handlers receive `IRequestHandler<TRequest, TResponse>` plus normal dependencies (`IDbContext`, `ICurrentUser`, etc.) — no service locator, no ISender.
- **Middleware-based error handling**: `ErrorResponseMiddleware` catches `ApiException` subclasses and maps them to `ProblemDetails` responses.
- **Identity on `UserEntity`**: ASP.NET Core Identity functionality is wired to `UserEntity` directly — no separate `ApplicationUser` projection.
- **Startup is flat**: All service registration is in `Program.cs` via thin extension methods. No per-layer DI modules.

### Feature Structure

Each slice is a self-contained file follow the REPR Pattern:

```csharp

public record CreateTodoRequest(DateOnly Date, string Title, TodoItemStatus Status);

public record CreateTodoResponse(bool Success);

public class CreateTodoEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost("/todos", ([FromBody] CreateTodoRequest request, CreateTodoHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Todos");
}

public class CreateTodoHandler(IDbContext dbContext) : IRequestHandler<CreateTodoRequest, CreateTodoResponse>
{
    public async Task<CreateTodoResponse> Handle(CreateTodoRequest request, CancellationToken ct)
    {
        dbContext.TodoItems.Add(new TodoItemEntity { ... });
        await dbContext.SaveChangesAsync(ct);
        return new CreateTodoResponse(true);
    }
}

public class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        RuleFor(x => x.Date).NotNull();
        RuleFor(x => x.Title).NotEmpty();
    }
}

```

### Update Requests Convention

For update operations, the `Id` is passed as a route parameter and marked with `[property: SwaggerIgnore]` in the request record to exclude it from the OpenAPI schema:

```csharp
public record UpdateTodoRequest([property: SwaggerIgnore] Guid Id, DateOnly Date, string Title, TodoItemStatus Status);
```

So that the Id is required but not duplicated and being shown in the request body.

### Endpoint Formatting

Keep `MapEndpoint` chains aligned like this:

```csharp
public void MapEndpoint(IEndpointRouteBuilder app) => app
    .MapGet("/roles", ([AsParameters] GetRolesRequest request, GetRolesHandler handler, CancellationToken ct) => handler.Handle(request, ct))
    .AllowAnonymous()
    .WithTags("Data");
```

### Error Handling

Throw a typed exception; the middleware handles the rest:

| Exception               | Status |
| ----------------------- | ------ |
| `BadRequestException`   | 400    |
| `ValidationException`   | 400    |
| `UnAuthorizedException` | 401    |
| `ForbiddenException`    | 403    |
| `NotFoundException`     | 404    |
| `ApiInternalException`  | 500    |

### Entities

All entities inherit `BaseEntity` (`Id: Guid`, `CreatedOn`, `CreatedBy`, `UpdatedOn`, `UpdatedBy`). Audit fields are filled automatically by `ApplicationDbContextAuditInterceptor` — do not set them manually. Enums are stored as strings via `EnumToStringConverter<T>`.

## Features

✅ Vertical Slice Architecture — one file per use case, zero shared layers

✅ Minimal API endpoints registered via `IEndpoint` and auto-discovered by assembly scanning

✅ .NET 10 + Entity Framework Core (SQLite)

✅ Role-based authorization

✅ `GlobalExceptionMiddleware` → `ProblemDetails` error responses

✅ Request validation via `FluentValidation` with automatic 400 responses

✅ EF Core audit interceptor (CreatedOn / UpdatedOn / CreatedBy / UpdatedBy)

✅ Strongly-typed `AppSettings` with `IOptions<T>` pattern

✅ Serilog structured logging with OpenTelemetry sink for Aspire dashboard

✅ CORS policy (configurable origins via `appsettings.json`)

✅ SendGrid email integration

✅ .NET Aspire orchestration with dashboard for monitoring and logs

✅ Docker support

## Observability & Aspire

The project uses **.NET Aspire** for distributed application orchestration and observability:

- **`SourceBase.ServiceDefaults`** — Shared OpenTelemetry configuration (metrics, traces, health checks, service discovery)
- **`SourceBase.AppHost`** — Aspire orchestrator that manages the API and dashboard
- **Dashboard** — Real-time logs, traces, metrics, and resource monitoring at `http://localhost:15017`

Logs are forwarded to the dashboard via Serilog's OpenTelemetry sink when `OTEL_EXPORTER_OTLP_ENDPOINT` is set.

## Getting Started

### Run the API standalone

```sh
# Run the API
dotnet run --project SourceBase.Api

# Build solution
dotnet build

# Apply EF migrations
sh cmd-migration-add.sh <MigrationName>
sh cmd-migration-update-db.sh

# Run tests
dotnet test
```

### Run with Aspire orchestration (recommended for development)

```sh
# Start the AppHost (orchestrates API + dashboard)
dotnet run --project SourceBase.AppHost

# Dashboard available at: http://localhost:15017
# Login with token from console output
```

### Docker

```sh
docker compose up
```

The database (`app.db`) is auto-created on first run and seeded with roles and the admin user defined in `AppSettings`.

## Deploy to VPS (1 GB RAM)

The stack runs as three Docker containers — API, Blazor Web, and Nginx — all managed by `docker compose`.

### Architecture

```
Browser → Nginx :80 → sourcebase-web :8080 → sourcebase-api :8080 → SQLite (volume)
```

The Blazor Web app is server-rendered, so all API calls happen inside the Docker network — the browser never calls the API directly.

### 1. Prepare the VPS (Ubuntu 22.04, one-time)

```sh
# Install Docker
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
newgrp docker

# Allow HTTP + SSH
sudo ufw allow 22 && sudo ufw allow 80 && sudo ufw enable
```

### 2. Deploy

```sh
# Clone the repo on the VPS
git clone https://github.com/your-org/sourcebase.git && cd sourcebase

# Fill in secrets
cp .env.example .env
nano .env   # set ADMIN_PASSWORD, WEB_URL, SENDGRID_API_KEY

# Build and start
docker compose up --build -d

# Verify
docker compose ps
docker compose logs -f
```

Open `http://YOUR_VPS_IP` in your browser.

### 3. Update

```sh
git pull
docker compose up --build -d
```

### Environment variables (`.env`)

| Variable | Description |
|---|---|
| `ADMIN_EMAIL` | Seed admin email |
| `ADMIN_PASSWORD` | Seed admin password |
| `WEB_URL` | Public URL, e.g. `http://1.2.3.4` — used for email links and CORS |
| `SENDGRID_API_KEY` | Leave blank to disable outbound email |
| `SENDGRID_ACCOUNT_OWNER` | Sender email address |

The SQLite database is persisted in a Docker named volume (`sqlite_data`) and survives container restarts and rebuilds.
