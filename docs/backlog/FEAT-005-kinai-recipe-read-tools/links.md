---
id: FEAT-005
type: links
status: planned
created_at: 2026-05-21
related:
  - FEAT-003
  - TASK-001
  - TASK-002
  - TASK-003
  - TASK-004
  - CR-001
  - RFC-001
---

# Links - FEAT-005

## Tasks

- [tasks/TASK-001-extend-kinai-tool-catalog-for-recipe-reads.md](tasks/TASK-001-extend-kinai-tool-catalog-for-recipe-reads.md)
- [tasks/TASK-002-implement-recipe-and-shopping-read-tools.md](tasks/TASK-002-implement-recipe-and-shopping-read-tools.md)
- [tasks/TASK-003-compose-core-react-recipe-deeplinks.md](tasks/TASK-003-compose-core-react-recipe-deeplinks.md)
- [tasks/TASK-004-render-clickable-links-in-kinai-chat.md](tasks/TASK-004-render-clickable-links-in-kinai-chat.md)

## Change Requests

- [cr/CR-001-core-react-recipe-deeplink.md](cr/CR-001-core-react-recipe-deeplink.md)

## Research

- [research/RFC-001-kinai-read-tooling-output-shape.md](research/RFC-001-kinai-read-tooling-output-shape.md)

## Moduli impattati

- `src/Infrastructures/Kin.KinHub.Core.OpenAi/ChatFeature/Services/OpenAiChatService.cs`
- `src/Businesses/Kin.KinHub.Core.Business/ChatFeature/Services/KinHubChatToolExecutor.cs`
- `src/Businesses/Kin.KinHub.Core.Business/ChatFeature/Models/ChatToolExecutionResult.cs`
- `src/Presentations/Kin.KinHub.KinAi.React/src/features/chat/pages/ConversationDetailPage.tsx`
- `src/Presentations/Kin.KinHub.KinAi.React/src/types/index.ts`
- `.github/workflows/deploy-kinai-frontend.yml`

