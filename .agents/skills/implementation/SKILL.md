---
id: kinhub-implementation
name: KinHub repository implementation workflow
version: 0.8.0
area: implementation
description: Esecuzione autonoma end-to-end di modifiche repository, checkpoint riprendibili e consegna tramite pull request.
references: AGENTS.md, .agents/skills/implementation/templates/implementation-progress.md
---

# KinHub repository implementation

## Scopo

Portare una modifica al repository, inclusi fix, refactor, workflow, documentazione versionata e feature approvate, fino alla pull request senza arresti prematuri, conservando uno stato riprendibile quando e indispensabile interrompere il lavoro.

## Quando usare

Usa questa skill ogni volta che l'utente chiede di implementare, completare, correggere o aggiornare qualcosa nel repository.

## Quando non usare

Non usarla per brainstorming, ricerca, backlog, sola pianificazione, code review o richieste informative che non prevedono modifiche.

## Componenti e servizi disponibili

La fonte autorevole e `AGENTS.md`; se il lavoro appartiene a una feature approvata usa la relativa cartella di backlog, altrimenti lavora direttamente nei percorsi coinvolti. Solo durante un'interruzione ammessa mantieni `implementation-progress.md` nella cartella della feature oppure, se non esiste, nella cartella piu vicina che rappresenta il lavoro corrente o nella root del repository. Git e GitHub CLI gestiscono la consegna finale.

## API e interfacce

Prima di lavorare individua il contenitore autorevole del lavoro: cartella della feature con `feature.md` e piano applicabile se la richiesta nasce dal backlog, oppure i file/percorsi direttamente interessati se si tratta di fix o modifica puntuale. Leggi anche eventuali Change Request e un checkpoint esistente. `implementation-progress.md` segue `templates/implementation-progress.md` e deve permettere a una nuova sessione di ripartire senza ricostruire decisioni gia prese.

Il checkpoint contiene: richiesta o feature di riferimento, data UTC, branch, commit di partenza e motivo dell'interruzione; scope e decisioni; lavoro completato; modifiche in corso per file; comandi di verifica con esito; pull request, SHA e stato delle GitHub Actions; lavoro residuo ordinato; eventuale richiesta human in the loop; prima azione concreta di ripresa.

## Esempi

Se un test fallisce, correggi codice o test e rilancialo: non creare un checkpoint solo per il fallimento. Se una GitHub Action della PR diventa rossa, leggi il log, correggi la causa, verifica localmente, crea un nuovo commit e push, quindi attendi il run relativo al nuovo SHA. Se l'utilizzo del contesto raggiunge il 35%, aggiorna il checkpoint con il comando fallito, l'errore utile e la prossima correzione concreta, quindi interrompi.

## Guardrail anti-regressione

Prima di implementare o correggere una modifica applica sempre questi controlli, emersi dai fix reali del repository:

- Non inventare valori Azure o .NET: runtime Flex, SKU, versioni provider, deployment name, model version, flag CLI e parametri devono essere verificati nella CLI corrente o nella documentazione ufficiale.
- Quando tocchi versioni o runtime, aggiorna nello stesso change tutti i consumer accoppiati: package .NET, Bicep/bicepparam, workflow, file generati e documentazione operativa.
- Per ogni rename di env var, app setting, parametro Bicep, secret, namespace o artifact name, esegui grep repository-wide e aggiorna codice, script, workflow, README, prompt e documentazione che lo consumano.
- Le modifiche ai workflow devono essere verificate contro i contratti reali del repository: path esistenti, artifact name, vars/secrets, permessi `GITHUB_TOKEN`, workflow riusabili, output e sintassi esatta dei comandi `az` tramite `--help`.
- Nei workflow di deploy mantieni un solo orchestratore su `main`, attivato esclusivamente da `infra/**`, `src/backend/**` e `src/frontend/**`. Infrastructure esegue solo Bicep, Backend applica migration e grant prima di One Deploy, Frontend distribuisce solo la SPA; nei commit misti il provisioning precede gli scope applicativi modificati.
- Su Azure Functions Flex usa `functionAppConfig` come fonte primaria per runtime, deployment storage, scala e concorrenza; non duplicare la stessa configurazione con app setting legacy se la piattaforma non li richiede.
- Le connessioni identity-based dello storage host Functions devono restare non ambigue: usa `accountName` oppure gli URI espliciti richiesti, mai entrambi; allinea anche i ruoli blob/queue/table realmente necessari.
- I bundle EF e l'automazione migration devono partire dal design-time factory/progetto autorevole. Se tocchi migration runner, Dockerfile, startup project o quoting SQL/KQL nei workflow, riesegui packaging e validazione end-to-end.
- Se modifichi una fonte che genera output versionati, rigenera e valida subito i file derivati invece di correggerli manualmente.
- Le integrazioni cloud opzionali devono degradare in modo esplicito quando mancano setting richiesti; non introdurre bootstrap crash in locale o in dev per exporter/servizi opzionali.
- Quando aggiungi una HTTP Function, aggiorna nella stessa modifica `openapi.yaml` con route, verbo, security, parametri, risposte e Problem Details applicabili; esegui `npm run skills:validate`, che fallisce se una route Function non e documentata.

## Dipendenze

Dipende dal contesto autorevole della richiesta, dalla Definition of Done del repository, dalle skill tecniche pertinenti, da Git, dal remote GitHub e da `gh` autenticato.

## Vincoli

Gli unici arresti ammessi sono utilizzo del contesto almeno al 35% e human in the loop realmente necessario. Non fermarti con documentazione incompleta, verifiche applicabili fallite o GitHub Actions della PR queued, in progress o non concluse con `success` sull'ultimo SHA. Se il lavoro porta una feature da `In progress` a `In review`, considera obbligatori commit, push, pull request aperta e monitoraggio fino al verde dell'ultimo SHA prima di trattare la consegna come completa, salvo blocco reale di credenziali o autorizzazioni. Non inserire secret o PII nel checkpoint, non includere modifiche estranee nel commit e non eseguire mai merge della pull request. Ogni pull request parte dal branch sorgente `dev` ed e destinata a `main`.

## Test richiesti

Esegui tutte le verifiche richieste dalla modifica e da `AGENTS.md`. Prima della consegna verifica almeno i validatori dei tool interessati e lo stato Git; build, test, lint, packaging e validazioni applicabili devono passare. Se tocchi workflow, runtime, observability, deploy o migration, verifica anche lo stato live risultante quando il repository e il contesto Azure lo consentono, includendo almeno runtime effettivo, smoke test health/version e ingestione telemetrica attesa. Dopo il push monitora i check della PR fino a esito terminale e accetta solo `success` per tutte le GitHub Actions attivate sull'ultimo commit.

## Checklist di aggiornamento

Leggi gli artefatti e l'eventuale checkpoint; verifica di lavorare su `dev`; implementa la modifica richiesta; aggiorna codice, test, documentazione, traduzioni, guide, skill e fragment applicabili; ripeti le verifiche fino al successo; se la feature passa a `In review`, non fermarti allo stato locale ma continua con diff e stato Git, commit e push su `dev`, apertura della PR verso `main` e monitoraggio dei check; per ogni esito non verde correggi, verifica, committa e pusha di nuovo; rimuovi il checkpoint solo quando tutti i check sono verdi; non eseguire il merge.

## Changelog

0.8.0: separo provisioning Azure, migration/deploy backend e deploy frontend in workflow riusabili attivati dai rispettivi path, mantenendo l'ordine nei commit misti.

0.7.0: richiedo l'aggiornamento di `openapi.yaml` per ogni HTTP Function e rendo obbligatoria la verifica automatica della copertura delle route.

0.6.0: definisco ownership, precedenza e serializzazione dei deploy path-based su `main`, incluse migration e modifiche miste.

0.5.0: rendo esplicito che il passaggio di una feature a `In review` non conclude il lavoro senza commit, push, pull request aperta e GitHub Actions verdi sull'ultimo SHA.

0.4.1: ricordo di includere i validator frontend introdotti dal design system condiviso quando la modifica tocca shell, route, componenti o CSS del frontend.

0.4.0: aggiungo guardrail anti-regressione per versioni/runtime Azure, workflow, rename configurativi, Flex Consumption, storage identity-based, EF bundle, artefatti generati e verifiche live post-deploy.

0.3.0: estendo la skill a qualsiasi modifica del repository, non solo a nuove feature, definisco la posizione del checkpoint fuori backlog e rendo obbligatori commit, push, PR e monitoraggio Actions per fix, workflow e aggiornamenti documentali versionati.

0.2.0: imposto `dev` come branch sorgente obbligatorio di ogni pull request verso `main` e richiedo l'esito verde di tutte le GitHub Actions della PR, con ciclo obbligatorio di diagnosi, correzione e push per ogni run non riuscito.

0.1.0: introdotti continuita obbligatoria, checkpoint al 35% o human in the loop e consegna tramite pull request senza merge.
