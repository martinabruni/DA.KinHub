# KinHub agent guide

KinHub (`APP_NAME=KinHub`, `APP_DOMAIN=KinHub`) è una piattaforma familiare minimalista. Default locale `it`, supportate `it,en`.

## Regole

- Backend .NET 10 in `src/backend`, DDD pragmatico: domain non dipende da EF/framework; business contiene use case; infrastructure implementa persistenza; applications espone HTTP.
- Frontend React/TypeScript strict, feature-oriented, shadcn/ui-compatible, mobile-first. Nessuna stringa visibile hardcoded: usare i18next e mantenere parità it/en.
- Ogni route deve stare nel route registry, avere titolo/help/guida in entrambe le lingue e renderizzare `PageHelpAccordion` subito dopo il titolo. Anche errori e 404.
- Mantenere light/dark/system, PWA, onboarding riavviabile e accessibilità tastiera/reduced motion.
- Aggiornare guida utente, patch note, change fragment e skill quando si introduce comportamento riutilizzabile. `VERSION` è la singola fonte SemVer.
- Le skill descrivono conoscenza/cataloghi, non eseguono codice dinamico; ogni modifica di produzione passa build/test/deploy.
- Non versionare segreti. Usare env, GitHub Secrets, OIDC e Key Vault. Log senza token o dati sensibili.

## Definition of Done

Build, test, lint/static checks, i18n, route docs, skill registry, fragments e Bicep validi; documentazione bilingue e aggiornamenti strutturali coerenti. Comandi principali: `dotnet build KinHub.sln`, `dotnet test KinHub.sln`, `cd src/frontend && npm ci && npm run build`, `npm run skills:validate`.

Per promuovere UI o servizi riutilizzabili: implementare nel layer corretto, aggiungere test/esempio, aggiornare la skill e `skills/registry.json`, creare fragment e documentazione bilingue. Aggiornare questo file se cambia una regola strutturale.
