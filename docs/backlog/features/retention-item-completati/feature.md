# FEAT-012 - Eliminare gli item completati oltre retention

- **Codice**: `retention-item-completati`
- **Tipo**: `operational`
- **Readiness**: `blocked`
- **Wave**: 6
- **Risultato**: ogni giorno KinList elimina in modo limitato solo gli item ancora completati da almeno 30 periodi di 24 ore, con esito retention verificabile.

## Contesto autonomo

La retention degli item Completed decorre da `CompletedAt`, non da ultima modifica o inattivazione famiglia. Un Timer Trigger giornaliero alle 00:00 UTC, senza `RunOnStartup`, acquisisce una volta `nowUtc`, legge pagine keyset massimo 5000 ed elimina in chunk massimo 1000 dopo aver ricontrollato stato/cutoff. Timeline e dati collegati vengono cancellati coerentemente; categorie ancora usate restano. Gli elementi non processati restano idonei.

## Scope

### Incluso

- Caso d'uso retention distinto e primo coordinamento timer giornaliero.
- Cutoff inclusivo dopo 30 periodi continuativi di 24 ore da `CompletedAt`.
- Pagine keyset, chunk limitati, ricontrollo condizionato e ripresa idempotente.
- Cancellazione coerente item/timeline/collegati e conservazione categorie usate.
- Esito zero candidati, errori parziali tra lotti sicuri, metriche/alert/runbook senza PII.
- Contratto timer predisposto a invocare separatamente cleanup FEAT-013.

### Escluso

- Schermata completati, cancellazione anticipata, soft delete lifecycle o cleanup utenti/famiglie.
- Garanzie su backup/PITR oltre il dominio applicativo.

## Tracciabilità

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | FLOW-008 | Retention item completati |
| Requisiti | FR-026 | Cutoff, controllo e ripresa |
| Regole/decisioni | BR-013, BR-014, BR-022, BR-037; DEC-012, DEC-028, DEC-033; ASM-007 | Cancellazione condizionata e osservabile |
| Architettura | ADR-008, ADR-017; sezioni 6.9, 9 | Timer, pagine/chunk e metriche |

## Dipendenze

### Feature prerequisite

| Feature | Tipo | Motivo | Output richiesto | Effetto sul parallelismo |
|---|---|---|---|---|
| FEAT-010 - Completare un item e annullare | hard | Retention dipende da stato Completed, `CompletedAt`, undo e timeline | Modello completion e query condizionabile | Inizio dopo FEAT-010 e chiusura GATE-002 |

### Gate e assunzioni

| ID | Stato | Impatto | Evidenza per chiudere |
|---|---|---|---|
| GATE-002 | open, blocking | Definisce la garanzia temporale privacy oltre la soglia minima | Conferma ASM-007 registrata |
| TECH-003 | open | Cursore/ordine candidati | Query e indice testati |
| TECH-006 | open | Budget host, FK e backup/PITR | Runbook/test operativi |

### Parallelismo consentito

Con FEAT-011 dopo CP-004, serializzando migration/repository item. Pubblicare CP-005 per FEAT-013.

## Contratto di consegna

### Comportamento

- Il timer `0 0 0 * * *` non parte all'avvio e usa un unico `nowUtc` per il run.
- Solo item ancora Completed con `CompletedAt <= cutoff` possono essere eliminati.
- Un item riattivato o non più idoneo viene saltato al ricontrollo.
- Zero candidati è successo; un errore non allarga la cancellazione e gli elementi rimasti sono ripresi in run successivi.
- Retention ha esito/metriche propri, separati dal cleanup lifecycle.

### Touchpoint previsti

- **Dominio/business**: cutoff e caso d'uso retention paginato.
- **Persistenza/migration**: query keyset, delete condizionato/chunk e cascade applicativa verificata.
- **API/integrazioni**: Timer Trigger nell'host Functions; nessun endpoint utente.
- **Frontend/UX**: Non pertinente: nessuna UI completati.
- **Infrastruttura/configurazione**: estensione timer/opzioni nell'host condiviso; nessuna nuova Function App.
- **Documentazione/operazioni**: runbook, verifica/rollback migration, metriche/alert e change fragment.

### Errori, sicurezza e osservabilità

- Log/trace includono run ID, cutoff, pagine/chunk, conteggi/durate/categorie errore, mai contenuti.
- Un chunk fallito non causa delete indiscriminato; retry è idempotente sui residui.
- Alert distinguono ritardo, fallimento e backlog oltre soglia; nessun falso successo globale.

## Criteri di accettazione

### AC-067 - Nessuna cancellazione anticipata

- **Dato** item Completed prima, esattamente e dopo il cutoff inclusivo
- **Quando** esegue retention
- **Allora** solo quelli al cutoff o più vecchi sono candidati e lo stato viene ricontrollato
- **Fonte**: FR-026, BR-013, DEC-012

### AC-068 - Pagine e chunk limitati

- **Dato** candidati oltre 5000
- **Quando** il job procede entro il budget
- **Allora** legge pagine massimo 5000 ed elimina chunk massimo 1000 senza `Get All`
- **Fonte**: FR-026, FR-049, FR-050, ADR-017

### AC-069 - Riattivato non eliminato

- **Dato** un candidato tornato Active prima del delete
- **Quando** il chunk ricontrolla le condizioni
- **Allora** l'item e la timeline restano disponibili
- **Fonte**: BR-013, FLOW-008

### AC-070 - Cancellazione collegata coerente

- **Dato** un item ancora idoneo con timeline e categorie
- **Quando** il delete riesce
- **Allora** item e dati collegati previsti spariscono insieme, mentre categorie usate da altri restano
- **Fonte**: BR-022, ADR-008

### AC-071 - Esito operativo separato e riprendibile

- **Dato** zero candidati, fallimento di un lotto o residui oltre budget
- **Quando** termina il run
- **Allora** retention espone esito/conteggi/durata redatti e i residui restano idonei al run successivo
- **Fonte**: FR-026, FR-030, DEC-033, NFR-007

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Cutoff inclusivo e clock singolo | Test con `TimeProvider` |
| Integrazione | Pagine/chunk, ricontrollo, FK e retry | Test PostgreSQL/job |
| Frontend/component | Non pertinente | Nessuna UI prevista |
| End-to-end/manuale | Run zero, run parziale, item riattivato | Log/metriche/DB coerenti |
| Validator repository | Backend test/build/publish/package, docs/release e Bicep se toccato | Esiti registrati |

## Definition of Done

- GATE-002, TECH-003 e TECH-006 chiusi; AC-067-AC-071 verificati.
- FEAT-010 integrata e CP-004/CP-005 congelati.
- Timer non usa `RunOnStartup`; migration/rollback e runbook includono verifica e ripresa.
- Telemetria/alert redatti e change fragment aggiornati.
- Comandi applicabili di `AGENTS.md` eseguiti.
- Nessuna UI, cancellazione anticipata, nuova risorsa o mescolanza semantica col lifecycle.
