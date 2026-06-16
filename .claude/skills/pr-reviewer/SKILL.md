---
name: pr-reviewer
description: "PR review checklist for SourceBase. Use when reviewing pull requests to verify architecture, conventions, tests, and code quality."
trigger: /pr-reviewer
---

# /pr-reviewer

Review pull requests against SourceBase's architecture and conventions.

## Invoke the built-in review skill

Start with the global `/review` skill for diff analysis, then apply the checklist below for project-specific concerns.

## Checklist

### Architecture

- [ ] Feature lives in `SourceBase.Application/Features/` as a single file (request, response, endpoint, handler, validator)
- [ ] No MediatR, no controllers — uses `IEndpoint` / `IRequestHandler<TRequest, TResponse>`
- [ ] Dependency direction respected: `Api` → `Application` ← `Infrastructure` ← `Domain`
- [ ] New interfaces added to `SourceBase.Application/Shared/Interfaces/`; implementations in `SourceBase.Infrastructure/Implementations/`
- [ ] New settings added to both `AppSettings.cs` and `appsettings.json`

### Endpoints & handlers

- [ ] `MapEndpoint` chain uses separate lines (`.MapXxx`, then auth, then `.WithTags`)
- [ ] Update endpoints use `PATCH` (not `PUT`) with partial semantics — `MapPatch`, not `MapPut`
- [ ] Update endpoints: `Id` is a route param marked `[property: SwaggerIgnore]` on the request record
- [ ] No multi-line parameter lists on records, handlers, or constructors
- [ ] Auth: default `RequireAuthorization()` present, or `.AllowAnonymous()` intentional
- [ ] Pagination uses `PagingRequest` base + feature-specific `OrderBy` enum + `.PaginateAsync()`

### Entities & data

- [ ] Entity inherits `BaseAuditableEntity`; audit fields (`CreatedOn/By`, `UpdatedOn/By`) never set manually
- [ ] Enums stored via `EnumToStringConverter`
- [ ] Errors thrown as typed exceptions (`NotFoundException`, `BadRequestException`, etc.) — no raw status codes

### Partial updates

- [ ] Update endpoints use `PATCH` (not `PUT`)
- [ ] All update request fields are nullable — only non-null fields are applied
- [ ] Partial update handlers use `entity.Field = request.Field ?? entity.Field` (null-coalescing), not if-guards
- [ ] Validator rules guarded with `.When(x => x.Field is not null)`
- [ ] DB-level validation (existence/ownership) is in the validator via `MustAsync`, not in the handler
- [ ] Single-field updates extend existing endpoints rather than adding new dedicated endpoints

### Tests

- [ ] At least one integration test per new/changed endpoint in `SourceBase.Tests/`
- [ ] Test class mirrors `Features/` path
- [ ] Test IDs follow `{FEATURE}-{ACTION}-{NNN}` format in `DisplayName`
- [ ] Method name follows `MethodName_WithCondition_ReturnsExpected`
- [ ] Uses `CreateTodoEndpoint.Route` (strong-typed) — no hardcoded URL strings
- [ ] Email codes retrieved via `GetLatestEmailCodeAsync` — no manually generated tokens
- [ ] `WithDbContextAsync` only used when asserting on a DB field not returned by the API

### Blazor (if applicable)

- [ ] No inline lambdas on event handlers or component parameters
- [ ] Loop-captured params use `Action`-returning or `Func<Task>`-returning methods
- [ ] Multi-statement handlers extracted to named methods
