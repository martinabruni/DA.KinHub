---
name: manage-azure-service-bus-infrastructure
description: Create or align publisher-only Azure Service Bus infrastructure for this DDD .NET style. Use for adding or refactoring generic publisher contracts, destination-specific publishers, `AzureServiceBusConfig`, shared send helpers, or DI registration for `Client.Project.AzureServiceBus`, while keeping consumers out of scope.
---

# Manage Azure Service Bus Infrastructure

Keep this integration strictly publisher-only. Keep Domain contracts message-focused and keep Azure Service Bus SDK details inside Infrastructure.

## Preconditions

- Detect whether `Client.Project.AzureServiceBus` already exists and align it before adding new assets.
- Identify the message contracts, destinations, and hosting model that will compose this publisher integration.
- Keep queue, topic, or namespace settings in configuration rather than in message contracts.

## Implementation Workflow

1. Create or align the Domain publisher abstraction as `IPublisher<TMessage>`.
2. Add destination-specific publisher interfaces only when the Domain needs explicit intent, and have them inherit from `IPublisher<TMessage>`.
3. Create or align `AzureServiceBusConfig` with namespace, transport, and destination settings plus a single `Validate()` method.
4. Create or align a shared `AzureServiceBusBasePublisher` that centralizes send logic, serialization policy, retries, and cancellation handling.
5. Create exactly one concrete publisher per destination under Infrastructure.
6. Register the integration through `AddAzureServiceBusInfrastructure(Action<AzureServiceBusConfig>)` and compose it from `AddInfrastructure(Action<InfrastructureOptions>)`.
7. Keep secrets and transport settings in secure configuration sources, not in code.

## Guardrails

- Do not scaffold consumers, handlers, or Azure Function triggers in this project.
- Do not couple Domain contracts to Azure SDK types or transport-specific configuration.
- Do not hardcode connection strings, keys, namespace values, queue names, or topic names.
- Do not duplicate validation outside `AzureServiceBusConfig.Validate()`.

## Completion Criteria

- Domain exposes `IPublisher<TMessage>` and any needed intent-specific publisher interfaces.
- Infrastructure contains one concrete publisher per destination and a shared base publisher.
- The integration is registered through the Action Pattern and validated centrally.
- No consumer code or Azure Function trigger logic is introduced.
