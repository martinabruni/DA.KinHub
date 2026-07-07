---
name: manage-sql-server-infrastructure
description: Create or align `Client.Project.SqlServer` infrastructure for this DDD .NET style using EF Core SQL Server Code First and migrations as the only supported schema workflow. Use for adding or refactoring SQL Server persistence, creating or reviewing entities, `DbContext`, entity type configurations, repositories, migrations, or idempotent release scripts, and for stopping safely if a pre-existing database is detected.
---

# Manage SQL Server Infrastructure

Use EF Core Code First with migrations. Treat the Infrastructure project as the source of truth for persistence models, mappings, and schema history. Keep Domain models separate and translate them with Mapster.

## Preconditions

- Detect whether the SQL Server infrastructure project already exists and align it before adding new files.
- Detect whether the target database is new or already contains schema history that did not originate from this project's migrations.
- Stop immediately if a pre-existing database is detected. Do not create a baseline migration and do not alter the schema.

## Target Structure

```text
src/
├── Domains/
│   └── Client.Project.Domain/
│       ├── Common/
│       └── <Feature>Feature/
│           ├── Models/
│           └── Repositories/
└── Infrastructures/
    └── Client.Project.SqlServer/
        ├── Common/
        │   ├── Configurations/
        │   ├── Context/
        │   └── Migrations/
        └── <Feature>Feature/
            ├── Entities/
            ├── Mappings/
            └── Repositories/
```

## Workflow

1. Create or align `Client.Project.SqlServer` with `Microsoft.EntityFrameworkCore.SqlServer` and the EF Core tooling package required for migrations.
2. Keep Domain models and repository contracts in Domain feature folders. Keep EF persistence entities, `DbContext`, `IEntityTypeConfiguration<T>`, and migrations inside SQL Server Infrastructure only.
3. Organize entities, mappings, and repositories by feature. Reserve `Common` for shared `DbContext`, configuration, migrations, and cross-feature helpers.
4. Translate between Domain models and persistence entities with Mapster; do not collapse the Domain model into the EF entity.
5. Configure DI with `AddSqlServerInfrastructure(Action<SqlServerConfig>)` and compose it from `AddInfrastructure(Action<InfrastructureOptions>)`.
6. Put every configuration rule in `SqlServerConfig.Validate()` and keep DI registration focused on binding, validation, and service registration.
7. Create descriptive migrations with an explicit command such as:

```powershell
dotnet ef migrations add <MigrationName> --project src/Infrastructures/Client.Project.SqlServer/Client.Project.SqlServer.csproj --startup-project <startup-project> --context <db-context>
```

8. Review every generated migration, designer file, and model snapshot before accepting it, with special attention to renames, drops, and other data-loss operations.
9. Keep existing applied migrations immutable. Do not delete, rewrite, or squash them.
10. Check for unmigrated model changes with `dotnet ef migrations has-pending-model-changes`.
11. Generate an idempotent release script for each rollout with `dotnet ef migrations script --idempotent`.
12. Apply the reviewed SQL script through the deployment pipeline before application rollout. Do not apply migrations at application startup.

## Guardrails

- Do not use reverse engineering, schema-first workflows, or external scaffold generators for SQL Server persistence in scope.
- Do not call `Database.MigrateAsync()`, `EnsureCreated()`, or `EnsureCreatedAsync()` during application startup.
- Do not expose EF entities from Domain contracts.
- Do not hardcode connection strings or bypass `SqlServerConfig.Validate()`.
- Do not continue when the target database already exists outside this migration workflow.

## Completion Criteria

- The SQL Server project uses EF Core Code First and versioned migrations only.
- Domain models remain separate from persistence entities and are mapped with Mapster.
- The Infrastructure project contains `DbContext`, feature mappings, reviewed migrations, and configuration validated through `SqlServerConfig.Validate()`.
- `dotnet ef migrations has-pending-model-changes` reports no pending model drift.
- An idempotent migration script is ready for pipeline execution, or the workflow stopped safely because a pre-existing database was detected.
