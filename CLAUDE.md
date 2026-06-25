# CLAUDE.md

The final goal of this project is to learn fancy design patterns and architecture, and apply them to a real-world project.

## Architecture

Clean Architecture with Vertical Slice features. Four projects with strict dependency direction: `Api` → `Application` ← `Infrastructure`, all referencing `Domain`.

```
SourceBase.Domain/          # Pure POCO entities (BaseAuditableEntity, ...)
SourceBase.Application/     # Features (use cases), interfaces, shared logic
SourceBase.Infrastructure/  # EF Core, implementations, migrations (PostgreSQL)
SourceBase.Api/             # HTTP entry point — wires AddApplication() + AddInfrastructure()
SourceBase.Web/             # Blazor WebAssembly SPA (Tailwind CSS v4, mobile-responsive)
SourceBase.Tests/           # Integration tests with xUnit + FluentAssertions + `WebApplicationFactory`
```

## Key rules & Code conventions

- Record/handler/constructor parameters on one line; avoid multi-line parameter lists.
- Use primary constructors for handlers and services to keep code concise and avoid boilerplate.
- Avoid nested `if` statements; use early returns and guard clauses instead. Keep every method short and focused on a single responsibility.
- **Config** — add new settings to `AppSettings.cs` and `appsettings.json`. Injected as singleton `AppSettings`.
- env variables in docker-compose.yml need to be defined in github vars and secrets pipeline docker-publish.yml for deployment to work.
- Avoid magic strings; use constants or enums or define value in AppSettings instead
- Use one line for everything if possible.
- Use helper `.Serialize()` and `.Deserialize<T>()` extension methods for JSON serialization/deserialization for centralized json configuration (e.g. camelCase, ignore nulls, enum, etc.).

## Skills

- `/be` — backend: Clean Architecture, vertical slice features, API endpoints, handlers, entities, validators, conventions
- `/fe` — frontend: Blazor components, Tailwind CSS v4, mobile-responsive layout (Mobile S 320px / Mobile M 375px)
- `/test` — test infrastructure and patterns
- `/pr-reviewer` — PR review checklist

## Skill auto-routing

Automatically invoke the matching skill based on what you're working on — no need to be asked:

- Touching `SourceBase.Domain`, `SourceBase.Application`, `SourceBase.Infrastructure`, or `SourceBase.Api` (entities, handlers, endpoints, validators, migrations, config) → use `/be`.
- Touching `SourceBase.Web` (Blazor components, Tailwind, layout) → use `/fe`.
- Touching `SourceBase.Tests` (integration tests, `WebApplicationFactory`, fixtures) → use `/test`.

If a change spans multiple projects, load each relevant skill (e.g. a feature with API + Blazor UI uses `/be` and `/fe`). Invoke the skill before writing code, not after.

## Graphify

This project has a knowledge graph at graphify-out/ with god nodes, community structure, and cross-file relationships.

Rules:

- For codebase questions, first run `graphify query "<question>"` when graphify-out/graph.json exists. Use `graphify path "<A>" "<B>"` for relationships and `graphify explain "<concept>"` for focused concepts. These return a scoped subgraph, usually much smaller than GRAPH_REPORT.md or raw grep output.

## Git workflow

- Create branch from main with descriptive name (e.g. `feature/todo-crud`), and commit early and often with descriptive messages. Don't wait to have a "complete" feature before committing.
- At least 1 commits per 1 phase of the workflow (specs, be implementation, fe implementation, docs update). Push to remote and open PR when ready for review.
