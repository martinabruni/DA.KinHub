---
status: In review
---

# BUG-FEAT-016-001 - Avviare la release dopo l'infrastruttura

- **Feature interessata**: FEAT-016 `riallineamento-infrastruttura-dev`
- **Tipo**: correzione delivery, orchestrazione GitHub Actions
- **Readiness**: `ready`
- **Stato**: `In review`
- **Breaking change prodotto**: no
- **Risultato**: `release.yml` distribuisce un commit soltanto dopo la conclusione con successo del provisioning infrastrutturale richiesto per quello stesso commit.

## Contesto autonomo

`infrastructure.yml` e `release.yml` sono attualmente attivati in parallelo dal push su `main`. Quando un commit modifica sia `infra/**` sia codice applicativo, la release può raggiungere il passaggio `Read ARM deployment outputs` mentre il deployment ARM stabile `kinhub-dev-infrastructure` è ancora in esecuzione. Gli output necessari alla release non sono quindi ancora disponibili e la pipeline termina senza eseguire migration o deployment applicativo.

Evidenza osservata:

- Release run `31216426464`, SHA `942031bcb6b4b543d213b183876eed8e857a50c5`: fallita alle `20:36:19Z` nel recupero degli output ARM.
- Infrastructure run `31216426414`, stesso SHA: conclusa con successo alle `20:39:27Z`.
- Il job release ha completato build, test, packaging, artifact upload e login OIDC; tutti i passaggi successivi alla lettura degli output sono stati saltati.

## Scope

### Incluso

- Introdurre una dipendenza di esecuzione tra provisioning infrastrutturale e release quando entrambi sono richiesti dallo stesso push.
- Impedire che la release legga gli output ARM prima che `infrastructure.yml` sia concluso con esito `success`.
- Correlare la release al commit SHA corretto e impedire l'avvio per un'infrastruttura fallita, cancellata o ancora in corso.
- Conservare i contratti esistenti di `ci.yml`, `infrastructure.yml` e `release.yml`, il deployment ARM stabile e il build-once deploy-many.
- Aggiungere test o validazioni del workflow per il percorso di successo e per gli esiti non riusciti dell'infrastruttura.

### Escluso

- Modifiche a Bicep, risorse Azure, migration, API, frontend o contratti di prodotto.
- Bypass del what-if, del deployment incrementale, dei controlli di sicurezza o del gate live `GATE-003`.
- Avvio della release mentre il provisioning è in corso o dopo un provisioning fallito.
- Introduzione di nuovi ambienti, workflow oltre quelli approvati o deployment concorrenti non coordinati.

## Tracciabilità

| Tipo             | Riferimenti                                                                     | Contributo del bug                                                               |
| ---------------- | ------------------------------------------------------------------------------- | -------------------------------------------------------------------------------- |
| Flussi           | Nessuno                                                                         | Corregge esclusivamente l'ordine operativo del delivery su `main`                |
| Requisiti        | NFR-004-NFR-007, NFR-011-NFR-014                                                | Preserva disponibilità, sicurezza, osservabilità e coerenza del deploy condiviso |
| Regole/decisioni | `AGENTS.md` sezioni CI/CD e Azure Functions; `feature.plan.md` sezioni 6-7 e 11 | Rende effettivo il prerequisito infrastrutturale prima della release             |
| Architettura     | `docs/brainstorming/architecture.md` sezioni 11-12; ADR-018, ADR-019, ADR-020   | Mantiene separati provisioning e delivery applicativo con un ordine verificabile |

## Dipendenze

### Feature prerequisite

Nessuna. Il bug corregge workflow già presenti e può essere sviluppato su un ramo dedicato.

### Gate e assunzioni

| ID       | Stato | Impatto                                                                                                | Evidenza per chiudere                                                                                       |
| -------- | ----- | ------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------- |
| GATE-003 | open  | La verifica completa del comportamento live resta vincolata all'accesso alla subscription Azure target | Run GitHub Actions riusciti sullo stesso SHA, smoke test release e verifica live secondo AC-096 di FEAT-016 |

## Parallelismo consentito

Nessuno con modifiche concorrenti a `.github/workflows/infrastructure.yml`, `.github/workflows/release.yml` o alla loro orchestrazione. La correzione deve congelare prima il contratto di triggering, il criterio di correlazione SHA e il comportamento per gli esiti `failure`, `cancelled` e `in_progress`.

## Contratto di consegna

### Comportamento

- Per un push che richiede provisioning e release, `release.yml` non deve iniziare il deploy applicativo prima del completamento riuscito di `infrastructure.yml`.
- La release deve usare il risultato dell'infrastruttura relativo allo stesso commit, non un deployment di un commit diverso.
- Se l'infrastruttura fallisce o viene cancellata, la release non deve partire e deve risultare spiegabile dai run collegati.
- Se l'infrastruttura è ancora in corso, la release deve attendere oppure restare non avviata; non deve fallire tentando di leggere output ARM prematuri.
- Dopo il gate riuscito devono restare invariati build, artifact, migration, One Deploy, deploy Static Web App e smoke test previsti da FEAT-016.

### Touchpoint previsti

- **Dominio/business**: Non pertinente.
- **Persistenza/migration**: Non pertinente; verificare solo che le migration restino dopo il gate infrastrutturale.
- **API/integrazioni**: Non pertinente; preservare gli output ARM consumati da `.github/workflows/release.yml`.
- **Frontend/UX**: Non pertinente.
- **Infrastruttura/configurazione**: `.github/workflows/infrastructure.yml`, `.github/workflows/release.yml`, eventuali validatori workflow in `tools/skill-harness/`.
- **Documentazione/operazioni**: `docs/operations/azure-deployment.md`, `README.md` e riferimenti operativi al trigger della release, se modificati dal comportamento.

### Errori, sicurezza e osservabilità

- Non usare secret, token o payload Azure per trasferire lo stato tra workflow.
- Conservare correlazione SHA, run ID e stato del workflow come metadati tecnici non sensibili.
- Non consentire che un run fallito o cancellato abiliti il deploy applicativo.
- Mantenere action esterne fissate a SHA completo e i permessi minimi già definiti.

## Criteri di accettazione

### AC-016-001 - Attesa del provisioning riuscito

- **Dato** un commit su `main` che richiede sia infrastruttura sia release
- **Quando** `infrastructure.yml` è ancora in esecuzione
- **Allora** `release.yml` non esegue lettura degli output ARM, migration o deployment applicativo
- **Fonte**: evidenza run `31216426464`/`31216426414`; feature.plan.md sezioni 6-7

### AC-016-002 - Correlazione sullo stesso commit

- **Dato** un provisioning concluso con successo
- **Quando** viene avviata la release conseguente
- **Allora** la release è associata allo stesso commit SHA del provisioning e usa gli output ARM di quel risultato
- **Fonte**: feature.md AC-093 e AC-094; AGENTS.md regola sui commit trusted

### AC-016-003 - Blocco su esito infrastrutturale non riuscito

- **Dato** un provisioning con esito `failure` o `cancelled`
- **Quando** il sistema valuta l'avvio della release
- **Allora** la release non parte e il motivo è visibile nei metadati o nei run collegati senza esporre dati sensibili
- **Fonte**: AGENTS.md regole CI/CD e sicurezza; feature.md AC-093

### AC-016-004 - Deploy applicativo invariato dopo il gate

- **Dato** un provisioning riuscito e una release autorizzata
- **Quando** la release completa il workflow
- **Allora** build, artifact, migration, One Deploy Function, deploy Static Web App e smoke test restano eseguiti nell'ordine previsto, senza ricompilazione aggiuntiva
- **Fonte**: feature.md AC-094; feature.plan.md sezione 7

### AC-016-005 - Validazione statica del contratto

- **Dato** il repository aggiornato
- **Quando** vengono eseguiti i validatori dei workflow e del repository
- **Allora** non esistono trigger o dipendenze che consentano una release concorrente al provisioning e le action, permessi e workflow ammessi restano conformi alle regole repository
- **Fonte**: AGENTS.md sezioni CI/CD e Definition of Done; feature.md AC-092 e AC-095

## Strategia di verifica

| Livello              | Verifica                                                                                                               | Evidenza attesa                                                                                                         |
| -------------------- | ---------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------- |
| Unitario             | Non pertinente: il comportamento è di orchestrazione workflow                                                          | N/A motivato                                                                                                            |
| Integrazione         | Validazione del grafo/trigger e correlazione SHA tra workflow                                                          | Test o script del repository con casi success, failure, cancelled e in-progress                                         |
| Frontend/component   | Non pertinente                                                                                                         | N/A motivato                                                                                                            |
| End-to-end/manuale   | Esecuzione di un commit che attiva entrambi i workflow e osservazione dell'ordine; verifica di un provisioning fallito | Run Actions collegati sullo stesso SHA, nessun deploy prematuro, smoke test release riuscito quando Azure è disponibile |
| Validator repository | `npm run skills:validate`, validazione workflow/actionlint e controlli applicabili                                     | Esiti verdi senza modifiche a file fuori scope                                                                          |

## Definition of Done

- Tutti i criteri di accettazione sono verificati oppure il vincolo live è esplicitamente mantenuto sotto `GATE-003`.
- La release non può più partire prima del successo dell'infrastruttura richiesta per lo stesso SHA.
- I casi `failure`, `cancelled` e `in_progress` sono verificati e non autorizzano deployment applicativi.
- Workflow, documentazione operativa e validatori applicabili sono coerenti e non introducono nuovi workflow o secret.
- I comandi di qualità richiesti da `AGENTS.md` sono eseguiti e riportati senza dichiarazioni non verificate.
- Il bug resta in stato `In review` finché la responsabile umana non autorizza la transizione successiva.
