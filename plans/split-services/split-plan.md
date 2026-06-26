# KinHub Separation + Code-First Migration Plan

## Summary
- Keep one repo and one PostgreSQL database, but split runtime ownership into three deployable areas:
  - `Kin.KinHub.Core`: hub-only Static Web App with cards and redirects.
  - `Kin.KinHub.Identity`: centralized identity Static Web App + containerized Azure Functions app.
  - `Kin.KinHub.KinRecipe`: recipe Static Web App + containerized Azure Functions app.
- Target Azure Container Apps Consumption for function containers, using GitHub Container Registry to avoid ACR standing cost.
- Use OIDC Authorization Code + PKCE for cross-domain authentication. Identity is the authorization server; every Kin frontend redirects to Identity and receives service-scoped tokens.
- Start DB-first to code-first migration with a no-op baseline that preserves current `identity`, `core`, and `kinrecipe` schemas exactly.

## Key Changes
- Create separate presentation modules:
  - `Kin.KinHub.Identity.Functions` hosts auth/OIDC, user/session, token, family/member, and service entitlement endpoints.
  - `Kin.KinHub.KinRecipe.Functions` hosts recipe/fridge/shopping-list/assistant HTTP functions.
  - `Kin.KinHub.Core.React` becomes hub-only; extract new `Kin.KinHub.Identity.React` and `Kin.KinHub.KinRecipe.React`.
- Replace `Kin.KinHub.Shared.Api` as the production host. Keep it temporarily only as a compatibility/reference module until function parity tests pass, then remove it.
- Preserve shared contracts in repo-local libraries, but avoid runtime coupling:
  - shared auth/token validation abstractions
  - shared HTTP result mapping/validation helpers
  - shared DTOs only where they are true wire contracts
- Move service discovery and service entitlement reads behind Identity-owned endpoints. KinRecipe validates JWTs locally but calls Identity only for user/family/service authorization decisions that cannot be trusted from token claims alone.
- Convert EF to code-first in stages:
  - First migration: baseline no-op matching current generated model and existing database.
  - Second stage: split DbContexts by ownership: `IdentityDbContext`, `CoreDbContext`, `KinRecipeDbContext`.
  - Keep one database and schema separation; no extra database.
- Update IaC:
  - one Azure Static Web App for each frontend
  - one Container Apps environment on Consumption
  - one container app per function backend
  - one PostgreSQL Flexible Server/database
  - one Key Vault
  - one Application Insights/Log Analytics setup with low-retention defaults
  - GHCR image settings and secrets instead of ACR
- Use Azure guidance as constraints: classic Functions Consumption is legacy for new apps; Container Apps Consumption can scale to zero and has free monthly grants. References: Azure Functions hosting options, .NET isolated worker guide, Container Apps overview/billing, and Functions on Container Apps docs.

## Task Plan With LLM Judge Gates
For every task below, use this loop: implement task, run deterministic checks, run an LLM judge against the task acceptance criteria, and if the judge fails, revert only that task’s changes and restart the task from its beginning.

1. **Architecture Baseline Task**
   - Document target module map, deployment map, auth flow, schema ownership, and compatibility strategy.
   - Judge passes only if the document clearly separates Core, Identity, and KinRecipe and identifies no hidden dependency on `Shared.Api`.

2. **Code-First Baseline Migration Task**
   - Remove EF Power Tools as the source of truth.
   - Add code-first entity configuration and initial no-op migrations for current schemas/tables/constraints.
   - Judge passes only if generated SQL is either empty or metadata-only against the current DB shape.

3. **Identity Service Task**
   - Build Identity Functions host with OIDC + PKCE, login/register/logout/refresh/me, family/member, service catalogue, and entitlement endpoints.
   - Judge passes only if Identity can authenticate users independently and exposes no KinRecipe business logic.

4. **KinRecipe Service Task**
   - Build KinRecipe Functions host for recipe, fridge, shopping list, and assistant endpoints.
   - Validate JWTs issued by Identity and enforce family/service access through claims plus Identity entitlement checks.
   - Judge passes only if KinRecipe has no direct dependency on Identity database/infrastructure projects.

5. **Frontend Split Task**
   - Turn Core React into hub-only cards and unauthenticated redirect to Identity URL.
   - Extract Identity frontend for sign-in/register/session flows.
   - Extract KinRecipe frontend for recipe workflows.
   - Judge passes only if each frontend builds independently and no service frontend imports another service’s feature code.

6. **Infrastructure Task**
   - Replace App Service Bicep with Static Web Apps + Container Apps Consumption + PostgreSQL + Key Vault + observability.
   - Add per-service app settings for Identity URL, issuer, audiences, CORS origins, callback URLs, and GHCR image references.
   - Judge passes only if there is no paid always-on API compute resource and no second database.

7. **CI/CD Task**
   - Update workflows to build/test each .NET service, build each frontend, build/push function containers to GHCR, and deploy only changed deployables where possible.
   - Judge passes only if Core, Identity, and KinRecipe can fail independently in CI.

8. **Compatibility Removal Task**
   - After parity tests pass, delete or archive `Kin.KinHub.Shared.Api` production wiring.
   - Judge passes only if no frontend, IaC, or workflow references the shared API host.

## Test Plan
- Backend: unit tests for auth/token validation, service entitlement, code-first mappings, and KinRecipe authorization.
- Integration: function host tests for Identity and KinRecipe HTTP endpoints using the same shared PostgreSQL test database with separate schemas.
- Migration: compare generated baseline SQL against current schema; verify migrations apply to an empty database and match expected schema.
- Frontend: build/lint all three apps; route tests for unauthenticated redirects, Identity callback handling, and hub card redirects.
- IaC: Bicep build/what-if validation; verify all custom domain and CORS settings are parameterized.
- End-to-end: login on Identity, return to Core hub, open KinRecipe, call protected KinRecipe API, logout centrally.

## Assumptions
- Use Azure Container Apps Consumption with containerized Azure Functions and GHCR images.
- Keep `.NET 10` isolated worker unless Azure hosting validation later proves a blocker.
- Use OIDC + PKCE, not shared localStorage tokens across domains.
- Keep one PostgreSQL database and existing schemas.
- First DB migration is a no-op baseline, not a schema redesign.
- Identity owns centralized authentication and service entitlement decisions.
