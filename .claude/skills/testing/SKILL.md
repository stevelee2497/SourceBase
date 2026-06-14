---
name: testing
description: "Integration test patterns and infrastructure for SourceBase. Use when writing, reviewing, or fixing tests."
trigger: /testing
---

# /testing

Integration test conventions for SourceBase using xUnit + FluentAssertions + `WebApplicationFactory`.

Test classes live in `SourceBase.Tests/` and mirror the `Features/` structure — one class per endpoint.

## Infrastructure

- `WebAppFactory` — full app with an isolated in-memory SQLite database per test run, seeded with an admin user (`AdminEmail` / `AdminPassword` from config).
- `CreateAuthorizedClient()` — returns an `HttpClient` pre-authorized as the seeded admin.
- `GetLatestEmailCodeAsync(email)` — queries `db.Emails` for the latest code sent to that address. Use after any endpoint that sends an email (register, forgot-password). Never generate tokens directly.
- `ConfirmEmailAsync(client, email)` — convenience wrapper: reads code from DB and POSTs to `/api/auth/confirmEmail`.
- Avoid `WithDbContextAsync` in tests unless asserting on a DB field not exposed by the API response.

## Test structure

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
    var todoResponse = await client.GetAsync(GetTodoEndpoint.Route.WithId(body.Id));
    var todo = await todoResponse.Content.ReadFromJsonAsync<GetTodoResponse>();
    todo!.CreatedBy.Should().Be(userInfo!.UserName);
    todo.UserId.Should().Be(userInfo.Id);
}
```

## Rules

- **Naming:** `MethodName_WithCondition_ReturnsExpected`
- **Test case IDs:** `{FEATURE}-{ACTION}-{NNN}` in `DisplayName` (e.g. `TODOS-CREATE-001`)
- All payload data defined inline — no helper methods that hide intent.
- All API calls must use strong-typed Route constants (e.g. `CreateTodoEndpoint.Route`) — never hardcoded strings.
