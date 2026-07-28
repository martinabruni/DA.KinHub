---
status: Open
---

# FEAT-011 - Completare una selezione come unico gruppo

- **Codice**: `completamento-multiplo`
- **Tipo**: `product`
- **Readiness**: `ready`
- **Wave**: 6
- **Risultato**: un membro completa fino a 5000 item della pagina filtrata come un solo esito atomico e può annullare l'intero gruppo entro cinque secondi.

## Contesto autonomo

La modalità Seleziona è esplicita e disponibile solo con item attivi visibili. `Seleziona tutti` riguarda esclusivamente la pagina filtrata corrente. Il browser invia un comando unico con ID distinti e versioni; il repository processa chunk massimi 1000 nella stessa transazione. Un solo elemento non valido causa rollback totale. Il successo produce un unico `Annulla N`, mai N feedback.

## Scope

### Incluso

- Entrata/uscita modalità selezione, checkbox, conteggio e `Completa N`.
- Selezione singola o di tutti i soli item della pagina filtrata corrente, senza duplicati, massimo 5000.
- Riconciliazione della selezione al cambio filtro/pagina.
- Comando idempotente atomico con versione per item, validation completa e chunk repository 1000 nella stessa transazione.
- Rimozione simultanea, unico snackbar `Annulla N` e undo atomico dell'intero comando.
- Errori/conflitti senza successo parziale e refresh autorevole.

### Escluso

- Selezione attraverso pagine non caricate, successo parziale, undo per singolo item bulk o N richieste HTTP.

## Tracciabilità

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | FLOW-005, FLOW-013 | Selezione, completa N e undo N |
| Requisiti | FR-044-FR-046 | Modalità, pagina e atomicità |
| Regole/decisioni | BR-018, BR-032-BR-034; DEC-014, DEC-025, DEC-028 | Conflitto, limite e undo unico |
| Architettura | ADR-014, ADR-015, ADR-017; sezione 6.6 | Scope, chunk e transazione |

## Dipendenze

### Feature prerequisite

| Feature | Tipo | Motivo | Output richiesto | Effetto sul parallelismo |
|---|---|---|---|---|
| FEAT-008 - Filtrare la lista per categoria | hard | Definisce esattamente la pagina filtrata di Seleziona tutti | Filtro/pagina e reset coerente | Inizio dopo FEAT-008 |
| FEAT-010 - Completare un item e annullare | hard | Riusa transizioni, timeline, idempotenza e finestra undo | Stato/eventi/comandi completion | Inizio dopo FEAT-010 |

### Gate e assunzioni

| ID | Stato | Impatto | Evidenza per chiudere |
|---|---|---|---|
| TECH-005 | open | Verifica sostenibilità della transazione massima | Test reale 5000/5 chunk, contesa e rollback |

### Parallelismo consentito

Con FEAT-012 dopo CP-004, ma una sola feature possiede migration/repository item alla volta. Non modificare snackbar/list selection condivisi senza coordinamento.

## Contratto di consegna

### Comportamento

- La modalità non appare senza item; uscire con Annulla selezione non cambia dati.
- `Seleziona tutti` prende la pagina corrente dopo filtro e visibilità, mai altre pagine.
- Tutti gli item sono validati prima/durante la transazione; un conflitto/invisibilità/stato invalido annulla ogni chunk.
- Il successo rimuove tutte le righe e offre un solo undo; il fallimento dichiara che nessun item è cambiato.
- Undo N riattiva tutti o nessuno nelle posizioni originarie.

### Touchpoint previsti

- **Dominio/business**: command group, validation completa e undo atomico.
- **Persistenza/migration**: command record, chunk 1000, singola transazione e timeline.
- **API/integrazioni**: endpoint complete/undo bulk con `Family`, ID/versioni e limite.
- **Frontend/UX**: lista/filtro, checkbox, conteggio, focus, unico snackbar e stati vuoti.
- **Infrastruttura/configurazione**: opzioni write max validate; nessuna risorsa.
- **Documentazione/operazioni**: guida bulk, verifica performance e change fragment.

### Errori, sicurezza e osservabilità

- Item Personal altrui, altra famiglia, duplicato, stale o non Active annulla tutto senza rivelare quale dato non consentito esista.
- Payload vuoto/oltre pagina e configurazione oltre 1000 chunk/5000 comando sono rifiutati.
- Metriche: cardinalità, chunk, durata, rollback, conflitto e undo, senza contenuti item.

## Criteri di accettazione

### AC-061 - Modalità esplicita

- **Dato** almeno un item attivo visibile
- **Quando** il membro preme Seleziona
- **Allora** compaiono checkbox, conteggio e sole azioni essenziali; Annulla selezione non modifica item
- **Fonte**: FR-044, NFR-001

### AC-062 - Tutti significa pagina filtrata

- **Dato** più pagine e un filtro attivo
- **Quando** usa Seleziona tutti
- **Allora** sono inclusi solo gli item visibili della pagina corrente, senza duplicati e fino a 5000
- **Fonte**: FR-045, BR-033

### AC-063 - Completamento tutti-o-nessuno

- **Dato** una selezione valida
- **Quando** conferma Completa N
- **Allora** tutti gli item, metadati e timeline cambiano in una transazione unica oppure nessuno cambia
- **Fonte**: FR-046, BR-032

### AC-064 - Conflitto o item invisibile

- **Dato** un item selezionato completato, cambiato o non più visibile prima del commit
- **Quando** arriva il comando
- **Allora** l'intero bulk fallisce senza leak, la lista si aggiorna e la selezione non viene riapplicata alla cieca
- **Fonte**: FR-023, FLOW-013, ADR-015

### AC-065 - Cinque chunk, un esito

- **Dato** una pagina di 5000 item
- **Quando** viene completata e un chunk intermedio riesce o fallisce
- **Allora** al massimo cinque chunk da 1000 condividono una transazione; successo totale o rollback totale
- **Fonte**: FR-045, FR-046, ADR-015

### AC-066 - Undo N unico e atomico

- **Dato** un bulk completato con successo
- **Quando** il membro usa Annulla N entro cinque secondi
- **Allora** tutti gli item tornano attivi nelle rispettive posizioni oppure nessuno, con un solo feedback
- **Fonte**: FR-046, BR-034

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Selezione, limiti, validazione e undo group | Test business |
| Integrazione | 5000/5 chunk, failure injection, idempotenza e no leak | Test PostgreSQL/API reale |
| Frontend/component | Modalità, filtro, conteggio, snackbar e accessibilità | Test componenti |
| End-to-end/manuale | Conflitto concorrente, ultimi item e undo N | Esito unico osservabile |
| Validator repository | Qualità completa, package e migration | Esiti registrati |

## Definition of Done

- AC-061-AC-066 e TECH-005 verificati; FEAT-008/010 integrate e CP-004 congelato.
- Test reale dimostra rollback totale a 5000 e nessun leak Personal/cross-family.
- Migration/rollback, UI, i18n, accessibilità, help/guida, telemetria e fragment completi.
- Comandi applicabili di `AGENTS.md` eseguiti.
- Nessun successo parziale, selezione cross-page o undo individuale introdotto.
