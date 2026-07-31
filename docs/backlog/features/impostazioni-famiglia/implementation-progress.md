# Stato implementazione: FEAT-004 - Consultare le impostazioni della famiglia

- Aggiornato (UTC): `2026-07-31T10:21:00Z`
- Branch: `dev`
- Commit di partenza: `1bc499a`
- Motivo checkpoint: `consegna locale completata; monitoraggio PR richiesto`

## Scope e decisioni

- FEAT-004 e stata portata a `In progress` in `docs/backlog/features/impostazioni-famiglia/feature.md`.
- Implementato uno stack read-side KinHub Family minimale ma coerente con il piano: entita `FamilyInvitation`, repository dettagli/membri/inviti, codec Data Protection separati per membri e inviti, servizio business `IFamilySettingsService`, endpoint HTTP GET protetti e OpenAPI runtime/statico aggiornati.
- Le proiezioni membro e creatore invito restituiscono oggi `displayName` e `initials` null per mancanza di persistenza PII approvata nel modello attuale; il fallback resta delegato al frontend come richiesto dal piano.
- Il frontend ha una nuova route `ProtectedRoute` `/settings/family`, estensione di `SettingsPage`, pagina `FamilySettingsPage`, metodi API dedicati, i18n/help e guide `family` it/en.
- La telemetria Family e stata integrata con nuovi operation name e misure di paginazione a bassa cardinalita.

## Completato

- Backend Domain/Business/Infrastructure per lettura Family: nuovi file sotto `src/backend/domains/DA.KinHub.Domain/Families`, `src/backend/business/DA.KinHub.Business/Identity`, `src/backend/infrastructure/DA.KinHub.Infrastructure/Persistence` e `.../Pagination`.
- Endpoint e middleware: aggiornati `KinHubFamilyFunctions.cs`, `ApiRoutes.cs`, `ExceptionHandlingMiddleware.cs`, `KinHubOperations.cs`, `KinHubTelemetry.cs`, `OpenApiDocumentProvider.cs`, `openapi.yaml`.
- Migration EF generata: `20260730202708_AddFamilyInvitations.*` + snapshot.
- Frontend: `FamilySettingsPage.tsx`, estensione `SettingsPage.tsx`, `App.tsx`, `route-registry.json`, `api.ts`, `KinPatterns.tsx`, nuove chiavi i18n/help.
- Documentazione utente e artifact generati: nuove guide `docs/user-guide/it/family.md`, `docs/user-guide/en/family.md`, update `settings.md`, `src/frontend/src/generated/docs/index.json`, `src/frontend/public/release-notes.json`, patch notes rigenerate.
- Fragment release aggiunto: `changes/none-added-family-settings-page.md`.

## Modifiche in corso

- `docs/operations/database-migrations.md`: manca documentazione esplicita di verifica/rollback per `AddFamilyInvitations`.
- `docs/operations/observability.md`: manca aggiornamento con le verifiche aggregate per `kinhub.family_details`, `kinhub.family_members_page`, `kinhub.family_invitations_page`.
- `tests/**`: aggiunti test dedicati per dominio inviti, business FamilySettings, migration/index PostgreSQL, telemetria Family e client API frontend.
- `docs/backlog/features/impostazioni-famiglia/feature.md`: portata a `In review`; la consegna resta subordinata a commit, push, PR e check verdi.

## Verifiche

| Comando | Esito | Dettaglio utile |
|---|---|---|
| `dotnet build KinHub.slnx --configuration Release --no-restore` | pass | Build .NET verde dopo endpoint, service e migration FEAT-004. |
| `dotnet test KinHub.slnx --configuration Release --no-build` | pass | 57 test passati; 5 integration PostgreSQL skip per harness non disponibile. |
| `dotnet ef migrations add AddFamilyInvitations --project "src/backend/infrastructure/DA.KinHub.Infrastructure/DA.KinHub.Infrastructure.csproj" --configuration Release` | pass | Migration generata con designer e snapshot. |
| `npm ci --prefix src/frontend` | pass | Dipendenze installate; presenti warning audit non affrontati in questa slice. |
| `npm run --prefix src/frontend typecheck` | pass | TypeScript verde. |
| `npm run --prefix src/frontend build` | pass | Build Vite/PWA verde dopo aggiunta della guida `family`. |
| `npm run --prefix src/frontend routes:validate` | pass | Route validator verde. |
| `npm run --prefix src/frontend i18n:validate` | pass | Parita traduzioni verde. |
| `npm run --prefix src/frontend design-system:validate` | pass | Validator design system verde. |
| `npm run --prefix src/frontend lint` | pass | ESLint verde. |
| `npm run --prefix src/frontend test` | pass | 31 test frontend esistenti passati. |
| `npm run docs:sync` | pass | Guide sincronizzate in `src/frontend/src/generated/docs`. |
| `npm run docs:validate` | pass | Documentazione utente valida. |
| `npm run release:generate` | pass | Patch notes e metadata rigenerati. |
| `npm run release:validate` | pass | Fragment release valido. |
| `npm run skills:validate` | pass | `openapi.yaml` allineato alle nuove HTTP Function. |
| `dotnet publish src/backend/applications/DA.KinHub.Functions/DA.KinHub.Functions.csproj -c Release -o artifacts/backend/publish --no-restore` | pass | Publish Function App riuscito. |
| `./scripts/package-backend.ps1 -Environment Development` | pass | ZIP backend prodotto con `host.json` e assembly alla root. |
| `git diff --check` | pass | Nessun whitespace error. |
| `npm run test --prefix src/frontend` dopo correzione test API | pass | 32 test passati. |

## Pull request e GitHub Actions

- Pull request: `da aprire dopo il commit`
- SHA monitorato: `da definire`
- Stato Actions: `da avviare`

## Lavoro residuo

- [x] Aggiungere test FEAT-004 per dominio inviti, business FamilySettings, repository PostgreSQL, telemetria e client API frontend.
- [x] Aggiornare `docs/operations/database-migrations.md` con verifica e rollback della migration `AddFamilyInvitations`.
- [x] Aggiornare `docs/operations/observability.md` con i nuovi segnali/metriche Family.
- [x] Eseguire build, test, lint, validator, `skills:build`, publish e package backend.
- [x] Riesaminare e rigenerare gli output di `release:generate` e `docs:sync`.
- [ ] Committare su `dev`, pushare, aprire PR verso `main` e attendere GitHub Actions verdi sull'ultimo SHA.

## Human in the loop

`Nessuno`

## Ripresa

Prima azione concreta: verificare `git diff` e `git status`, creare il commit della slice su `dev`, eseguire push, aprire la PR verso `main` e monitorare tutti i check fino a esito `success`.
