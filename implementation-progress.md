# Stato implementazione: ristrutturazione infrastruttura e pipeline dev

- Aggiornato (UTC): `2026-08-06T00:00:00Z`
- Branch: `dev`
- Commit di partenza: `c69b4a47db75f61775c2ca3582d504e5b96a349d`
- Motivo checkpoint: `contesto >= 30%`

## Scope e decisioni

- Applicato `docs/backlog/features/riallineamento-infrastruttura-dev/feature.plan.md` per Bicep, workflow, skill harness, packaging e documentazione.
- Nomi dev confermati esplicitamente in `infra/environments/dev.bicepparam`.
- Nessuna verifica live Azure eseguita: la subscription target non e' accessibile dalla sessione corrente.
- Preservate modifiche preesistenti non pertinenti nel worktree.

## Completato

- Creati `infra/main.bicep`, `infra/environments/dev.bicepparam` e moduli `monitoring`, `data-security`, `functions`, `static-web-app`; rimossi entry point/moduli precedenti.
- Aggiunti linked backend Static Web Apps `/api`, cap Log Analytics e tag richiesti.
- Sostituiti workflow precedenti con `ci.yml`, `infrastructure.yml`, `release.yml`; aggiunto CODEOWNERS.
- Aggiunta modalita' packaging backend senza ricompilazione.
- Aggiunta skill infrastructure e aggiornato il workflow skill implementation.
- Aggiornati README, bootstrap prompt, AGENTS, deployment plan e documentazione operativa principale.

## Modifiche in corso

- `tools/skill-harness/index.mjs`: validazione workflow/Bicep implementata e verificata.
- `docs/**`, `README.md`: riferimenti operativi aggiornati; restano solo riferimenti storici e il testo del piano.
- `infra/**`, `.github/workflows/**`: Bicep validato; actionlint/YAML parser non disponibili localmente.
- Artefatti release e registry: rigenerati e verificati.

## Verifiche

| Comando | Esito | Dettaglio utile |
|---|---|---|
| `npm run skills:read -- implementation` | `pass` | Skill letta prima delle modifiche. |
| inventario repository / grep consumer | `pass` | Identificati entry point, workflow e riferimenti obsoleti. |
| Azure CLI live inventory / what-if | `non eseguito` | Subscription target non accessibile dalla sessione. |
| `az bicep build` | `pass` | Verificato anche dopo il consolidamento del modulo data-security. |
| `az bicep lint/build/build-params` | `pass` | Entry point e parameter file dev compilano senza errori. |
| `npm.cmd run skills:build && npm.cmd run skills:validate` | `pass` | Registry aggiornato; 7 skill valide. |
| `npm.cmd run docs:validate && npm.cmd run docs:sync` | `pass` | 7 pagine sincronizzate bilingue. |
| `npm.cmd run release:validate && npm.cmd run release:generate` | `pass` | 44 fragment validi e metadata generati. |
| `dotnet build KinHub.slnx --configuration Release --no-restore` | `pass` | Build senza warning/errori. |
| `dotnet test KinHub.slnx --configuration Release --no-build` | `pass` | 60 test passati, 5 integration skip. |
| `npm run --prefix src/frontend test/lint/typecheck/build` | `pass` | Test, lint, typecheck e build frontend passati. |
| `powershell.exe -ExecutionPolicy Bypass -File scripts/package-backend.ps1 -Environment CI -SkipBuild` | `pass` | ZIP One Deploy e checksum generati senza ricompilare. |
| `actionlint` / parser YAML | `non eseguito` | Tool non installati nella sessione locale; CI installa actionlint. |

## Pull request e GitHub Actions

- Pull request: `non ancora aperta`
- SHA monitorato: `non ancora disponibile`
- Stato Actions: `non eseguito`

## Lavoro residuo

- [x] Estendere harness e generare `.agents/skills/registry.json`.
- [x] Aggiornare tutti i consumer non storici di Bicep/workflow.
- [x] Correggere e verificare Bicep, workflow, docs, frontend e backend localmente.
- [x] Eseguire build/test/lint/package/validatori applicabili.
- [ ] Validare YAML con actionlint in CI e verificare il workflow su GitHub.
- [ ] Verificare accesso Azure; se non disponibile, documentare il blocco senza dichiarare verifiche live passate.

## Human in the loop

Serve accesso Azure alla subscription `a148a62f-0509-4dd5-a61f-0043b182d5f1` per inventory, validate, what-if e smoke test live. Serve inoltre esecuzione CI GitHub per actionlint e verifica dei workflow sul runner Ubuntu.

## Ripresa

Prima azione concreta: ottenere accesso Azure e avviare `infrastructure.yml` da `main`; prima del deploy leggere l'artifact what-if e bloccare qualsiasi delete/replacement.
