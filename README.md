# Sergin

A .NET 10 **modular monolith** for a HES (Head-End System) / IoT platform, built with Domain-Driven Design (DDD), Clean Architecture, and per-feature vertical slices. It uses .NET Aspire for local orchestration and PostgreSQL for storage.

The central component is the **HeadEnd** module — the primary entry point for IoT device communication, data processing, and integration with other subsystems — alongside a **UserAccess** module for identity and access concerns. Both are composed into a single runnable host.

## 🏗 Architectural Approach

The solution follows modern architecture practices to keep domain logic clear and the system maintainable and scalable:

- **Domain-Driven Design (DDD)** – Rich domain model with aggregates, strongly-typed IDs, domain events, and clear boundaries.
- **Clean Architecture** – Strict dependency direction across `Domain → Application → Infrastructure / Presentation`.
- **Modular Monolith** – Independent, self-contained modules (`HeadEnd`, `UserAccess`) that can later be decomposed into services.
- **CQRS** – Writes flow through MediatR commands to EF Core repositories; reads use dedicated query repositories backed by raw SQL for performance.

## 🧱 Solution Structure

```
src/
├── Hosts/
│   ├── Sergin.Hosts.All        # Runnable all-in-one Web API (composition root)
│   └── Sergin.Hosts.Shared     # Aspire service defaults (OpenTelemetry, health checks)
├── Modules/
│   ├── HeadEnd/                # IoT device management module
│   └── UserAccess/             # Identity & access module
└── SharedKernel/               # Framework-level building blocks shared across modules
```

Each module is split into `.Domain`, `.Application`, `.Infrastructure`, `.Infrastructure.Data` (DbContext + migrations), and `.Presentation` (minimal-API endpoints), plus a composition project that wires it into the host. Each module owns its own `DbContext`, migrations, and PostgreSQL schema.

## 📌 Key Features

- Centralized **HeadEnd** for managing IoT devices and data, plus a **UserAccess** module for users and permissions.
- Clean separation between domain, application, and infrastructure layers, enforced by project dependencies.
- CQRS with MediatR pipeline behaviors for permission checks and validation.
- Domain events raised on aggregates and dispatched on `SaveChanges` via EF Core interceptors.
- Extensible design for adding future modules with minimal coupling.

## 🛠 Technologies & Libraries

- **.NET 10** – Core development framework
- **.NET Aspire** – Local orchestration and observability dashboard (Postgres + pgAdmin + API)
- **Entity Framework Core** – ORM for the write side, migrations, and value converters
- **Dapper / raw SQL** – High-performance read-side query repositories via `IDbConnectionFactory`
- **PostgreSQL** – Relational database backend (per-module schemas)
- **MediatR** – In-process messaging for CQRS and decoupled communication
- **FluentValidation** – Strongly-typed, fluent request validation
- **ErrorOr** – Result/error modeling for handlers, mapped to ProblemDetails at the API edge

## 🚀 Getting Started

Requires the **.NET 10 SDK** (VS 17.13+ / Rider). Run all commands from the repo root.

```bash
# Build (warnings are treated as errors — analyzers + SonarAnalyzer enforced)
dotnet build Sergin.slnx

# Run everything in Docker (API + postgres:17 + Aspire dashboard)
docker compose -f docker-compose/docker-compose.yml up --build
```

### Run from Visual Studio

If you use **Visual Studio** (17.13+), open `Sergin.slnx`, set **`docker-compose`**
(`docker-compose/docker-compose.dcproj`) as the startup project, and press **F5**.
Visual Studio builds the images and launches the full stack (API + `postgres:17` +
Aspire dashboard) via Docker Compose, then attaches the debugger to the API.

### EF Core migrations

Each module owns its own `DbContext` and migrations. Example for the HeadEnd module:

```bash
dotnet ef migrations add <Name> \
  --project src/Modules/HeadEnd/Sergin.HeadEnd.Infrastructure.Data \
  --startup-project src/Hosts/Sergin.Hosts.All
```

Migrations are applied automatically at startup **only in the Development environment**.

> **Note:** `Directory.Build.props` enables `TreatWarningsAsErrors`, `AnalysisMode=All`, and SonarAnalyzer with `EnforceCodeStyleInBuild`. Any analyzer, style, or nullable warning will fail the build.
