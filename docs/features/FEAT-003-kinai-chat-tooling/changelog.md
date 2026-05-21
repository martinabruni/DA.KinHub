---
id: CHANGELOG-FEAT-003
feature: FEAT-003
type: changelog
status: validated
created_at: 2026-05-20
related:
  - FEAT-003
  - BUG-001
  - BUG-002
  - TASK-001
  - TASK-002
  - TASK-003
  - TASK-004
  - RFC-001
---

# Changelog

## 2026-05-20

### FEAT

- Implemented confirmed KinAi shopping-list tool execution and persisted tool results back into chat
  - files:
    - src/Businesses/Kin.KinHub.Core.Business/ChatFeature/Interfaces/IChatManager.cs
    - src/Businesses/Kin.KinHub.Core.Business/ChatFeature/Interfaces/IChatToolExecutor.cs
    - src/Businesses/Kin.KinHub.Core.Business/ChatFeature/Models/ChatToolExecutionResult.cs
    - src/Businesses/Kin.KinHub.Core.Business/ChatFeature/Services/ChatManager.cs
    - src/Businesses/Kin.KinHub.Core.Business/ChatFeature/Services/KinHubChatToolExecutor.cs
    - src/Businesses/Kin.KinHub.Core.Business/ServiceCollectionExtensions.cs
    - src/Presentations/Kin.KinHub.Shared.Api/ChatFeature/Controllers/ChatController.cs

### BUG

- Fixed KinAi message sender labels, alignment, and readable time metadata in the conversation UI
  - files:
    - src/Presentations/Kin.KinHub.KinAi.React/src/features/chat/pages/ConversationDetailPage.tsx
    - src/Presentations/Kin.KinHub.KinAi.React/src/types/index.ts
    - src/Presentations/Kin.KinHub.KinAi.React/src/i18n/locales/it.json
    - src/Presentations/Kin.KinHub.KinAi.React/src/i18n/locales/en.json
    - src/Domains/Kin.KinHub.Core.Domain/ChatFeature/Models/ChatMessageRole.cs
    - src/Domains/Kin.KinHub.Core.Domain/ChatFeature/Models/ChatToolCallStatus.cs

- Fixed no-argument tool previews so list commands no longer render as raw `{}`
  - files:
    - src/Presentations/Kin.KinHub.KinAi.React/src/features/chat/pages/ConversationDetailPage.tsx
    - src/Presentations/Kin.KinHub.KinAi.React/src/i18n/locales/it.json
    - src/Presentations/Kin.KinHub.KinAi.React/src/i18n/locales/en.json
