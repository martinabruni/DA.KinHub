# FEAT-013 - Eliminare in sicurezza i dati inattivi

- **Codice**: `cleanup-dati-inattivi`
- **Tipo**: `operational`
- **Readiness**: `blocked`
- **Wave**: 7
- **Risultato**: il run giornaliero elimina in modo limitato solo utenti, membership, famiglie e dati collegati inattivi da almeno 30 periodi di 24 ore e privi di legami attivi.

## Contesto autonomo

Il cleanup lifecycle è distinto dalla retention item. Decorre da `InactiveAt`, ricontrolla inattività continua e assenza di collegamenti attivi e deve rispettare l'ordine delle foreign key. Membership riattivate vengono saltate. Il timer introdotto da FEAT-012 tenta retention e cleanup come due casi indipendenti anche se uno fallisce, ma l'invocazione complessiva segnala fallimento se almeno uno fallisce.

## Scope

### Incluso

- Predisposizione soft delete user nello schema, senza endpoint/UI per attivarlo.
- Candidati user/membership/family/dati collegati con `InactiveAt <= cutoff`, pagine 5000 e chunk 1000.
- Ricontrollo stato, riattivazione e legami attivi prima di ogni delete.
- Ordine sicuro delle eliminazioni, transazioni limitate, ripresa idempotente e zero candidati riuscito.
- Secondo caso d'uso nel timer giornaliero, esiti/metriche separati e stato globale corretto.
- Runbook, alert, verifica/rollback e test su leave/riattivazione concorrente.

### Escluso

- Delete-account, hard delete durante leave, recupero utente della famiglia o nuovo servizio di cleanup.
- Retention degli item Completed in famiglie attive, già FEAT-012.

## Tracciabilità

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | FLOW-014 | Inattivazione e pulizia definitiva |
| Requisiti | FR-043 | Soglia, ricontrollo e cancellazione lifecycle |
| Regole/decisioni | BR-030, BR-031, BR-037; DEC-023, DEC-024, DEC-028, DEC-033; ASM-007 | Ciclo inattivo e job separato |
| Architettura | ADR-013, ADR-017; sezioni 6.9, 8, 9 | Soft delete, pagine/chunk e orchestrazione |

## Dipendenze

### Feature prerequisite

| Feature | Tipo | Motivo | Output richiesto | Effetto sul parallelismo |
|---|---|---|---|---|
| FEAT-006 - Lasciare la famiglia in sicurezza | hard | Produce `InactiveAt`, soft delete e lifecycle ultimo membro | Stati inattivi coerenti e dati collegati | Inizio dopo FEAT-006 |
| FEAT-012 - Eliminare gli item completati oltre retention | hard | Introduce timer e contratto di esiti distinti da estendere | Timer, run context e CP-005 | Inizio dopo FEAT-012 |

### Gate e assunzioni

| ID | Stato | Impatto | Evidenza per chiudere |
|---|---|---|---|
| GATE-002 | open, blocking | Semantica temporale privacy del cleanup | Conferma ASM-007 |
| TECH-003 | open | Ordini/cursori per tipo lifecycle | Query/indici testati |
| TECH-006 | open | Ordine FK, budget e limiti backup/PITR | Runbook e prove di cancellazione |

### Parallelismo consentito

Nessuno durante integrazione di timer e migration. Dopo CP-005, test e documentazione possono procedere separatamente senza modificare il contratto.

## Contratto di consegna

### Comportamento

- Il cutoff lifecycle parte da `InactiveAt`; nessun record attivo o riattivato viene eliminato.
- Prima del delete sono ricontrollati stato continuo e assenza di membership/legami attivi.
- Pagine e chunk restano entro 5000/1000 e seguono l'ordine FK verificato.
- Retention e cleanup vengono entrambi tentati; metriche/esiti restano distinti e il run globale fallisce se uno fallisce.
- La predisposizione user soft delete non crea un modo per l'utente di cancellare l'account.

### Touchpoint previsti

- **Dominio/business**: cutoff lifecycle, eligibility per tipo e coordinatore dei due casi.
- **Persistenza/migration**: soft delete user/shared, query storiche esplicite, delete ordinato e vincoli.
- **API/integrazioni**: Timer Trigger condiviso; nessun endpoint utente.
- **Frontend/UX**: Non pertinente; nessuna nuova UI.
- **Infrastruttura/configurazione**: host/opzioni/alert esistenti; nessuna nuova risorsa.
- **Documentazione/operazioni**: runbook lifecycle, backup/PITR scope, migration e change fragment.

### Errori, sicurezza e osservabilità

- Filtri ordinari escludono inattivi; join/cleanup usano percorsi espliciti per storici senza rendere accessibili i dati.
- Un errore lascia residui inattivi per il run successivo e non allarga il delete.
- Metriche per tipo includono candidati, eliminati, saltati per legami/riattivazione, falliti ed età massima, senza PII.

## Criteri di accettazione

### AC-072 - Soglia lifecycle corretta

- **Dato** record inattivi prima, esattamente e dopo 30 periodi di 24 ore da `InactiveAt`
- **Quando** esegue cleanup
- **Allora** solo quelli al cutoff o più vecchi possono essere candidati
- **Fonte**: FR-043, BR-031, DEC-024

### AC-073 - Riattivazione protegge dal delete

- **Dato** una membership storica riattivata con join valido prima della cancellazione
- **Quando** il chunk ricontrolla
- **Allora** membership, famiglia attiva e dati collegati non vengono eliminati
- **Fonte**: FR-043, FLOW-014, BR-031

### AC-074 - Collegamenti attivi proteggono dal delete

- **Dato** un candidato con membership o legami ancora attivi
- **Quando** viene valutato
- **Allora** è saltato e il motivo tecnico aggregato è osservabile
- **Fonte**: FR-043, BR-031

### AC-075 - Cancellazione limitata e ordinata

- **Dato** candidati di più tipi oltre una pagina
- **Quando** il cleanup procede
- **Allora** usa pagine massimo 5000 e chunk massimo 1000 nell'ordine FK verificato, senza `Get All`
- **Fonte**: FR-049, FR-050, ADR-013, ADR-017

### AC-076 - Due esiti indipendenti nello stesso run

- **Dato** retention riuscita e cleanup fallito, o viceversa
- **Quando** termina il timer
- **Allora** entrambi sono stati tentati, esiti/metriche restano separati e l'invocazione globale segnala fallimento
- **Fonte**: DEC-033, FR-030

### AC-077 - Nessun delete-account implicito

- **Dato** il modello user predisposto al soft delete
- **Quando** si esaminano API e UI pubbliche
- **Allora** non esiste alcun endpoint o controllo di cancellazione account
- **Fonte**: ADR-013, out of scope analisi sezione 5

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Cutoff, eligibility, run status combinato | Test business con clock controllato |
| Integrazione | Riattivazione concorrente, FK, pagine/chunk e filtri storici | Test PostgreSQL/job |
| Frontend/component | Non pertinente | Assenza verificata di UI delete-account |
| End-to-end/manuale | Leave ultimo -> soglia -> cleanup; join prima del delete; un caso job fallisce | DB e telemetria coerenti |
| Validator repository | Backend/build/test/publish/package, docs/release e Bicep se toccato | Esiti registrati |

## Definition of Done

- GATE-002, TECH-003 e TECH-006 chiusi; AC-072-AC-077 verificati.
- FEAT-006/012 integrate e CP-005 rispettato senza cicli o secondo timer.
- Migration/rollback, runbook, alert e test di riattivazione/legami attivi completi.
- Telemetria redatta e change fragment aggiornato; comandi applicabili di `AGENTS.md` eseguiti.
- Nessun endpoint delete-account, hard delete sincrono, nuova risorsa o confusione con retention item.
