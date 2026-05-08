---
id: TASK-003
feature: FEAT-002
type: task
status: implemented
priority: medium
created_at: 2026-05-08
related:
  - TASK-001
---

# TASK-003 — Remove Duplicate Toast Handlers from Providers

## Obiettivo

Una volta che l'interceptor gestisce globalmente i toast, rimuovere i `toast.error(getApiErrorMessage(err))` dagli `onError` delle singole mutation per evitare toast duplicati.

## Providers da aggiornare

Per ogni mutation che contiene solo `onError: (err) => toast.error(getApiErrorMessage(err))`, rimuovere il callback `onError` (o ridurlo alla sola logica non-toast, es. reset di stato locale).

Provider candidati (verificare al momento dell'implementazione):

- `src/features/family/FamilyProvider.tsx`
- `src/features/fridges/FridgeProvider.tsx`
- qualsiasi altro provider con lo stesso pattern

## App.tsx

Rimuovere il `toast.error` dal `QueryCache.onError` (l'interceptor lo gestisce già). Mantenere il callback vuoto o rimuoverlo del tutto se non serve per altri scopi.

## Note

- Non rimuovere `onError` che contengono logica diversa dal toast (es. reset form, navigate, setState)
- Dopo questa task, il toast di errore esiste in un solo posto: l'axios interceptor
