---
id: TASK-002
feature: FEAT-002
type: task
status: implemented
priority: high
created_at: 2026-05-08
related:
  - TASK-001
---

# TASK-002 — Add i18n Keys for Status-Specific Error Messages

## Obiettivo

Aggiungere le chiavi di traduzione per i messaggi di errore specifici per codice di stato HTTP.

## Chiavi da aggiungere

```json
{
  "errors": {
    "sessionExpired": "Sessione scaduta, effettua di nuovo il login.",
    "forbidden": "Non hai i permessi per eseguire questa operazione.",
    "notFound": "La risorsa richiesta non è stata trovata.",
    "serverError": "Errore del server. Riprova tra qualche istante.",
    "generic": "Si è verificato un errore. Riprova."
  }
}
```

```json
{
  "errors": {
    "sessionExpired": "Session expired, please log in again.",
    "forbidden": "You don't have permission to perform this action.",
    "notFound": "The requested resource was not found.",
    "serverError": "Server error. Please try again in a moment.",
    "generic": "Something went wrong. Please try again."
  }
}
```

## File da modificare

- `src/i18n/locales/it.json`
- `src/i18n/locales/en.json`
