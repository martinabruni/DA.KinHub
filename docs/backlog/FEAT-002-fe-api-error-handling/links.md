---
id: FEAT-002
type: links
---

# Links — FEAT-002

## Bug

- [bugs/BUG-001-missing-global-error-toast.md](bugs/BUG-001-missing-global-error-toast.md)

## Tasks

- [tasks/TASK-001-extend-axios-interceptor.md](tasks/TASK-001-extend-axios-interceptor.md)
- [tasks/TASK-002-i18n-error-keys.md](tasks/TASK-002-i18n-error-keys.md)
- [tasks/TASK-003-remove-duplicate-toast-handlers.md](tasks/TASK-003-remove-duplicate-toast-handlers.md)

## File da modificare

- `src/api/apiClient.ts` — estendere response interceptor
- `src/lib/errors.ts` — aggiungere `getStatusAwareErrorMessage`
- `src/App.tsx` — rimuovere toast da QueryCache.onError
- `src/i18n/locales/it.json` — aggiungere chiavi `errors.*`
- `src/i18n/locales/en.json` — aggiungere chiavi `errors.*`
- `src/features/family/FamilyProvider.tsx` — rimuovere onError duplicati
- `src/features/fridges/FridgeProvider.tsx` — rimuovere onError duplicati
- altri provider con `toast.error` in `onError`
