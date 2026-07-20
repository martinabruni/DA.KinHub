# FEAT-010 - Completare un item e annullare

- **Codice**: `completamento-singolo`
- **Tipo**: `product`
- **Readiness**: `ready`
- **Wave**: 5
- **Risultato**: un membro completa un item con feedback immediato e può riattivarlo entro cinque secondi senza duplicati o riordino.

## Contesto autonomo

Il completamento singolo è una transizione persistita subito, non un'attesa client. La riga scompare ottimisticamente, il server registra stato, autore, `CompletedAt` ed evento. Un command ID rende il retry idempotente. Undo è un secondo comando atomico, accettato nella finestra di cinque secondi più il solo margine tecnico di latenza; produce `Riattivato` e riusa la chiave d'ordine originaria.

## Scope

### Incluso

- Spunta singola, rimozione ottimistica e ripristino UI su errore.
- Comando idempotente e condizionato da famiglia, visibility, Active e versione.
- Stato Completed, autore/momento e timeline nello stesso commit.
- Snackbar accessibile per cinque secondi; azioni singole ravvicinate restano distinte.
- Undo condizionato, evento `Riattivato`, posizione originaria e stato effettivo su scadenza/errore.
- Telemetria redatta, conflitti e refresh manuale.

### Escluso

- Schermata completati, undo dopo scadenza, ritardo della persistenza o fusione implicita in bulk.
- Retention fisica, trattata da FEAT-012.

## Tracciabilità

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | FLOW-007 | Completamento e Annulla |
| Requisiti | FR-024, FR-025 | Transizione e ripristino |
| Regole/decisioni | BR-010-BR-012, BR-020; DEC-006-DEC-008 | Finestra, evento e azioni distinte |
| Architettura | ADR-006-ADR-008, ADR-014; sezione 6.6 | Idempotenza, timeline e ordine |

## Dipendenze

### Feature prerequisite

| Feature | Tipo | Motivo | Output richiesto | Effetto sul parallelismo |
|---|---|---|---|---|
| FEAT-009 - Correggere un item e consultarne la storia | hard | Completion e undo scrivono timeline e usano versione concorrente | Tipi evento, modello versione e transazione item+timeline | Inizio dopo FEAT-009 |

### Gate e assunzioni

Nessuno. Il margine server resta tecnico e non estende la finestra visibile approvata.

### Parallelismo consentito

Con FEAT-006 dopo CP-003. Coordinare migration item/timeline e le superfici inferiori frontend.

## Contratto di consegna

### Comportamento

- La riga scompare subito; se il comando fallisce riappare nello stato autorevole.
- Successo ripetuto con lo stesso command ID non duplica stato o evento.
- Undo valido riattiva item ed evento insieme e lo reinserisce secondo ordine persistito.
- Scadenza, conflitto o rete incerta non producono un falso ripristino; la PWA aggiorna lo stato.
- Ogni completamento singolo ravvicinato conserva il proprio feedback/undo.

### Touchpoint previsti

- **Dominio/business**: transizioni Active/Completed, finestra undo, idempotenza.
- **Persistenza/migration**: `CompletedAt`, command records, versione e timeline transazionale.
- **API/integrazioni**: complete/undo con `Family`, version/command ID e Problem Details.
- **Frontend/UX**: riga lista, coda snackbar accessibile, focus e riconciliazione.
- **Infrastruttura/configurazione**: opzione undo validata; nessuna risorsa.
- **Documentazione/operazioni**: guida completamento, metriche e change fragment.

### Errori, sicurezza e osservabilità

- Scope e visibility sono ricontrollati per entrambi i comandi; ID diretto non rivela item invisibili.
- Retry, conflitto, scadenza e dipendenza hanno codici stabili; nessun contenuto nei log.
- Metriche misurano durata, successo, rollback, duplicate command, conflitto e undo accettato/scaduto.

## Criteri di accettazione

### AC-056 - Completamento persistito

- **Dato** un item Active visibile con versione corrente
- **Quando** il membro lo spunta
- **Allora** scompare, diventa Completed con autore/momento ed evento e compare Annulla per cinque secondi
- **Fonte**: FR-024, BR-010

### AC-057 - Retry idempotente

- **Dato** lo stesso command ID inviato più volte
- **Quando** il primo commit è riuscito
- **Allora** le risposte rappresentano lo stesso esito senza eventi o transizioni duplicate
- **Fonte**: BR-010, NFR-006

### AC-058 - Undo atomico e ordinato

- **Dato** un completamento riuscito ancora nella finestra
- **Quando** Annulla viene accettato
- **Allora** item ed evento Riattivato sono committati insieme e la riga torna nella posizione originaria
- **Fonte**: FR-025, BR-011, DEC-007

### AC-059 - Scadenza o errore onesti

- **Dato** finestra scaduta, rete incerta o comando non più valido
- **Quando** il membro usa Annulla
- **Allora** la UI non dichiara il ripristino, mostra lo stato effettivo e non duplica la riga
- **Fonte**: BR-012, FLOW-007

### AC-060 - Singoli ravvicinati distinti e accessibili

- **Dato** più item completati con azioni singole rapide
- **Quando** compaiono i feedback
- **Allora** ogni azione conserva riferimento e undo, con focus/annunci accessibili e senza diventare bulk
- **Fonte**: BR-020, DEC-008, NFR-009

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Transizioni, cutoff undo e idempotenza | Test dominio/business con clock controllato |
| Integrazione | Commit item+timeline, retry/conflitto/scadenza | Test PostgreSQL/API |
| Frontend/component | Optimistic UI, coda snackbar, focus/errori | Test componente/accessibilità |
| End-to-end/manuale | Complete -> undo, timeout e perdita rete | Stato coerente e ordine stabile |
| Validator repository | Qualità completa e package | Esiti registrati |

## Definition of Done

- AC-056-AC-060 verificati, FEAT-009 integrata e CP-003 pubblicato.
- Migration/rollback, clock e margine tecnico documentati; nessun segreto/contenuto nei log.
- UI, i18n, accessibilità, temi, guida/help, telemetria e fragment completi.
- Comandi applicabili di `AGENTS.md` eseguiti.
- Nessuna schermata completati, undo tardivo o aggregazione implicita introdotta.
