# Module Registration Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace per-module host wiring with a uniform `ISerginModule`/`ISerginWebApiModule` contract, generic SharedKernel DbContext helpers, and a Hosts.Shared bootstrap, so Program.cs holds one line per module.

**Architecture:** New `Sergin.SharedKernel.Modules` project holds two contract interfaces. Each module's composition root ships one public sealed `<Module>Module` class replacing its `InstallationExtensions.cs`. `Sergin.Hosts.Shared` gains `AddSerginWebApi`/`UseSerginWebApiAsync` extensions that register everything Program.cs registers today and loop the module list. Spec: `docs/superpowers/specs/2026-07-26-module-registration-design.md`.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, EF Core 10 + Npgsql, MediatR 12, xUnit + Testcontainers (existing integration suite is the safety net — no new tests).

## Global Constraints

- `Directory.Build.props` sets `TreatWarningsAsErrors=true`, `AnalysisMode=All`, SonarAnalyzer, `EnforceCodeStyleInBuild` — **any warning fails the build**. Mirror the code shapes of existing files named in each task; they are proven analyzer-clean.
- Central Package Management: every `PackageReference` is **version-less**; versions live only in `Directory.Packages.props` (`MediatR` 12.5.0, `Microsoft.AspNetCore.OpenApi` 10.0.9, `Scalar.AspNetCore` 2.16.10 already exist there — no `Directory.Packages.props` change expected).
- Commit style: sentence-case imperative, no prefix, e.g. `Adopt Central Package Management via Directory.Packages.props`. **Never add a `Co-Authored-By` trailer.**
- Code style: file-scoped namespaces, explicit types (no `var`), locals typed as the narrowest sufficient interface, no code comments in new source files, tabs in `.csproj` files, usings sorted System-first then alphabetical.
- Run all commands from the repo root. Build: `dotnet build Sergin.slnx` — expect `Build succeeded` with 0 warnings/0 errors.
- Integration tests need **Docker Desktop running**: `dotnet test tests/Sergin.IntegrationTests/Sergin.IntegrationTests.csproj`.
- All work on branch `feature/module-registration` (Task 1 creates it; skip if the execution harness already made an isolated worktree/branch).

---

### Task 1: Contract project `Sergin.SharedKernel.Modules`

**Files:**
- Create: `src/SharedKernel/Sergin.SharedKernel.Modules/Sergin.SharedKernel.Modules.csproj`
- Create: `src/SharedKernel/Sergin.SharedKernel.Modules/ISerginModule.cs`
- Create: `src/SharedKernel/Sergin.SharedKernel.Modules/ISerginWebApiModule.cs`
- Modify: `Sergin.slnx` (add project to the `/src/SharedKernel/` folder, lines 53–56)

**Interfaces:**
- Consumes: nothing.
- Produces: `Sergin.SharedKernel.Modules.ISerginModule` { `string Schema { get; }`, `Assembly ApplicationAssembly { get; }`, `void AddServices(IServiceCollection services, IConfigurationSection configuration)`, `Task MigrateAsync(IServiceProvider services)` } and `Sergin.SharedKernel.Modules.ISerginWebApiModule : ISerginModule` { `void MapEndpoints(RouteGroupBuilder group)` }. Tasks 3–6 depend on these exact names.

- [ ] **Step 1: Create the branch**

```bash
git switch -c feature/module-registration
```

Skip if already on an isolated feature branch/worktree.

- [ ] **Step 2: Create the csproj**

`src/SharedKernel/Sergin.SharedKernel.Modules/Sergin.SharedKernel.Modules.csproj` (tabs, no `PropertyGroup` — `Directory.Build.props` supplies everything; shape mirrors `Sergin.SharedKernel.Presentation.WebApi.csproj`):

```xml
<Project Sdk="Microsoft.NET.Sdk">
	<ItemGroup>
		<FrameworkReference Include="Microsoft.AspNetCore.App" />
	</ItemGroup>
</Project>
```

- [ ] **Step 3: Create `ISerginModule.cs`**

```csharp
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Sergin.SharedKernel.Modules;

public interface ISerginModule
{
    string Schema { get; }

    Assembly ApplicationAssembly { get; }

    void AddServices(IServiceCollection services, IConfigurationSection configuration);

    Task MigrateAsync(IServiceProvider services);
}
```

(`Task` and `IServiceProvider` come from implicit usings.)

- [ ] **Step 4: Create `ISerginWebApiModule.cs`**

```csharp
using Microsoft.AspNetCore.Routing;

namespace Sergin.SharedKernel.Modules;

public interface ISerginWebApiModule : ISerginModule
{
    void MapEndpoints(RouteGroupBuilder group);
}
```

- [ ] **Step 5: Register in `Sergin.slnx`**

In the `<Folder Name="/src/SharedKernel/">` element (currently listing Application and Domain), add alphabetically after the Domain line:

```xml
    <Project Path="src/SharedKernel/Sergin.SharedKernel.Modules/Sergin.SharedKernel.Modules.csproj" />
```

- [ ] **Step 6: Build**

Run: `dotnet build Sergin.slnx`
Expected: `Build succeeded`, 0 warnings, 0 errors (new project compiles; nothing references it yet).

- [ ] **Step 7: Commit**

```bash
git add src/SharedKernel/Sergin.SharedKernel.Modules Sergin.slnx
git commit -m "Add SharedKernel.Modules project with module contract interfaces"
```

---

### Task 2: Generic DbContext/migration helpers in SharedKernel EFCore

**Files:**
- Create: `src/SharedKernel/Sergin.SharedKernel.Infrastructure.Data.EFCore/ModuleDbContextExtensions.cs`

**Interfaces:**
- Consumes: `SerginDbContext` (same project, `Sergin.SharedKernel.Infrastructure.Data.EFCore`), internal `EventDispatcherInterceptor` (same assembly, namespace `...EFCore.Interceptors`).
- Produces: `AddModuleDbContext<TContext, TIContext, TIUnitOfWork>(this IServiceCollection, IConfigurationSection, string schema)` returning `IServiceCollection`, and `MigrateDbContextAsync<TContext>(this IServiceProvider)` returning `Task`, both in namespace `Sergin.SharedKernel.Infrastructure.Data.EFCore`. Tasks 3–4 call them.

- [ ] **Step 1: Create `ModuleDbContextExtensions.cs`**

The `AddDbContext` lambda body is copied from the current `AddDbContextAndUnitOfWork` in `src/Modules/UserAccess/Sergin.UserAccess/InstallationExtensions.cs:47-52`; the migration body mirrors `ApplyMigration` at `:57-63` (including the inner `using` on the resolved context — that exact shape is analyzer-proven).

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sergin.SharedKernel.Infrastructure.Data.EFCore.Interceptors;

namespace Sergin.SharedKernel.Infrastructure.Data.EFCore;

public static class ModuleDbContextExtensions
{
    public static IServiceCollection AddModuleDbContext<TContext, TIContext, TIUnitOfWork>(
        this IServiceCollection services,
        IConfigurationSection configuration,
        string schema)
        where TContext : SerginDbContext, TIContext, TIUnitOfWork
        where TIContext : class
        where TIUnitOfWork : class
    {
        string connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Sergin:ConnectionStrings:Database' is not configured.");

        services.AddDbContext<TContext>((sp, options) =>
            options.UseNpgsql(
                connectionString,
                pgOptions => pgOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, schema))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<EventDispatcherInterceptor>()));

        services.AddScoped<TIContext>(p => p.GetRequiredService<TContext>());
        services.AddScoped<TIUnitOfWork>(p => p.GetRequiredService<TContext>());

        return services;
    }

    public static async Task MigrateDbContextAsync<TContext>(this IServiceProvider services)
        where TContext : DbContext
    {
        using IServiceScope scope = services.CreateScope();
        using TContext context = scope.ServiceProvider.GetRequiredService<TContext>();

        await context.Database.MigrateAsync();
    }
}
```

Note the deliberate behavior change (spec decision 7): `TIContext`/`TIUnitOfWork` are forwarding registrations to the single scoped `TContext` — not `AddScoped<TIContext, TContext>()`, which would construct a second instance.

- [ ] **Step 2: Build**

Run: `dotnet build Sergin.slnx`
Expected: `Build succeeded`, 0 warnings, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/SharedKernel/Sergin.SharedKernel.Infrastructure.Data.EFCore/ModuleDbContextExtensions.cs
git commit -m "Add generic module DbContext and migration helpers to SharedKernel EFCore"
```

---

### Task 3: `UserAccessModule` class

**Files:**
- Modify: `src/Modules/UserAccess/Sergin.UserAccess/Sergin.UserAccess.csproj`
- Create: `src/Modules/UserAccess/Sergin.UserAccess/UserAccessModule.cs`

**Interfaces:**
- Consumes: `ISerginWebApiModule` (Task 1); `AddModuleDbContext`/`MigrateDbContextAsync` (Task 2); existing `UserAccessDbContext` (internal, `Sergin.UserAccess.Infrastructure.Data`, `public const string Schema = "ua"`), `IUserAccessDbContext` (same namespace), `IUserAccessUnitOfWork` (`Sergin.UserAccess.Application`), `UserAccessApplicationAssemblyReference.Assembly` (`Sergin.UserAccess.Application`, static readonly field), `AddUserDependencies()`/`MapUserEndpoints()` (internal extensions in `Sergin.UserAccess.Users`, on `IServiceCollection`/`IEndpointRouteBuilder`).
- Produces: `public sealed class Sergin.UserAccess.UserAccessModule : ISerginWebApiModule` with a public parameterless constructor. Task 6 instantiates it.

- [ ] **Step 1: Add the contract project reference**

In `Sergin.UserAccess.csproj`, add to the existing `ItemGroup` with the two `ProjectReference`s:

```xml
		<ProjectReference Include="..\..\..\SharedKernel\Sergin.SharedKernel.Modules\Sergin.SharedKernel.Modules.csproj" />
```

- [ ] **Step 2: Create `UserAccessModule.cs`**

Do **not** delete `InstallationExtensions.cs` yet (Task 7 does) — both compile side by side.

```csharp
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sergin.SharedKernel.Infrastructure.Data.EFCore;
using Sergin.SharedKernel.Modules;
using Sergin.UserAccess.Application;
using Sergin.UserAccess.Infrastructure.Data;
using Sergin.UserAccess.Users;

namespace Sergin.UserAccess;

public sealed class UserAccessModule : ISerginWebApiModule
{
    public string Schema => UserAccessDbContext.Schema;

    public Assembly ApplicationAssembly => UserAccessApplicationAssemblyReference.Assembly;

    public void AddServices(IServiceCollection services, IConfigurationSection configuration)
    {
        services.AddModuleDbContext<UserAccessDbContext, IUserAccessDbContext, IUserAccessUnitOfWork>(configuration, UserAccessDbContext.Schema);

        services.AddUserDependencies();
    }

    public Task MigrateAsync(IServiceProvider services) => services.MigrateDbContextAsync<UserAccessDbContext>();

    public void MapEndpoints(RouteGroupBuilder group) => group.MapUserEndpoints();
}
```

Accessibility note: `UserAccessDbContext` is `internal` but referencing it inside member *bodies* and generic *call* arguments of a public class is legal (this is why the spec rejected a generic base class — CS0060).

- [ ] **Step 3: Build**

Run: `dotnet build Sergin.slnx`
Expected: `Build succeeded`, 0 warnings, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/Modules/UserAccess/Sergin.UserAccess
git commit -m "Add UserAccessModule implementing the web module contract"
```

---

### Task 4: `HeadEndModule` class

**Files:**
- Modify: `src/Modules/HeadEnd/Sergin.HeadEnd/Sergin.HeadEnd.csproj`
- Create: `src/Modules/HeadEnd/Sergin.HeadEnd/HeadEndModule.cs`

**Interfaces:**
- Consumes: same contracts/helpers as Task 3; existing `HeadEndDbContext` (internal, `Sergin.HeadEnd.Infrastructure.Data`, `public const string Schema = "hes"`), `IHeadEndDbContext`, `IHeadEndUnitOfWork` (`Sergin.HeadEnd.Application`), `HeadEndApplicationAssemblyReference.Assembly` (`Sergin.HeadEnd.Application`), `AddDeviceDependencies()`/`MapDeviceEndpoints()` (`Sergin.HeadEnd.Devices`), `AddManufacturerDependencies()`/`MapManufacturerEndpoints()` (`Sergin.HeadEnd.Manufacturers`).
- Produces: `public sealed class Sergin.HeadEnd.HeadEndModule : ISerginWebApiModule` with a public parameterless constructor. Task 6 instantiates it.

- [ ] **Step 1: Add the contract project reference**

In `Sergin.HeadEnd.csproj`, add to the existing `ItemGroup` with the two `ProjectReference`s:

```xml
		<ProjectReference Include="..\..\..\SharedKernel\Sergin.SharedKernel.Modules\Sergin.SharedKernel.Modules.csproj" />
```

- [ ] **Step 2: Create `HeadEndModule.cs`**

```csharp
using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sergin.HeadEnd.Application;
using Sergin.HeadEnd.Devices;
using Sergin.HeadEnd.Infrastructure.Data;
using Sergin.HeadEnd.Manufacturers;
using Sergin.SharedKernel.Infrastructure.Data.EFCore;
using Sergin.SharedKernel.Modules;

namespace Sergin.HeadEnd;

public sealed class HeadEndModule : ISerginWebApiModule
{
    public string Schema => HeadEndDbContext.Schema;

    public Assembly ApplicationAssembly => HeadEndApplicationAssemblyReference.Assembly;

    public void AddServices(IServiceCollection services, IConfigurationSection configuration)
    {
        services.AddModuleDbContext<HeadEndDbContext, IHeadEndDbContext, IHeadEndUnitOfWork>(configuration, HeadEndDbContext.Schema);

        services.AddDeviceDependencies();
        services.AddManufacturerDependencies();
    }

    public Task MigrateAsync(IServiceProvider services) => services.MigrateDbContextAsync<HeadEndDbContext>();

    public void MapEndpoints(RouteGroupBuilder group) => group.MapDeviceEndpoints().MapManufacturerEndpoints();
}
```

- [ ] **Step 3: Build**

Run: `dotnet build Sergin.slnx`
Expected: `Build succeeded`, 0 warnings, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/Modules/HeadEnd/Sergin.HeadEnd
git commit -m "Add HeadEndModule implementing the web module contract"
```

---

### Task 5: Host bootstrap in `Sergin.Hosts.Shared`

**Files:**
- Modify: `src/SharedKernel/Sergin.SharedKernel.Infrastructure.Data.EFCore/Properties/AssemblyInfo.cs`
- Modify: `src/Hosts/Sergin.Hosts.Shared/Sergin.Hosts.Shared.csproj`
- Create: `src/Hosts/Sergin.Hosts.Shared/SerginWebApiExtensions.cs`

**Interfaces:**
- Consumes: `ISerginModule`/`ISerginWebApiModule` (Task 1); existing public types `PermissionCheckPipelineBehavior<,>` (`Sergin.SharedKernel.Application.Securities.Authorization`), `ValidationPipelineBehavior<,>` (`Sergin.SharedKernel.Application.Commands`), `IEventDispatcher` (`Sergin.SharedKernel.Application.Events`) / `DefaultEventDispatcher` (`Sergin.SharedKernel.Infrastructure.Events`), `IDbConnectionFactory` (`Sergin.SharedKernel.Infrastracture.Data` — note existing "Infrastracture" spelling), `IUserContextFactory` (`Sergin.SharedKernel.Application.Securities.Users`) / `InternalUserContextFactory` (`Sergin.SharedKernel.Infrastracture.WebApi.Users`), `ILocalizer` (`Sergin.SharedKernel.Application.Localizations`) / `DefaultLocalizer` (`Sergin.SharedKernel.Infrastructure.Localizations`); **internal** types `EventDispatcherInterceptor` (`Sergin.SharedKernel.Infrastructure.Data.EFCore.Interceptors`) and `PostgresDbConnectionFactory` (`Sergin.SharedKernel.Infrastructure.Data.EFCore`, ctor `(string connectionString)`) — both need the new `InternalsVisibleTo` grant.
- Produces: in namespace `Microsoft.Extensions.Hosting`, static class `SerginWebApiExtensions` with `AddSerginWebApi(this WebApplicationBuilder builder, IReadOnlyCollection<ISerginModule> modules)` returning `WebApplicationBuilder`, and `UseSerginWebApiAsync(this WebApplication app, IReadOnlyCollection<ISerginModule> modules)` returning `Task<WebApplication>`. Task 6 calls both.

- [ ] **Step 1: Grant internals access to Hosts.Shared**

In `src/SharedKernel/Sergin.SharedKernel.Infrastructure.Data.EFCore/Properties/AssemblyInfo.cs`, after the existing three `InternalsVisibleTo` lines, add:

```csharp
[assembly: InternalsVisibleTo("Sergin.Hosts.Shared")]
```

(Do not remove the existing grants yet — Task 7 does.)

- [ ] **Step 2: Extend `Sergin.Hosts.Shared.csproj`**

Add one `ItemGroup` with project references and add three version-less packages to the existing package `ItemGroup`, plus the audit suppression. Final file content:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />

    <PackageReference Include="MediatR" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" />
    <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" />
    <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" />
    <PackageReference Include="OpenTelemetry.Extensions.Hosting" />
    <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Http" />
    <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" />
    <PackageReference Include="Scalar.AspNetCore" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\SharedKernel\Sergin.SharedKernel.Application\Sergin.SharedKernel.Application.csproj" />
    <ProjectReference Include="..\..\SharedKernel\Sergin.SharedKernel.Infrastracture.Data\Sergin.SharedKernel.Infrastracture.Data.csproj" />
    <ProjectReference Include="..\..\SharedKernel\Sergin.SharedKernel.Infrastracture.WebApi\Sergin.SharedKernel.Infrastracture.WebApi.csproj" />
    <ProjectReference Include="..\..\SharedKernel\Sergin.SharedKernel.Infrastructure.Data.EFCore\Sergin.SharedKernel.Infrastructure.Data.EFCore.csproj" />
    <ProjectReference Include="..\..\SharedKernel\Sergin.SharedKernel.Infrastructure\Sergin.SharedKernel.Infrastructure.csproj" />
    <ProjectReference Include="..\..\SharedKernel\Sergin.SharedKernel.Modules\Sergin.SharedKernel.Modules.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Update="Microsoft.Extensions.Options" />
  </ItemGroup>

  <ItemGroup>
    <!--
      GHSA-v5pm-xwqc-g5wc (CVE-2026-49451): stack overflow when PARSING untrusted OpenAPI
      documents in Microsoft.OpenApi. Pulled in transitively (2.x) by Microsoft.AspNetCore.OpenApi
      10.0.9; the only fixed line is 3.x, which is API-incompatible with the .NET 10 OpenAPI stack.
      This service only GENERATES its own OpenAPI document (for Scalar) and never parses external
      documents, so it is not exposed. Revisit when an AspNetCore.OpenApi patch ships on a fixed 2.x.
    -->
    <NuGetAuditSuppress Include="https://github.com/advisories/GHSA-v5pm-xwqc-g5wc" />
  </ItemGroup>

</Project>
```

(This file currently uses 2-space indentation — keep it; the comment text is moved verbatim from `Sergin.Hosts.WebApi.All.csproj`.)

- [ ] **Step 3: Create `SerginWebApiExtensions.cs`**

Namespace matches the existing `Extensions.cs` in this project (`Microsoft.Extensions.Hosting`, the Aspire convention) so hosts need no extra `using`. All registrations are verbatim moves from `src/Hosts/Sergin.Hosts.WebApi.All/Program.cs` except the two documented changes: MediatR scans `module.ApplicationAssembly` in a loop, and the connection string gets a fail-fast `?? throw` instead of `!`.

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Scalar.AspNetCore;
using Sergin.SharedKernel.Application.Commands;
using Sergin.SharedKernel.Application.Events;
using Sergin.SharedKernel.Application.Localizations;
using Sergin.SharedKernel.Application.Securities.Authorization;
using Sergin.SharedKernel.Application.Securities.Users;
using Sergin.SharedKernel.Infrastracture.Data;
using Sergin.SharedKernel.Infrastracture.WebApi.Users;
using Sergin.SharedKernel.Infrastructure.Data.EFCore;
using Sergin.SharedKernel.Infrastructure.Data.EFCore.Interceptors;
using Sergin.SharedKernel.Infrastructure.Events;
using Sergin.SharedKernel.Infrastructure.Localizations;
using Sergin.SharedKernel.Modules;

namespace Microsoft.Extensions.Hosting;

public static class SerginWebApiExtensions
{
    public static WebApplicationBuilder AddSerginWebApi(this WebApplicationBuilder builder, IReadOnlyCollection<ISerginModule> modules)
    {
        IConfigurationSection serginSection = builder.Configuration.GetRequiredSection("Sergin");

        builder.Services.AddMediatR(options =>
        {
            foreach (ISerginModule module in modules)
            {
                options.RegisterServicesFromAssembly(module.ApplicationAssembly);
            }

            options.AddOpenBehavior(typeof(PermissionCheckPipelineBehavior<,>));
            options.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        builder.Services.AddOpenApi();

        builder.Services.AddScoped<IEventDispatcher, DefaultEventDispatcher>();
        builder.Services.AddScoped<EventDispatcherInterceptor>();

        string connectionString = serginSection.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Connection string 'Sergin:ConnectionStrings:Database' is not configured.");

        builder.Services.AddScoped<IDbConnectionFactory>(p => new PostgresDbConnectionFactory(connectionString));

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<IUserContextFactory, InternalUserContextFactory>();
        builder.Services.AddScoped(p => p.GetRequiredService<IUserContextFactory>().CreateUserContext());

        builder.Services.AddSingleton<ILocalizer, DefaultLocalizer>();

        foreach (ISerginModule module in modules)
        {
            module.AddServices(builder.Services, serginSection);
        }

        return builder;
    }

    public static async Task<WebApplication> UseSerginWebApiAsync(this WebApplication app, IReadOnlyCollection<ISerginModule> modules)
    {
        if (app.Environment.IsDevelopment())
        {
            foreach (ISerginModule module in modules)
            {
                await module.MigrateAsync(app.Services);
            }
        }

        foreach (ISerginWebApiModule webModule in modules.OfType<ISerginWebApiModule>())
        {
            webModule.MapEndpoints(app.MapGroup(webModule.Schema));
        }

        app.MapOpenApi();

        if (app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference();
        }

        return app;
    }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build Sergin.slnx`
Expected: `Build succeeded`, 0 warnings, 0 errors. If `CS0122` appears on `EventDispatcherInterceptor` or `PostgresDbConnectionFactory`, Step 1's `InternalsVisibleTo` line is missing or misspelled.

- [ ] **Step 5: Commit**

```bash
git add src/Hosts/Sergin.Hosts.Shared src/SharedKernel/Sergin.SharedKernel.Infrastructure.Data.EFCore/Properties/AssemblyInfo.cs
git commit -m "Add Sergin web API bootstrap extensions to Hosts.Shared"
```

---

### Task 6: Compose the host from the module list

**Files:**
- Modify: `src/Hosts/Sergin.Hosts.WebApi.All/Program.cs` (full rewrite)
- Modify: `src/Hosts/Sergin.Hosts.WebApi.All/Sergin.Hosts.WebApi.All.csproj`

**Interfaces:**
- Consumes: `HeadEndModule` (Task 4), `UserAccessModule` (Task 3), `AddSerginWebApi`/`UseSerginWebApiAsync` (Task 5), existing `AddServiceDefaults` (`Sergin.Hosts.Shared`, `Extensions.cs`).
- Produces: the runnable host; `public partial class Program;` must remain — `tests/Sergin.IntegrationTests/SerginApiFactory.cs` is `WebApplicationFactory<Program>`.

- [ ] **Step 1: Rewrite `Program.cs`**

Full new content (the local is `IReadOnlyCollection<ISerginModule>`, not an array — repo convention: narrowest sufficient interface):

```csharp
using Sergin.HeadEnd;
using Sergin.SharedKernel.Modules;
using Sergin.UserAccess;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults("sergin-all");

IReadOnlyCollection<ISerginModule> modules = [new HeadEndModule(), new UserAccessModule()];

builder.AddSerginWebApi(modules);

WebApplication app = builder.Build();

await app.UseSerginWebApiAsync(modules);

await app.RunAsync();

public partial class Program;
```

- [ ] **Step 2: Slim `Sergin.Hosts.WebApi.All.csproj`**

Full new content — drops `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`, the `Sergin.SharedKernel.Infrastracture.WebApi` reference (now transitive via Hosts.Shared), and the `NuGetAuditSuppress` group (moved in Task 5). Keeps `Microsoft.EntityFrameworkCore.Design` (required by `dotnet ef --startup-project`), `UserSecretsId`, and the Docker properties:

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

	<PropertyGroup>
		<UserSecretsId>2a09bf43-7332-4840-b0f1-257f452d1cc5</UserSecretsId>
		<DockerDefaultTargetOS>Linux</DockerDefaultTargetOS>
		<DockerfileContext>..\..\..</DockerfileContext>
		<DockerComposeProjectPath>..\..\..\docker-compose\docker-compose.dcproj</DockerComposeProjectPath>
	</PropertyGroup>
	<ItemGroup>
		<ProjectReference Include="..\..\Modules\HeadEnd\Sergin.HeadEnd\Sergin.HeadEnd.csproj" />
		<ProjectReference Include="..\..\Modules\UserAccess\Sergin.UserAccess\Sergin.UserAccess.csproj" />
		<ProjectReference Include="..\Sergin.Hosts.Shared\Sergin.Hosts.Shared.csproj" />
	</ItemGroup>
	<ItemGroup>
		<PackageReference Include="Microsoft.EntityFrameworkCore.Design">
			<PrivateAssets>all</PrivateAssets>
			<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
		</PackageReference>
	</ItemGroup>

</Project>
```

- [ ] **Step 3: Build**

Run: `dotnet build Sergin.slnx`
Expected: `Build succeeded`, 0 warnings, 0 errors. Contingency: if an `NU1902`/`NU1903` audit warning for GHSA-v5pm-xwqc-g5wc surfaces in `Sergin.Hosts.WebApi.All` (transitive audit), re-add the `NuGetAuditSuppress` `ItemGroup` (with its comment) to this csproj as well — the spec explicitly allows keeping it in both projects.

- [ ] **Step 4: Run the integration suite**

Docker Desktop must be running.
Run: `dotnet test tests/Sergin.IntegrationTests/Sergin.IntegrationTests.csproj`
Expected: all tests pass — this proves MediatR scanning, migrations via `MigrateAsync`, route groups, and both modules' endpoints all work through the new composition.

- [ ] **Step 5: Commit**

```bash
git add src/Hosts/Sergin.Hosts.WebApi.All
git commit -m "Compose the all-in-one host from the module contract list"
```

---

### Task 7: Delete the superseded installation extensions

**Files:**
- Delete: `src/Modules/UserAccess/Sergin.UserAccess/InstallationExtensions.cs`
- Delete: `src/Modules/HeadEnd/Sergin.HeadEnd/InstallationExtensions.cs`
- Modify: `src/SharedKernel/Sergin.SharedKernel.Infrastructure.Data.EFCore/Properties/AssemblyInfo.cs`

**Interfaces:**
- Consumes: nothing new. The deleted files' callers were removed in Task 6.
- Produces: nothing — pure removal. `Register<Module>Commands`, `Add<Module>Module`, `Run<Module>Module` cease to exist.

- [ ] **Step 1: Delete both files**

```bash
git rm src/Modules/UserAccess/Sergin.UserAccess/InstallationExtensions.cs src/Modules/HeadEnd/Sergin.HeadEnd/InstallationExtensions.cs
```

- [ ] **Step 2: Trim the internals allowlist**

After the deletions, no code in `Sergin.Hosts.WebApi.All`, `Sergin.HeadEnd`, or `Sergin.UserAccess` touches EFCore-assembly internals (the interceptor resolution now happens inside `ModuleDbContextExtensions`, same assembly; the connection factory only in Hosts.Shared). Reduce `AssemblyInfo.cs` to:

```csharp
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// In SDK-style projects such as this one, several assembly attributes that were historically
// defined in this file are now automatically added during build and populated with
// values defined in project properties. For details of which attributes are included
// and how to customise this process see: https://aka.ms/assembly-info-properties


// Setting ComVisible to false makes the types in this assembly not visible to COM
// components.  If you need to access a type in this assembly from COM, set the ComVisible
// attribute to true on that type.

[assembly: ComVisible(false)]

[assembly: InternalsVisibleTo("Sergin.Hosts.Shared")]
```

Contingency: if the build then fails with `CS0122` naming a specific assembly, restore only that assembly's `InternalsVisibleTo` line and note which internal type it still uses.

- [ ] **Step 3: Build**

Run: `dotnet build Sergin.slnx`
Expected: `Build succeeded` — proving nothing referenced the deleted extensions or the removed grants.

- [ ] **Step 4: Run the integration suite**

Run: `dotnet test tests/Sergin.IntegrationTests/Sergin.IntegrationTests.csproj`
Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add -A src/Modules src/SharedKernel/Sergin.SharedKernel.Infrastructure.Data.EFCore/Properties
git commit -m "Remove per-module installation extensions superseded by module classes"
```

---

### Task 8: Update CLAUDE.md and the two skills

**Files:**
- Modify: `.claude/CLAUDE.md`
- Modify: `.claude/skills/add-module/SKILL.md`
- Modify: `.claude/skills/add-feature/SKILL.md`

**Interfaces:**
- Consumes: final shape from Tasks 1–7.
- Produces: docs matching reality. No code.

- [ ] **Step 1: `.claude/CLAUDE.md` — six targeted replacements**

1. In "### EF Core migrations", replace the sentence fragment
   `(`Run<Module>Module` → `ApplyMigration`, called for every module from `Sergin.Hosts.WebApi.All/Program.cs`)`
   with
   `(the host bootstrap's `UseSerginWebApiAsync` in `Sergin.Hosts.Shared` calls every module's `ISerginModule.MigrateAsync`)`.
2. In "### Host / module composition", replace the `Sergin.Hosts.WebApi.All` bullet's text after the dash with:
   `the actual runnable Web API ("all-in-one" host). Its `Program.cs` is ~15 lines: it builds an `IReadOnlyCollection<ISerginModule>` (`[new HeadEndModule(), new UserAccessModule()]`) and hands it to the Hosts.Shared bootstrap — `builder.AddSerginWebApi(modules)` before `Build()`, `await app.UseSerginWebApiAsync(modules)` after. Adding a module to a host = one `ProjectReference` + one element in that collection.`
3. Replace the `Sergin.Hosts.Shared` bullet's text after the dash with:
   `Aspire service defaults (OpenTelemetry, health checks) **plus the Sergin web bootstrap** (`SerginWebApiExtensions`, namespace `Microsoft.Extensions.Hosting`): `AddSerginWebApi` registers MediatR (scanning every module's `ApplicationAssembly`) + pipeline behaviors, OpenAPI, event dispatcher/interceptor, `IDbConnectionFactory`, user context, localizer, then loops `module.AddServices(...)`; `UseSerginWebApiAsync` migrates every module (Development only), maps each `ISerginWebApiModule`'s endpoints under `MapGroup(module.Schema)`, then maps OpenAPI/Scalar.`
4. In the "Modules live under..." bullet, replace
   `A module is wired into the host through its **`InstallationExtensions`** (in the `Sergin.<Module>` composition project, no suffix): `Add<Module>Module` registers DI + DbContext; `Run<Module>Module` maps endpoints under a route group and applies migrations.`
   with
   `A module is wired into hosts through its **`<Module>Module` class** (in the `Sergin.<Module>` composition project, no suffix) implementing `ISerginWebApiModule` from `Sergin.SharedKernel.Modules`: `Schema`, `ApplicationAssembly`, `AddServices` (calls the generic `AddModuleDbContext<TContext, TIContext, TIUnitOfWork>` helper plus per-aggregate `Add<X>Dependencies()`), `MigrateAsync`, and `MapEndpoints` (per-aggregate `Map<X>Endpoints()`). One class per module implements all its capabilities; which capabilities run is the host's choice.`
5. In "Cross-cutting conventions", replace
   `**MediatR pipeline behaviors** (registered in `Program.cs`, order matters):`
   with
   `**MediatR pipeline behaviors** (registered in Hosts.Shared's `AddSerginWebApi`, order matters):`
6. In the "Endpoint route strings never include the schema segment" gotcha, replace
   `the schema prefix is added exactly once via `application.MapGroup("<schema>")` in the module's `Run<Module>Module`.`
   with
   `the schema prefix is added exactly once by the host bootstrap (`app.MapGroup(module.Schema)` inside `UseSerginWebApiAsync`).`
   And in the "Endpoints" convention bullet, replace `are instantiated and mapped in the module's `*InstallationExtensions.Map...Endpoints`` with `are instantiated and mapped in the aggregate's `<Aggregate>InstallationExtensions.Map<Aggregate>Endpoints`, called from the module's `<Module>Module.MapEndpoints``.

- [ ] **Step 2: `.claude/skills/add-module/SKILL.md` — four replacements**

1. In the step-1 table, change the composition-root row's References cell from `` `<Module>.Infrastructure`, `<Module>.Presentation.WebApi` `` to `` `<Module>.Infrastructure`, `<Module>.Presentation.WebApi`, `SharedKernel.Modules` `` and the sentence below the table from `same two `ProjectReference`s + this `FrameworkReference`` to `same three `ProjectReference`s + this `FrameworkReference``.
2. In the `InternalsVisibleTo` paragraph of step 1, delete the sentences beginning `**In addition**, `src/SharedKernel/...AssemblyInfo.cs` gates `EventDispatcherInterceptor`` through `...only shows up once the composition root project exists.` — new modules no longer need an EFCore-assembly grant (the interceptor is resolved inside `AddModuleDbContext`, same assembly).
3. Replace the whole step-4 section body ("Copy `Sergin.UserAccess/InstallationExtensions.cs` structure exactly..." and its three bullet lines) with:

   ````markdown
   Create `Sergin.<Module>/<Module>Module.cs` — copy `Sergin.UserAccess/UserAccessModule.cs` exactly, renaming `UserAccess` → `<Module>` and swapping the schema/DbContext/assembly-reference types:

   - `public sealed class <Module>Module : ISerginWebApiModule` (from `Sergin.SharedKernel.Modules`).
   - `Schema` → `<Module>DbContext.Schema`; `ApplicationAssembly` → `<Module>ApplicationAssemblyReference.Assembly`.
   - `AddServices` → `services.AddModuleDbContext<<Module>DbContext, I<Module>DbContext, I<Module>UnitOfWork>(configuration, <Module>DbContext.Schema);` plus per-aggregate `Add<X>Dependencies()` calls (none yet on a fresh module).
   - `MigrateAsync` → `services.MigrateDbContextAsync<<Module>DbContext>();`
   - `MapEndpoints` → per-aggregate `Map<X>Endpoints()` calls (empty method body on a fresh module).
   ````
4. Replace step-5's second bullet (the `Program.cs` instructions starting `add `using Sergin.<Module>;` and, matching the existing HeadEnd/UserAccess lines...`) with:
   `add `using Sergin.<Module>;` and one element to the modules collection: `IReadOnlyCollection<ISerginModule> modules = [new HeadEndModule(), new UserAccessModule(), new <Module>Module()];` — nothing else; the bootstrap loops handle MediatR, DI, migrations, and endpoint mapping.`

- [ ] **Step 3: `.claude/skills/add-feature/SKILL.md` — one addition**

At the end of step 6 (`Register the endpoint in the module's `<Aggregate>InstallationExtensions.Map<Aggregate>Endpoints`...`), append:
` For a brand-new aggregate, create that file first (copy `UserInstallationExtensions.cs`) and wire it into the module class: `services.Add<Aggregate>Dependencies()` in `<Module>Module.AddServices` and `group.Map<Aggregate>Endpoints()` in `<Module>Module.MapEndpoints`.`

- [ ] **Step 4: Commit**

```bash
git add .claude/CLAUDE.md .claude/skills/add-module/SKILL.md .claude/skills/add-feature/SKILL.md
git commit -m "Update docs and skills for module contract registration"
```

---

### Task 9: Final verification

**Files:** none (verification only).

**Interfaces:** none.

- [ ] **Step 1: Clean build**

Run: `dotnet build Sergin.slnx`
Expected: `Build succeeded`, 0 warnings, 0 errors.

- [ ] **Step 2: Full integration suite**

Docker Desktop running.
Run: `dotnet test tests/Sergin.IntegrationTests/Sergin.IntegrationTests.csproj`
Expected: all tests pass.

- [ ] **Step 3: Startup smoke test**

Run: `dotnet run --project src/Hosts/Sergin.Hosts.WebApi.All`
Expected: host starts without exceptions (requires the dev database from user secrets/env; if unavailable, the integration suite in Step 2 already booted the identical host in-process — treat that as the smoke test and skip this step). Stop with Ctrl+C.

- [ ] **Step 4: Wrap up the branch**

Use the superpowers:finishing-a-development-branch skill to decide merge/PR handling for `feature/module-registration`.
