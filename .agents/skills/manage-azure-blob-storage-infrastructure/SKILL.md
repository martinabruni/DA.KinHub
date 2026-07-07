---
name: manage-azure-blob-storage-infrastructure
description: Create or align Azure Blob Storage infrastructure for this DDD .NET style while keeping storage contracts business-facing and Azure-agnostic. Use for adding or refactoring blob persistence, defining `AzureBlobStorageConfig`, shared Infrastructure helpers, feature-specific storage implementations, or DI registration that keeps `Azure.Storage.Blobs` and `Azure.Identity` confined to Infrastructure.
---

# Manage Azure Blob Storage Infrastructure

Keep Domain and Business focused on storage behavior, not Azure SDK types. Keep `Azure.Storage.Blobs` and authentication details inside Infrastructure.

## Preconditions

- Detect whether `Client.Project.AzureBlobStorage` already exists and align it before creating new assets.
- Identify the business operations, containers, and authentication mode required by the feature.
- Prefer feature folders for concrete storage implementations and reserve `Common` for shared configuration and helpers.

## Workflow

1. Create or align a Domain contract such as `IBlobRepository` or a feature-specific storage contract that speaks in business terms instead of Azure terminology.
2. Create or align `AzureBlobStorageConfig` with section name, account or endpoint settings, container settings, and a single `Validate()` method.
3. Create or align shared Infrastructure helpers under `Common`, including client creation, container resolution, and reusable blob operations.
4. Instantiate `BlobServiceClient` in Infrastructure only, preferring `DefaultAzureCredential` or another `Azure.Identity` credential. Use connection strings only when the host cannot use Azure AD.
5. Implement feature-specific storage classes that inherit from the shared helper and keep SDK types inside Infrastructure boundaries.
6. Register the integration through `AddAzureBlobStorageInfrastructure(Action<AzureBlobStorageConfig>)` and compose it from `AddInfrastructure(Action<InfrastructureOptions>)`.
7. Propagate `CancellationToken` through async blob operations.

## Guardrails

- Do not leak `BlobServiceClient`, `BlobContainerClient`, `BlobClient`, SAS URIs, or Azure Identity types into Domain or Business.
- Do not hardcode container names, account names, endpoints, or secrets.
- Do not duplicate validation logic outside `AzureBlobStorageConfig.Validate()`.
- Do not bypass shared Infrastructure helpers for client or container creation.

## Completion Criteria

- Domain contracts remain storage-focused and Azure-agnostic.
- All Azure SDK and credential usage stays inside Infrastructure.
- The integration is registered through the Action Pattern and validated centrally.
- Feature implementations use shared helpers and propagate cancellation correctly.
