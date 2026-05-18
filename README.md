# Source Base

A .NET 10 API starter built on **Vertical Slice Architecture** — each feature lives in a single file, from the HTTP endpoint down to the database call. No layers, no abstractions for their own sake.

## Architecture

One project. One folder per concern.

```
SourceBase.Api/
├── Features/          # One file per use case (endpoint + handler + request record)
│   ├── Auth/          # Login, Register, ForgotPassword, ...
│   ├── Todo/          # CreateTodo, GetTodos, UpdateTodo, ...
│   └── Data/          # GetAudits, GetRoles
├── Entities/          # Pure POCO domain entities (BaseEntity, TodoItemEntity, ...)
├── Infrastructure/    # DbContext, Identity, migrations, email helper
├── Shared/            # Exceptions, AppSettings, interfaces, constants
├── Middlewares/       # Error response middleware
├── Extensions/        # IServiceCollection + IEndpointRouteBuilder extensions
└── Program.cs         # Composition root — all DI wiring lives here
```

### Key Design Decisions

- **Minimal API slices via `IEndpoint`**: Every feature implements `IEndpoint` and registers its own route in `MapEndpoint`. No controllers.
- **Single-file features**: Request record, response record, and handler are colocated in one `.cs` file per use case.
- **Direct DI injection**: Handler methods receive dependencies (`IDbContext`, `ICurrentUser`, etc.) as parameters — no service locator, no MediatR.
- **Middleware-based error handling**: `ErrorResponseMiddleware` catches `ApiException` subclasses and maps them to `ProblemDetails` responses.
- **Identity on `UserEntity`**: ASP.NET Core Identity is wired to `UserEntity : IdentityUser<Guid>` directly — no separate `ApplicationUser` projection.
- **Startup is flat**: All service registration is in `Program.cs` via thin extension methods. No per-layer DI modules.

### Feature Structure

Each slice is a self-contained file:

```csharp
public class CreateTodo : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
        => app.MapPost("/todos", Handler).WithTags("Todos");

    private async Task<NoContent> Handler(
        [FromBody] CreateTodoRequest request,
        IDbContext dbContext,
        ICurrentUser currentUser,
        CancellationToken ct)
    {
        dbContext.TodoItems.Add(new TodoItemEntity { ... });
        await dbContext.SaveChangesAsync(ct);
        return TypedResults.NoContent();
    }
}

public record CreateTodoRequest([Required] DateOnly? Date, [Required] string Title, TodoItemStatus Status);
```

### Error Handling

Throw a typed exception; the middleware handles the rest:

| Exception | Status |
|---|---|
| `NotFoundException` | 500 |
| `UnAuthorizedException` | 401 |
| `ForbiddenException` | 403 |
| `ValidationException` | 400 |
| `ApiInternalException` | 500 |

### Entities

All entities inherit `BaseEntity` (`Id: Guid`, `CreatedOn`, `CreatedBy`, `UpdatedOn`, `UpdatedBy`). Audit fields are filled automatically by `ApplicationDbContextAuditInterceptor` — do not set them manually. Enums are stored as strings via `EnumToStringConverter<T>`.


## Features

✅ Vertical Slice Architecture — one file per use case, zero shared layers

✅ Minimal API endpoints registered via `IEndpoint` and auto-discovered by assembly scanning

✅ .NET 10 + Entity Framework Core (SQLite)

✅ ASP.NET Core Identity with bearer token auth (`AddIdentityApiEndpoints`)

✅ Role-based authorization

✅ `ErrorResponseMiddleware` → `ProblemDetails` error responses

✅ Request validation via `DataAnnotations` with automatic 400 responses

✅ EF Core audit interceptor (CreatedOn / UpdatedOn / CreatedBy / UpdatedBy)

✅ Strongly-typed `AppSettings` with `IOptions<T>` pattern

✅ Serilog structured logging

✅ CORS policy (configurable origins via `appsettings.json`)

✅ SendGrid email integration

✅ Docker support


## Getting Started

```sh
# Run the API
dotnet run --project SourceBase.Api

# Build solution
dotnet build

# Apply EF migrations
sh migration-add.sh <MigrationName>
sh migration-update-db.sh

# Docker
docker compose up
```

The database (`app.db`) is auto-created on first run and seeded with roles and the admin user defined in `AppSettings`.

