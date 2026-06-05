# Source Base

A .NET 10 API starter built on **Vertical Slice Architecture** and **REPR Pattern** — each feature lives in a single file, from the HTTP endpoint down to the database call. No layers, no abstractions for their own sake.

## Documentation

Feature documentation — user stories, detailed flows, and test case traceability:

| Domain                          | Description                                                                      |
| ------------------------------- | -------------------------------------------------------------------------------- |
| [Auth](docs/features/auth.md)   | Login, Register, Email Confirmation, Password Reset, Token Refresh, User Profile |
| [Users](docs/features/users.md) | Admin-managed user accounts — create, list, update, delete, password reset       |
| [Roles](docs/features/roles.md) | Role management — create, list, update, delete                                   |
| [Todos](docs/features/todos.md) | Todo lists and todo items — full CRUD, filters, paging                           |
| [Data](docs/features/data.md)   | Audit history, enum definitions, application statistics                          |

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

| Capability                         | Notes                                                               |
| ---------------------------------- | ------------------------------------------------------------------- |
| Vertical Slice Architecture        | One file per use case, zero shared layers                           |
| Minimal API + `IEndpoint`          | Auto-discovered by assembly scanning — no controllers               |
| .NET 10 + EF Core (SQLite)         | Lightweight, zero-config database                                   |
| Role-based authorization           | JWT-based with per-endpoint `RequireAuthorization`                  |
| `GlobalExceptionMiddleware`        | Typed exceptions → `ProblemDetails` responses                       |
| FluentValidation                   | Auto-wired — invalid requests return `400` before reaching handlers |
| EF Core audit interceptor          | `CreatedOn / UpdatedOn / CreatedBy / UpdatedBy` set automatically   |
| Strongly-typed `AppSettings`       | `IOptions<T>` pattern, injected as singleton                        |
| Serilog + OpenTelemetry            | Structured logging forwarded to Aspire dashboard                    |
| CORS                               | Configurable origins via `appsettings.json`                         |
| .NET Aspire orchestration          | Dashboard for logs, traces, metrics at `http://localhost:15017`     |
| Docker support                     | Multi-stage images, `docker compose up` for local dev               |
| Standalone Aspire dashboard on VPS | Logs, traces, metrics via Tailscale on port `18888`                 |
| sqlite-web on VPS                  | SQLite browser via Tailscale on port `18080`                        |

## Backlogs

🎯 Todo page: able to create multiple todo lists, manage todos

🎯 User page:

- Able to reset user pass, set user to a new random pass and sent the newly created password to user email.
- When admin reset pass, the new pass should show up for the 1st time before submit so admin can copy it.
- Able to set status email confirmed for user

🎯 Email service: set up free email provider to send email to user

🎯 Dash board: set up more friendly dash boards with informative widgets: number of users, sum of todo items, sum of done items

## Observability & Aspire

The project uses **.NET Aspire** for distributed application orchestration and observability:

- **`SourceBase.ServiceDefaults`** — Shared OpenTelemetry configuration (metrics, traces, health checks, service discovery)
- **`SourceBase.AppHost`** — Aspire orchestrator that manages the API and dashboard
- **Local dashboard** — Real-time logs, traces, metrics, and resource monitoring at `http://localhost:15017`
- **VPS dashboard** — Standalone Aspire dashboard container at `http://TAILSCALE_IP:18888` (accessible via Tailscale VPN)

Logs and traces are forwarded to the dashboard via OpenTelemetry when `OTEL_EXPORTER_OTLP_ENDPOINT` is set.

### VPS internal tools (Tailscale VPN only)

| Tool             | Port    | Purpose                 |
| ---------------- | ------- | ----------------------- |
| Aspire Dashboard | `18888` | Logs, traces, metrics   |
| sqlite-web       | `18080` | SQLite database browser |

These ports are **not** exposed via nginx — connect via Tailscale to access them.

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

Images are built by GitHub Actions and pushed to GitHub Container Registry (GHCR) on every merge to `main`. The VPS only needs to pull and run — no compiler, no Node.js, no build tools required.

### Architecture

```
Browser → Nginx :80 → sourcebase-web :8080 → sourcebase-api :8080 → SQLite (volume)
```

The Blazor Web app is server-rendered, so all API calls happen inside the Docker network — the browser never calls the API directly.

### CI/CD flow

```
git push → GitHub Actions: dotnet test → build images → push to GHCR → SSH into VPS → docker compose pull && up
```

### 1. Prepare the VPS (Ubuntu 22.04, one-time)

```sh
# Install Docker
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker $USER
newgrp docker

# Allow HTTP + SSH
sudo ufw allow 22 && sudo ufw allow 80 && sudo ufw enable
```

### 2. Configure GitHub Actions secrets (one-time)

The pipeline SSHes into the VPS to deploy automatically. Add these in **GitHub → repo Settings → Secrets and variables → Actions**:

| Secret        | Value                                                  |
| ------------- | ------------------------------------------------------ |
| `VPS_HOST`    | VPS IP address or hostname                             |
| `VPS_USER`    | SSH username (e.g. `ubuntu`)                           |
| `VPS_SSH_KEY` | Contents of your SSH private key (`cat ~/.ssh/id_rsa`) |

And optionally set a **variable** (not secret) `VPS_APP_PATH` to the absolute path where the repo is cloned on the VPS (defaults to `~/SourceBase`).

> **Tip — generate a dedicated deploy key:**
>
> ```sh
> # On your local machine
> ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/deploy_key -N ""
> # Add the public key to the VPS
> ssh-copy-id -i ~/.ssh/deploy_key.pub YOUR_USER@YOUR_VPS_IP
> # Paste the contents of ~/.ssh/deploy_key into the VPS_SSH_KEY secret
> ```

### 3. Authenticate with GHCR on the VPS (one-time, private repos only)

If the repository is **private**, create a GitHub Personal Access Token with the `read:packages` scope at <https://github.com/settings/tokens>, then log in:

```sh
echo YOUR_PAT | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

> Skip this step if the repository is public — GHCR images are public too.

### 4. Deploy (first time)

```sh
# Clone the repo on the VPS
git clone https://github.com/stevelee2497/SourceBase.git && cd SourceBase

# Pull pre-built images and start
docker compose pull
docker compose up -d

# Verify
docker compose ps
docker compose logs -f
```

Open `http://YOUR_VPS_IP` in your browser.

### 5. Update (automatic after this)

Every push to `main` triggers the pipeline which, on success, SSHes into the VPS and runs:

```sh
docker compose pull && docker compose up -d --remove-orphans
```

No manual action needed. To force a redeploy, push any commit to `main` or re-run the workflow from the GitHub Actions tab.
