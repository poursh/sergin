---
name: add-feature
description: Scaffold a new CQRS vertical slice (command or query) in a Sergin module — Application handler, Infrastructure repository wiring, Presentation endpoint, and DI/route registration — following the existing UserAccess module pattern. Invoke with /add-feature.
disable-model-invocation: true
---

Scaffold a new vertical-slice feature for: $ARGUMENTS

Expected input: `<ModuleName> <AggregateFolder> <FeatureName> <command|query>`, e.g. `/add-feature UserAccess Users DeactivateUser command`. Ask the user for whatever is missing before generating anything — don't guess the module, aggregate, or verb.

This repo has no scaffolding CLI; slices are hand-authored following a strict, repeated shape. Use `src/Modules/UserAccess/**/Users/**` as the reference implementation for every file below — read the matching file there before writing the new one, and match its style exactly (sealed records, primary constructors, `ErrorOr<T>` returns, no comments).

## Layout to create (module = e.g. `UserAccess`, aggregate = e.g. `Users`, feature = e.g. `DeactivateUser`)

**Command** (state-changing):
1. `src/Modules/<Module>/Sergin.<Module>.Application/<Aggregate>/Commands/<Feature>/<Feature>Command.cs` — `public sealed record <Feature>Command(...) : ICommand<<Feature>CommandResponse>;`
2. `.../<Feature>/<Feature>CommandResponse.cs` — `public sealed record <Feature>CommandResponse(...);`
3. `.../<Feature>/<Feature>CommandHandler.cs` — `internal sealed class` implementing `ICommandHandler<TCommand, TResponse>`, primary-constructor-injects `I<Module>UnitOfWork` + the domain repository, calls a domain factory/behavior method, calls `unitOfWork.SaveChangesAsync`, returns the response.
4. If the domain aggregate needs a new factory method or behavior (e.g. `User.Deactivate()`), add it to the aggregate class in `Sergin.<Module>.Domain`. Don't add public setters — mutate via methods on the aggregate.
5. Presentation: `src/Modules/<Module>/Sergin.<Module>.Presentation.WebApi/<Aggregate>/Endpoints/<Feature>/<Feature>Endpoint.cs` implementing `IEndpoint.MapEndpoint`, mapping the appropriate HTTP verb, binding a request model (add one alongside the endpoint if the command needs a body, e.g. `New<X>Model.cs`), sending via `ISender`, returning `res.ToApiResult()`.
6. Register the endpoint in the module's `<Aggregate>InstallationExtensions.Map<Aggregate>Endpoints` (e.g. `UserInstallationExtensions.MapUserEndpoints`) — instantiate and call `.MapEndpoint(routeBuilder)`.
7. If a new repository interface/dependency is needed, register it in the same file's `Add<Aggregate>Dependencies` (`services.AddTransient<IFoo, Foo>()`).

**Query** (read-side, bypasses EF):
Same shape but under `Commands/<Feature>/` still (this repo keeps queries in the `Commands` folder alongside commands — match that, don't invent a `Queries` folder), implementing `IQuery<TResponse>` / `IQueryHandler<TQuery, TResponse>` from `Sergin.SharedKernel.Application.Commands.Queries`. The handler depends on a dedicated `I<Feature>QueryRepository` interface (returns nullable response, handler maps null to `Error.NotFound()`). Implement that interface in `Sergin.<Module>.Infrastructure/<Aggregate>/Repositories/Queries/<Aggregate>QueryRepository.cs` using `IDbConnectionFactory` + raw SQL against the module's Postgres schema (see `UserQueryRepository.cs` for the `QuerySingleOrDefaultAsync` / `QueryMultipleAsync` Dapper-style pattern) — never use EF Core for reads. If the query needs authorization, add `[RequiredPermissions("permission.<schema>.<resource>.<action>")]` on the query record.

## After scaffolding

1. Check each new project's `GlobalUsings.cs` before adding `using` statements — many namespaces (`ErrorOr`, `Sergin.SharedKernel.*`) are already global.
2. If the feature needs new/changed columns, add or update the `IEntityTypeConfiguration` in `Sergin.<Module>.Infrastructure.Data`, then generate a migration:
   ```
   dotnet ef migrations add <Name> --project src/Modules/<Module>/Sergin.<Module>.Infrastructure.Data --startup-project src/Hosts/Sergin.Hosts.WebApi.All
   ```
3. Build to confirm it compiles cleanly — this repo treats every analyzer/style warning as a build error:
   ```
   dotnet build Sergin.slnx
   ```
