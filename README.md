# Source Base
I've poured my heart and soul into this project, weaving in all the valuable lessons I've learned over the years.


## Architecture

Clean Architecture with CQRS/MediatR pattern. Dependencies flow inward — outer layers depend on inner layers, never the reverse.

```
API (ASP.NET Core) → Infrastructure → Application → Domain
```

| Layer | Responsibility |
|-------|---------------|
| **Domain** | Pure POCO entities, no framework dependencies |
| **Application** | Use cases (Commands/Queries/Handlers via MediatR), abstractions (`IDbContext`, `IIdentityService`, `IEmailHelper`, `IUserContext`) |
| **Infrastructure** | EF Core DbContext, ASP.NET Identity (`ApplicationUser`/`ApplicationRole`), email service, Identity service implementations |
| **API** | Controllers, filters, middleware, DI composition root |

### Key Design Decisions

- **Domain decoupled from Identity**: `UserEntity`/`RoleEntity` are pure POCOs. Infrastructure uses `ApplicationUser : IdentityUser<Guid>` for Identity, with projections to map to domain entities.
- **CQRS with MediatR**: All features use Commands/Queries dispatched via `ISender`. Handlers live in Application (business logic) or Infrastructure (Identity operations).
- **`IDbContext` with `DbSet<T>`**: Application references `Microsoft.EntityFrameworkCore` for `DbSet<T>` on business entities (`TodoItems`, `AuditHistories`). User/Role access uses `IQueryable<T>` projections.
- **Explicit DI registration**: Each layer provides its own `DependencyInjection.cs` extension method (`AddApplication()`, `AddInfrastructure()`).


## Features

✅ Clean architecture with layered design: API, Application, Domain, Infrastructure layers

✅ CQRS pattern with MediatR (Commands, Queries, Handlers)

✅ Entity Framework and .NET 8

✅ Exception filter, Model binding validation, Audit log interceptors

✅ Customized EF Identity authentication with `ApplicationUser` mapping and role-based authorization

✅ Docker support

✅ Email service with SendGrid provider for OTP confirmation, email confirmation, forgot password, reset password

✅ Singleton AppSettings with IOptions pattern

✅ Explicit per-layer dependency injection registration

✅ Logging mechanism with Serilog

✅ CORS policy