# Source Base
I've poured my heart and soul into this project, weaving in all the valuable lessons I've learned over the years.


## Architecture

This codebase is in active migration from layered Clean Architecture toward a simpler vertical-slice structure.

Current runtime shape:

```
API (feature slices + startup, active build project)
```

| Project | Responsibility |
|-------|---------------|
| **API** | Feature-local controllers and MediatR handlers, DI composition root, and the active build/runtime project |
| **Domain code** | Pure POCO entities now compiled from `SourceBase.Api/Domain/Entities` |
| **Infrastructure code** | DbContext, Identity, helpers, and common types now compiled from `SourceBase.Api/Infrastructure` |

### Key Design Decisions

- **Domain decoupled from Identity**: `UserEntity`/`RoleEntity` are pure POCOs. Infrastructure uses `ApplicationUser : IdentityUser<Guid>` for Identity, with projections to map to domain entities.
- **CQRS with MediatR**: Feature handlers now live alongside their API slices and are dispatched via `ISender`.
- **Direct startup composition**: Service registration now happens in `SourceBase.Api/Program.cs` instead of per-layer dependency injection extension methods.
- **Current migration status**: Todo, Data, and Auth slices live under `SourceBase.Api/Features`, and shared domain/infrastructure code now also builds from `SourceBase.Api`.


## Features

✅ Feature-first API slices for Todo, Auth, and Data

✅ CQRS pattern with MediatR (Commands, Queries, Handlers)

✅ Entity Framework and .NET 10

✅ Exception filter, Model binding validation, Audit log interceptors

✅ Customized EF Identity authentication with `ApplicationUser` mapping and role-based authorization

✅ Docker support

✅ Email service with SendGrid provider for OTP confirmation, email confirmation, forgot password, reset password

✅ Singleton AppSettings with IOptions pattern

✅ Direct startup registration in the API host

✅ Logging mechanism with Serilog

✅ CORS policy


## Migrations

EF Core migrations are intentionally out of scope for the current migration step and can be reintroduced later.