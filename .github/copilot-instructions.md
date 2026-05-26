# Copilot Instructions

## Commands

```sh
dotnet run --project SourceBase.Api
dotnet build
sh cmd-migration-add.sh <Name> && sh cmd-migration-update-db.sh
docker compose up
```

## Architecture

Single project VSA. `SourceBase.Api/Features/` — one file per use case (endpoint + handler + request/response records). No MediatR, no controllers.

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

- All DI wiring in `Program.cs` and defined in Program.Configuration.cs via thin extension methods
- `IEndpoint` implementations auto-discovered via assembly scanning
- Endpoints mounted under `/api` with `RequireAuthorization()`
- Keep `MapEndpoint` chains aligned on separate lines: `.MapXxx(...)`, `.AllowAnonymous()` / `.RequireAuthorization()`, then `.WithTags(...)`.
- All Record/Handler/Constructors parameters should be in 1 lines, avoid multiple lines of parameters
- For update operations, the `Id` is passed as a route parameter and marked with `[property: SwaggerIgnore]` in the request record to exclude it from the OpenAPI schema

## Conventions

**Errors** — throw typed exceptions; `GlobalExceptionMiddleware` maps to `ProblemDetails`:

- `NotFoundException` → 404 · `UnAuthorizedException` → 401 · `ForbiddenException` → 403 · `ValidationException` → 400 · `ApiInternalException` → 500

**Entities** — inherit `BaseEntity` (`Id`, `CreatedOn/By`, `UpdatedOn/By`). Audit fields set by interceptor — never set manually. Enums stored as strings.

**Config** — add to `AppSettings.cs` + `appsettings.json`. Available as `IOptions<AppSettings>` and singleton.

**DB** — SQLite `app.db`, auto-created and seeded on startup.

## Testing

Integration tests live in `SourceBase.Tests/` using `xUnit` + `FluentAssertions` + `WebApplicationFactory`.

**Infrastructure**

- `WebAppFactory` — spins up the full app with an isolated in-memory SQLite database per test run. Seeded with an admin user (`AdminEmail` / `AdminPassword`).
- `Utilities` — extension methods: `PostAsJsonAsync`, `PutAsJsonAsync`, `ReadFromJsonAsync` (with enum-string JSON options), `AuthorizeAsync`, `GetAccessTokenAsync`.

**Helpers on `WebAppFactory`**

- `CreateAuthorizedClient()` — creates an `HttpClient` pre-authorized as the seeded admin.
- `GetLatestEmailCodeAsync(email)` — queries `db.Emails` for the most recent email sent to that address and extracts the `code` query param from the link in the body. Use this after any endpoint that sends an email (register, forgotPassword).
- `ConfirmEmailAsync(client, email)` — convenience wrapper: reads the code from the DB and POSTs to `/api/auth/confirmEmail`.

**Email testing** — `SendGridEmailHelper` always persists every outbound email to `db.Emails` (`EmailEntity`: `To`, `Subject`, `Body`, `SentOn`) before dispatching via SendGrid. In tests (no API key configured) the email is only saved to the DB. Retrieve codes via `GetLatestEmailCodeAsync` instead of generating tokens directly from `UserManager`.

**Test structure** — every test follows the AAA pattern:

```csharp
[Fact]
public async Task DoSomething_WithCondition_ReturnsExpected()
{
    // Arrange
    var client = factory.CreateClient();
    // ... setup

    // Act
    var response = await client.PostAsJsonAsync("/api/...", new { ... });

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await response.Content.ReadFromJsonAsync<MyResponse>();
    body!.Field.Should().Be(expected);
}
```

**Test naming** — `MethodName_WithCondition_ReturnsExpected` (e.g. `Login_WithWrongPassword_ReturnsUnauthorized`).

**Test isolation** — use `Guid.NewGuid()` to generate unique emails per test (e.g. `$"user_{Guid.NewGuid():N}@test.com"`) to avoid state conflicts between tests sharing the same `WebAppFactory` instance.
