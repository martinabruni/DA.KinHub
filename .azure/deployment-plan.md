# KinList audio async rollout plan

> **Historical record.** This document captures a point-in-time validation of the KinList audio
> pipeline rollout performed on 2026-07-04, when KinList still ran as a dedicated Container App and
> migrations ran as a Container Apps Job. That architecture has since been replaced: KinRecipe,
> KinList, and the KinList audio worker are now hosted together in the `Kin.KinHub.App.Functions`
> Azure Functions app (see `ops/iac/main.bicep`), and migrations run directly from the CI/CD
> pipeline (see `.github/workflows/deploy-backend.yml`) instead of via a Container Apps Job. The
> sections below are left as-is for historical reference; do not use them as current deploy guidance.

## Status

- Current status: Validated
- Local implementation preflight: Completed on 2026-07-04
- Target-environment `azure-validate`: Completed on 2026-07-04 against `rg-kinhub-dev` with an environment-specific parameter set derived from the live dev resources.

## Scope

Roll out the KinList audio pipeline migration from synchronous multipart processing to:

`browser -> blob upload (SAS) -> storage queue -> KinList audio worker -> PostgreSQL -> polling API -> frontend draft/proposals`

## Prerequisites

- Apply `managed-identities.bicep` before `main.bicep`.
- Start from these checked-in parameter scaffolds, then replace placeholders with target-environment values:
  - `ops/iac/managed-identities.sample.bicepparam`
  - `ops/iac/main.sample.bicepparam`
- Build and publish these images:
  - `Kin.KinHub.KinList.Api`
  - `Kin.KinHub.KinList.AudioWorker`
  - `Kin.KinHub.Migrations.Runner`
- Provide image tags for:
  - `kinListImage`
  - `kinListAudioWorkerImage`
  - `kinListMigrationImage`

## Infrastructure changes

- Dedicated Storage Account for KinList audio blobs and queues.
- Private blob container `kinlist-audio`.
- Queues:
  - `kinlist-audio-processing`
  - `kinlist-audio-poison`
- User-assigned identity for the KinList audio worker.
- Container App for the KinList audio worker.
- Lifecycle policy deletes stale audio blobs after 24 hours.

## Rollout order

1. Deploy managed identities.
2. Deploy infrastructure changes from `ops/iac/main.bicep`.
3. Run the KinList migration job before exposing the new API/frontend revision.
4. Deploy the KinList API revision with `AudioStorage__*` and `KinList__Audio*` settings.
5. Deploy the KinList audio worker revision.
6. Deploy the KinList React frontend that uses `audio-operations`.

## Validation

- `az bicep build --file ops/iac/main.bicep --outfile ops/iac/main.json`
- `az bicep build --file ops/iac/managed-identities.bicep --outfile ops/iac/managed-identities.json`
- `az bicep lint --file ops/iac/main.bicep`
- `az deployment group validate --resource-group <rg> --template-file ops/iac/managed-identities.bicep --parameters @<managed-identities.parameters.json>`
- `az deployment group what-if --resource-group <rg> --template-file ops/iac/managed-identities.bicep --parameters @<managed-identities.parameters.json> --no-pretty-print`
- `az deployment group validate --resource-group <rg> --template-file ops/iac/main.bicep --parameters @<main.parameters.json>`
- `az deployment group what-if --resource-group <rg> --template-file ops/iac/main.bicep --parameters @<main.parameters.json> --no-pretty-print`
- `dotnet build Kin.KinHub.Core.slnx`
- `dotnet test src\Tests\Kin.KinHub.Core.Test\Kin.KinHub.Core.Test.csproj --filter "KinList"`
- `dotnet test src\Tests\Kin.KinHub.Core.Test\Kin.KinHub.Core.Test.csproj --filter "KinListAzureStorageAzuriteIntegrationTests"`
- `npm test` in `src/Presentations/Kin.KinHub.KinList.React`
- `npm run build` in `src/Presentations/Kin.KinHub.KinList.React`

## Role Assignment Verification

- Status: Verified by static code review on 2026-07-04.
- Identities checked:
  - KinList API user-assigned identity
  - KinList audio worker user-assigned identity
  - KinList migration job user-assigned identity
- Roles confirmed in `ops/iac/main.bicep`:
  - KinList API: `Key Vault Secrets User`, `Storage Blob Data Contributor`, `Storage Queue Data Contributor`, `Cognitive Services User`, `Cognitive Services OpenAI User`
  - KinList audio worker: `Key Vault Secrets User`, `Storage Blob Data Contributor`, `Storage Queue Data Message Processor`, `Cognitive Services User`, `Cognitive Services OpenAI User`
  - KinList migration job: `Key Vault Secrets User`
- Runtime configuration checked:
  - KinList API and KinList audio worker now set `OpenAi__UseManagedIdentity=true` and `Speech__UseManagedIdentity=true`
  - KinList API and worker no longer receive OpenAI/Speech API key secrets in the normal runtime path

## Validation Proof

- `az account show`
  - Passed. Active subscription: `MPN-BM` (`a148a62f-0509-4dd5-a61f-0043b182d5f1`), tenant `Doers Academy`.
- `az bicep lint --file ops/iac/main.bicep`
  - Passed. No lint errors.
- `az bicep build --file ops/iac/main.bicep --outfile ops/iac/main.json`
  - Passed.
- `az bicep build --file ops/iac/managed-identities.bicep --outfile ops/iac/managed-identities.json`
  - Passed.
- `az deployment group validate --resource-group rg-kinhub-dev --template-file ops/iac/managed-identities.bicep --parameters @<temp-managed-identities-parameters.json>`
  - Passed.
- `az deployment group what-if --resource-group rg-kinhub-dev --template-file ops/iac/managed-identities.bicep --parameters @<temp-managed-identities-parameters.json> --no-pretty-print`
  - Passed.
  - Expected change set: create the new `kinlist-audio-worker-dev-ca-identity` user-assigned identity.
- `az deployment group validate --resource-group rg-kinhub-dev --template-file ops/iac/main.bicep --parameters @<temp-main-parameters.json>`
  - Passed.
- `az deployment group what-if --resource-group rg-kinhub-dev --template-file ops/iac/main.bicep --parameters @<temp-main-parameters.json> --no-pretty-print`
  - Passed after wiring the Static Web Apps repo metadata explicitly in `ops/iac/main.bicep`.
  - Expected change set includes the new KinList audio storage account, blob container, processing/poison queues, worker Container App, and the related RBAC assignments.
  - Residual `Modify` noise remains on the existing Static Web Apps for `properties.stableInboundIP` and `properties.trafficSplitting`. Those values are service-managed runtime properties; the previous unwanted drift on `branch`, `provider`, `repositoryUrl`, and `deploymentAuthPolicy` is gone.
- `az bicep build-params --file ops/iac/managed-identities.sample.bicepparam`
  - Passed.
- `az bicep build-params --file ops/iac/main.sample.bicepparam`
  - Passed.
- `dotnet build Kin.KinHub.Core.slnx`
  - Passed.
  - Residual warning: `NU1903` on `Microsoft.OpenApi` `2.0.0`.
- `dotnet test src\Tests\Kin.KinHub.Core.Test\Kin.KinHub.Core.Test.csproj --filter "KinList"`
  - Passed after aligning the infrastructure template assertions with the Managed Identity runtime path and re-running the KinList-only suite in sequence.
  - Includes an API integration proof that `POST /api/audio-operations` and `POST /api/audio-operations/{id}/complete-upload` stay below 500 ms locally and do not invoke the audio draft generator, even when the fake generator is configured with a 2-second delay.
- `dotnet test src\Tests\Kin.KinHub.Core.Test\Kin.KinHub.Core.Test.csproj --filter "KinListAzureStorageAzuriteIntegrationTests"`
  - Passed.
  - Covers real Blob SAS round-trip and Queue enqueue/receive/renew/poison/delete against Azurite via the production storage adapters.
- `npm test` in `src/Presentations/Kin.KinHub.KinList.React`
  - Passed: 54/54 tests.
  - Includes explicit refresh/resume coverage for persisted audio-operation ids on:
    - `KinListsPage` new-list audio flow
    - `KinListDetailPage` draft and append audio flows
  - Includes client-side cleanup coverage for the case where `POST /api/audio-operations` succeeds but the SAS upload fails: the frontend now issues best-effort `DELETE /api/audio-operations/{id}` and avoids unhandled promise rejections in the capture dialog.
  - The upload-failure cleanup path is covered for:
    - new-list audio on `KinListsPage`
    - draft audio on `KinListDetailPage`
    - append audio on `KinListDetailPage`
  - Includes explicit cancellation coverage for in-flight processing from the capture dialog across:
    - new-list audio on `KinListsPage`
    - draft audio on `KinListDetailPage`
    - append audio on `KinListDetailPage`
  - The cancellation path aborts the client request, clears the pending operation id, and issues best-effort `DELETE /api/audio-operations/{id}` without surfacing a failure toast.
- `npm run build` in `src/Presentations/Kin.KinHub.KinList.React`
  - Passed.
  - Residual warning: Vite chunk-size warning for the main bundle.
- Target environment details used for validation:
  - Resource group: `rg-kinhub-dev`
  - Key Vault: `kinhubdevkv`
  - Azure OpenAI account: `kinhub-dev-oai`
  - Speech account: `kinhub-dev-spch`
  - PostgreSQL server/database: `kinhub-dev-psql` / `kinhub-dev-psqldb`
  - Container Apps environment: `kinhub-dev-cae`
  - New audio storage account name validated for availability: `kinhubdevaudio`
  - New worker Container App name validated in the parameter set: `kinlist-audio-worker-dev-ca`
  - Validation parameters were supplied via temporary local JSON files so checked-in sample parameter files can remain free of environment secrets and credentials.

## Non-goals

- No automatic deploy is performed from this implementation branch.
- A real deploy is still intentionally out of scope for this implementation branch.
