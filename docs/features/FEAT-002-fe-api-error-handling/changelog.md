---
id: CHANGELOG-FEAT-002
feature: FEAT-002
type: changelog
status: implemented
created_at: 2026-05-08
related:
  - FEAT-002
  - BUG-001
---

# Changelog

## 2026-05-08

### FEAT

- Implemented global API error toast for all HTTP >= 400 responses (BUG-001)
  - Centralized in axios response interceptor — fires for every API call
  - Status-specific messages: 403 `errors.forbidden`, 404 `errors.notFound`, 5xx `errors.serverError`, other `errors.generic`
  - Backend message takes priority over generic fallback when present
  - 401: unchanged token-refresh flow; added "session expired" toast before redirect to `/login`
  - files:
    - src/Presentations/Kin.KinHub.Core.React/src/api/apiClient.ts
    - src/Presentations/Kin.KinHub.Core.React/src/lib/errors.ts
    - src/Presentations/Kin.KinHub.Core.React/src/App.tsx
    - src/Presentations/Kin.KinHub.Core.React/src/i18n/locales/it.json
    - src/Presentations/Kin.KinHub.Core.React/src/i18n/locales/en.json
    - src/Presentations/Kin.KinHub.Core.React/src/features/family/FamilyProvider.tsx

### REFACTOR

- Removed duplicate `toast.error(getApiErrorMessage(err))` from all `onError` mutation callbacks in `FamilyProvider` (7 mutations)
  - files:
    - src/Presentations/Kin.KinHub.Core.React/src/features/family/FamilyProvider.tsx

- Removed `QueryCache.onError` toast handler from `App.tsx`; interceptor is now the single source of truth
  - files:
    - src/Presentations/Kin.KinHub.Core.React/src/App.tsx
