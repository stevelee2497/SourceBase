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
- Avoid calling WithDbContextAsync directly in tests if possible; instead call appropriate apis to get the response data you need. Only use WithDbContextAsync for assertions that require checking the DB entity field that are not exposed via the API response.

**Email testing** — `SendGridEmailHelper` always persists every outbound email to `db.Emails` (`EmailEntity`: `To`, `Subject`, `Body`, `SentOn`) before dispatching via SendGrid. In tests (no API key configured) the email is only saved to the DB. Retrieve codes via `GetLatestEmailCodeAsync` instead of generating tokens directly from `UserManager`.

**Test structure** — every test follows the AAA pattern:

```csharp
[Fact(DisplayName = "TODOS-CREATE-006: CreateTodo_WithValidTodoListId_ReturnsOk")]
public async Task CreateTodo_WithValidTodoListId_ReturnsOk()
{
    // Arrange
    var client = await factory.CreateAuthorizedClient();
    var listResponse = await client.PostAsJsonAsync("todo-lists", new { name = $"List_{Guid.NewGuid():N}" });
    var list = await listResponse.Content.ReadFromJsonAsync<CreateTodoListResponse>();

    // Act
    var response = await client.PostAsJsonAsync(CreateTodoEndpoint.Route, new
    {
        date = "2025-06-01",
        title = "Todo in list",
        status = "Open",
        todoListId = list!.Id,
    });

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var body = await response.Content.ReadFromJsonAsync<CreateTodoResponse>();
    var todo = await factory.WithDbContextAsync(db => db.TodoItems.SingleAsync(x => x.Id == body!.Id));
    todo!.TodoListId.Should().Be(list.Id);
}
```

**Test case id convention** - `{FEATURE}-{ACTION}-{NNN}` (e.g. `TODOS-CREATE-001`), included in the `DisplayName` of the test for easy identification and traceability to requirements.

**Test isolation and transparency** - All payload data defined in the test body for clarity; no external helper methods that abstract away the intent of the test.

**Test naming** — `MethodName_WithCondition_ReturnsExpected` (e.g. `Login_WithWrongPassword_ReturnsUnauthorized`).

**Feature isolation** — tests for different features (Auth, Todos, etc.) organized into separate test classes and files. Each test class should only focus on one endpoint.

## Blazor

**Event handlers** — never use inline lambdas (`() => Method(param)`) for event handlers or component parameters. Use named delegates instead:

- `void` method with loop-captured parameter → return `Action`:
  ```csharp
  private Action OpenEdit(T item) => () => { _editing = item; _showForm = true; };
  // usage: @onclick="OpenEdit(item)"
  ```
- `async Task` method with loop-captured parameter → return `Func<Task>`:
  ```csharp
  private Func<Task> SelectItem(Guid id) => async () => { _selectedId = id; await LoadAsync(); };
  // usage: @onclick="SelectItem(item.Id)"
  ```
- Multi-statement or single-expression inline lambdas on component parameters (`OnClose`, `OnCancel`, `OnSaved`, etc.) → extract to named `void` methods:
  ```csharp
  private void CancelDelete() { _showDelete = false; _deleting = null; }
  // usage: OnCancel="CancelDelete"
  ```
