---
name: project-initializer
description: Scaffold or update DDD-style .NET solution layouts with standardized Domain, Business, Infrastructure, Presentation, and Test projects, plus `.slnx` or `.sln` generation. Use for creating a new backend skeleton, adding or removing integration, presentation, or test projects, or realigning an existing solution tree with this catalog's conventions.
---

# Project Initializer

Use `skills/project-initializer/ddd_initializer.py` to create or update the catalog's standard .NET solution layout.

## Preconditions

- Verify `python --version` reports Python 3.10 or later.
- Verify `dotnet --version` succeeds before running the script.
- Run the command from the repository root or pass `--root` explicitly.
- Treat `--remove-project` as destructive and expect an interactive confirmation.

## Managed Layout

```text
root/
├── docs/
│   ├── specs/
│   ├── researches/
│   └── plans/
├── ops/
│   ├── iac/
│   └── pipelines/
├── src/
│   ├── Domains/
│   │   └── Client.Project.Domain/
│   ├── Businesses/
│   │   └── Client.Project.Business/
│   ├── Infrastructures/
│   │   └── Client.Project.SqlServer/
│   ├── Presentations/
│   │   └── Client.Project.Api/
│   └── Tests/
│       └── Client.Project.UnitTests/
└── Client.Project.slnx (or .sln)
```

## Workflow

1. Normalize `--client`, `--project`, and optional `--domain` to alphanumeric namespace segments.
2. Always create the core `Domain` and `Business` projects.
3. Add `Presentation`, `Infrastructure`, and `Test` projects from the comma-separated `--presentations`, `--infrastructures`, and `--tests` arguments.
4. Use repeatable `--add-project Category:Type` arguments for incremental additions after the initial scaffold.
5. Use repeatable `--remove-project Category:Type` arguments for incremental removals and confirm each destructive action.
6. Prefer `.slnx` by default and switch to `--solution-format sln` only when a classic solution file is required.
7. Re-run the script idempotently to add missing projects or regenerate the solution without duplicating existing `.csproj` files.

## Invocation Patterns

```powershell
python skills/project-initializer/ddd_initializer.py --client Contoso --project Sales --presentations Api --infrastructures SqlServer,OpenAi
```

```powershell
python skills/project-initializer/ddd_initializer.py --client Contoso --project Sales --domain Orders --presentations Api,Function --infrastructures SqlServer,Redis --tests UnitTests,IntegrationTests
```

```powershell
python skills/project-initializer/ddd_initializer.py --client Contoso --project Sales --solution-format sln
```

```powershell
python skills/project-initializer/ddd_initializer.py --client Contoso --project Sales --add-project Infrastructures:Redis --add-project Presentations:Function
```

```powershell
python skills/project-initializer/ddd_initializer.py --client Contoso --project Sales --remove-project Infrastructures:Redis
```

## Guardrails

- Keep the standard `docs/`, `ops/`, and `src/` layout intact.
- Prefer rerunning the script over manually editing project scaffolding.
- Do not remove a project unless the user requested it and the confirmation prompt is accepted.
- Do not assume a specific shell; keep commands valid from the current workspace root.

## Completion Criteria

- The requested project tree exists under the standard layout.
- The solution file exists in the requested format and includes the created projects.
- Repeated additions do not duplicate existing projects.
- Requested removals happen only after confirmation and leave the rest of the tree intact.
