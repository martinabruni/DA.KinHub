---
name: manage-dataverse-infrastructure
description: Create or align Dataverse infrastructure for this DDD .NET style while keeping Dataverse SDK usage and logical names inside Infrastructure. Use for adding or refactoring `Client.Project.Dataverse`, centralizing metadata, building resilient repositories and batching, registering `DataverseConfig`, or mapping Domain models with Mapster only.
---

# Manage Dataverse Infrastructure

Keep Domain business-focused. Keep Dataverse SDK access, logical names, metadata, and batching mechanics inside Infrastructure.

## Preconditions

- Detect whether `Client.Project.Dataverse` already exists and align it before adding new assets.
- Identify the Domain models, repository contracts, and Dataverse tables involved in the requested feature.
- Reserve `Common` for shared configuration and repository helpers; keep metadata and repositories feature-first.

## Workflow

1. Create or align Domain models and repository contracts outside Infrastructure, typed on Domain models rather than Dataverse payloads.
2. Create or align `DataverseConfig` with environment, credential, batching, and retry settings plus a single `Validate()` method.
3. Create or align `DataverseBaseRepository` to centralize `ServiceClient` usage, transient retry handling, batching, pagination, and cancellation propagation.
4. Centralize logical table names, primary keys, and attribute names in static metadata classes inside Infrastructure.
5. Configure Mapster globally and translate every Domain-to-Dataverse boundary through `.Adapt<T>()`.
6. Create or align feature repositories that consume the metadata constants and inherit from the shared repository base.
7. Register the integration through `AddDataverseInfrastructure(Action<DataverseConfig>)` and compose it from `AddInfrastructure(Action<InfrastructureOptions>)`.

## Guardrails

- Do not place Dataverse logical names, schema names, or SDK types in Domain or Business.
- Do not scatter logical names across repositories; keep them in centralized metadata classes.
- Do not use AutoMapper, `IMapper`, or manual mapping code for Domain translations.
- Do not hardcode environment URLs, tenant IDs, client IDs, or secrets.

## Completion Criteria

- Domain contracts remain infrastructure-agnostic.
- Infrastructure owns all Dataverse SDK access, metadata, and retry or batch behavior.
- Mapster is the only mapping mechanism used at the boundary.
- The integration is registered through the Action Pattern and validated centrally.
