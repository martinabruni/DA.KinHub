---
name: manage-unit-tests
description: Create or evolve unit tests for DDD-style .NET Business services using xUnit, FluentAssertions, and NSubstitute. Use for adding a test project, resolving the real service under test through DI, registering repositories or gateways as substitutes, and verifying business behavior plus collaborator interactions.
---

# Manage Unit Tests

Test the real Business service through DI and isolate repositories or external integrations with `NSubstitute`.

## Preconditions

- Inspect existing test assets before creating new ones.
- Identify the Business service interface, the concrete implementation resolved by DI, and the collaborators it receives.
- Keep xUnit, FluentAssertions, and NSubstitute unless the repository already mandates a different test stack.

## Workflow

1. Detect or create the target test project and keep it aligned with the current solution structure.
2. Create or align shared test bootstrapping such as `Startup`, `ServiceCollectionExtensions`, a fixture, or `BaseTest` so the container can register `AddBusiness(...)`, `AddInfrastructure(...)`, and supporting dependencies.
3. Register repository interfaces and other external collaborators as `NSubstitute` instances in the test container.
4. Resolve the real Business service from the service provider instead of constructing it manually.
5. Add tests for the requested scenarios and the relevant public service methods, preferring `[Theory]` when parameterization improves coverage and readability.
6. Assert both business outcomes and interactions with substituted dependencies.
7. Propagate `CancellationToken` through the public API and verify it reaches collaborators when that behavior is part of the contract.

## Guardrails

- Do not mock the Business service under test.
- Do not replace DI with ad-hoc object construction when the service is normally resolved through `AddBusiness(...)`.
- Do not leave placeholder assertions in completed test work.
- Do not assert implementation noise; assert observable behavior and required collaborator calls.

## Completion Criteria

- The test container builds successfully.
- The real Business service is resolved from DI.
- Dependencies that touch persistence or external systems are substituted.
- The requested scenarios assert both business logic and required collaborator interactions.
