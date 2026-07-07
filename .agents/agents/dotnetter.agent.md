---
name: Dotnetter
user-invocable: true
description: Follow these conventions when generating or refactoring .NET backend code in this repository.
---

# Dotnetter

Apply these conventions to application `.cs` files under:

- `src/Domains/`
- `src/Businesses/`
- `src/Infrastructures/`
- `src/Presentations/`

Ignore scripts, tooling, and non-application C# files unless the task explicitly targets them.

## Architecture

- Organize code by feature. Name each feature folder `<FeatureName>Feature`.
- Use `Common` only for types that are genuinely shared across features.
- Keep business logic in the Business layer.
- Create one Infrastructure project per external integration type and name it `Client.Project.<IntegrationType>`.
- Keep Domain models separate from Infrastructure persistence or transport models and translate across boundaries with Mapster.

## Files and Namespaces

- Keep one type per file.
- Make classes `sealed` by default. Remove `sealed` only when real extensibility is required.
- Use file-scoped namespaces.
- Derive the namespace from the path up to the feature folder or `Common`.
- Do not include subfolders such as `Models`, `Services`, `Repositories`, `Interfaces`, `Enums`, `Dtos`, or `Exceptions` in the namespace.
- Use collection expressions such as `[]` when the target type supports them.
- Prefer `Count` over `Any()` when both are equivalent.
- Use `is null` and `is not null` instead of `== null` and `!= null`.
- Keep code comments rare. Use XML documentation on interfaces and `/// <inheritdoc/>` on implementations.
- Put one parameter per line in long method or constructor signatures.

## Dependency Injection and Configuration

- In `ServiceCollectionExtensions.cs`, use `namespace Microsoft.Extensions.DependencyInjection;`.
- Expose `AddBusiness(Action<BusinessOptions>)` for the Business layer.
- Expose `Add<Integration>Infrastructure(Action<Config>)` for each integration and compose them from `AddInfrastructure(Action<InfrastructureOptions>)`.
- Keep validation inside the options model through `Validate()`. Do not duplicate it in DI registration.
- Keep secrets and environment-specific values in configuration, never hardcoded in code.

## Validation, Mapping, and Async

- Validate request and input models with FluentValidation.
- Use Mapster only through `.Adapt<T>()`.
- Register Mapster configuration globally in DI rather than passing mapper configuration around per call.
- Propagate `CancellationToken` through async public APIs and downstream calls.

## Domain and Persistence

- When the solution models persistent entities in Domain, define `IEntity<T>` and `BaseEntity<T>` under Domain `Common` and derive persistent Domain entities from that base.
- Keep repositories and adapters aligned with the feature-first structure.
- Use interfaces, factories, and strategies when they clarify the architecture rather than adding ceremony.
