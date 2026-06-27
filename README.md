# Source Base

The final goal of this project is to learn fancy design patterns and architecture, and apply them to a real-world project.

A .NET 10 API starter built on **Vertical Slice Architecture** and **REPR Pattern** — each feature lives in a single file, from the HTTP endpoint down to the database call. No layers, no abstractions for their own sake.

## Architecture

Clean Architecture with Vertical Slice features. Four separate projects with strict dependency direction: `Api` → `Application` ← `Infrastructure`, all referencing `Domain`.

```
SourceBase.Domain/
└── Entities/          # Pure POCO domain entities (BaseAuditableEntity, TodoItemEntity, ...)

SourceBase.Application/
├── Features/          # One file per use case (endpoint + handler + request/response + validator)
│   ├── Auth/          # Login, Register, ForgotPassword, ...
│   ├── Todo/          # CreateTodo, GetTodos, UpdateTodo, ...
│   └── ...
└── Shared/            # Interfaces, exceptions, AppSettings, constants

SourceBase.Infrastructure/
├── DbContexts/        # EF Core DbContext + audit/logging interceptors
├── Implementations/   # IEmailHelper, ICurrentUser, ISecurityProvider, IStorageService, ...
├── Hubs/              # SignalR hubs
└── Migrations/        # EF Core migrations (PostgreSQL)

SourceBase.Api/
├── Middlewares/       # GlobalExceptionMiddleware, ValidationEndpointFilter
└── Program.cs         # Entry point — wires AddApplication() + AddInfrastructure()
```

### Key Design Decisions

- **Minimal API slices via `IEndpoint`**: Every feature implements `IEndpoint` and registers its own route. No controllers. Features live in `SourceBase.Application`.
- **Single-file features**: Endpoint, request/response records, handler, and validator are colocated in one `.cs` file per use case.
- **Direct DI injection**: Route handlers receive concrete handler types plus dependencies (`IDbContext`, `ICurrentUser`, etc.) — no service locator, no ISender.
- **Layered DI**: `AddApplication()` auto-discovers endpoints, handlers, and validators. `AddInfrastructure()` registers EF Core, auth, and service implementations.
- **Middleware-based error handling**: `GlobalExceptionMiddleware` catches typed exceptions and maps them to `{ TraceId, Code, Message, Errors }` JSON responses.
- **Identity on `UserEntity`**: ASP.NET Core BearerToken auth wired directly to `UserEntity` — no separate `ApplicationUser`.
- **PATCH with partial updates**: All update endpoints use `PATCH`. Fields are nullable and only applied when non-null (null-coalescing). DB-level validation lives in the validator via `MustAsync`.

### Feature Structure

Each slice is a self-contained file following the REPR Pattern (in `SourceBase.Application/Features/`):

```csharp
public record CreateTodoRequest(DateOnly Date, string Title, TodoItemStatus Status);

public record CreateTodoResponse(Guid Id);

public class CreateTodoEndpoint : IEndpoint
{
    public const string Route = "todos";

    public void MapEndpoint(IEndpointRouteBuilder app) => app
        .MapPost(Route, ([FromBody] CreateTodoRequest request, CreateTodoHandler handler, CancellationToken ct) => handler.Handle(request, ct))
        .WithTags("Todos");
}

public class CreateTodoHandler(IDbContext dbContext) : IRequestHandler<CreateTodoRequest, CreateTodoResponse>
{
    public async Task<CreateTodoResponse> Handle(CreateTodoRequest request, CancellationToken ct)
    {
        var entity = new TodoItemEntity { Title = request.Title };
        dbContext.TodoItems.Add(entity);
        await dbContext.SaveChangesAsync(ct);
        return new CreateTodoResponse(entity.Id);
    }
}

public class CreateTodoRequestValidator : AbstractValidator<CreateTodoRequest>
{
    public CreateTodoRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
    }
}
```

### Update Requests Convention

Update endpoints use **`PATCH`** with partial update semantics. All request fields are nullable; only provided (non-null) fields are applied. The handler uses null-coalescing so omitted fields are never overwritten, and validator rules are guarded with `.When`:

```csharp
public record UpdateTodoRequest([property: SwaggerIgnore][property: FromRoute] Guid Id, DateOnly? Date, string? Title, TodoItemStatus? Status);
```

```csharp
// Handler — null-coalescing keeps existing values for omitted fields
item.Title = request.Title ?? item.Title;
item.Status = request.Status ?? item.Status;
item.Date = request.Date ?? item.Date;
```

```csharp
// Validator — rules only fire when the field is actually provided
RuleFor(x => x.Title).NotEmpty().When(x => x.Title is not null);
```

> **Note:** Sending `null` for a field means "keep existing value" — it does not clear the field.

### Endpoint Formatting

Keep `MapEndpoint` chains aligned like this:

```csharp
public void MapEndpoint(IEndpointRouteBuilder app) => app
    .MapGet(Route, ([AsParameters] GetRolesRequest request, GetRolesHandler handler, CancellationToken ct) => handler.Handle(request, ct))
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

All entities inherit `BaseAuditableEntity` (`Id: Guid`, `CreatedOn`, `CreatedBy`, `UpdatedOn`, `UpdatedBy`). Audit fields are filled automatically by `ApplicationDbContextAuditInterceptor` — do not set them manually. Enums are stored as strings via `EnumToStringConverter<T>`.

## Features

| Capability                     | Notes                                                               |
| ------------------------------ | ------------------------------------------------------------------- |
| Clean Architecture + VSA       | Domain / Application / Infrastructure / Api — one file per use case |
| Minimal API + `IEndpoint`      | Auto-discovered by assembly scanning — no controllers               |
| .NET 10 + EF Core (PostgreSQL) | Managed via `SourceBase.Infrastructure` migrations                  |
| Role-based authorization       | JWT-based with per-endpoint `RequireAuthorization`                  |
| `GlobalExceptionMiddleware`    | Typed exceptions → `ProblemDetails` responses                       |
| FluentValidation               | Auto-wired — invalid requests return `400` before reaching handlers |
| EF Core audit interceptor      | `CreatedOn / UpdatedOn / CreatedBy / UpdatedBy` set automatically   |
| Strongly-typed `AppSettings`   | `IOptions<T>` pattern, injected as singleton                        |
| Serilog + OpenTelemetry        | Structured logging to console and file                              |
| CORS                           | Configurable origins via `appsettings.json`                         |
| Docker support                 | Multi-stage images, `docker compose up` for local dev               |

## Getting Started

### Run the API

```sh
# Run the API
dotnet run --project SourceBase.Api

# Run API + Web
sh run.sh

# Build solution
dotnet build

# Apply EF migrations
sh cmd-migration-add.sh <MigrationName>
sh cmd-migration-update-db.sh

# Run tests
dotnet test
```

### Docker

```sh
docker compose up
```

## Deploy to VPS (1 GB RAM)

Images are built by GitHub Actions and pushed to GitHub Container Registry (GHCR) on every merge to `main`. The VPS only needs to pull and run — no compiler, no Node.js, no build tools required.

### Architecture

```
Browser → Nginx :80 → sourcebase-web :8080 → sourcebase-api :8080 → SQLite (volume)
```

The Blazor Web app is web wasm, hosting on static pages of cloudflare pages, and the API is hosted on a separate container. Nginx acts as a reverse proxy to route requests to the appropriate service.

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
