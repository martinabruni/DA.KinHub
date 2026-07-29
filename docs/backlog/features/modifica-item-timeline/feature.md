---
status: Open
---

# FEAT-009 - Correggere un item e consultarne la storia

- **Codice**: `modifica-item-timeline`
- **Tipo**: `product`
- **Readiness**: `ready`
- **Wave**: 5
- **Risultato**: un membro apre un drawer, corregge nome/categorie con salvataggio esplicito e consulta una timeline paginata coerente.

## Contesto autonomo

Tutti i membri possono modificare gli Shared. Il drawer da destra mostra metadati, categorie e timeline. Solo nome e categorie sono modificabili; owner, visibility e ordine restano stabili. Una versione attesa impedisce sovrascritture silenziose. Nessun cambiamento significa nessun aggiornamento o evento.

## Scope

### Incluso

- Drawer accessibile e responsive con nome, categorie multiple, nuova categoria, autore e date read-only.
- Prime pagine e navigazione avanti/indietro di catalogo categorie e timeline.
- Salvataggio esplicito atomico di item, associazioni categoria ed evento `Modificato` solo se cambia qualcosa.
- Timeline append-only con tipi approvati Creazione, Modifica, Completamento e Riattivazione, autore e data/ora.
- Concorrenza ottimistica, conflitto esplicito, refresh e riapplicazione consapevole.
- Validazioni inline, input preservato e posizione riga invariata.

### Escluso

- Autosave, cambio owner/visibility, fusione automatica, nuovi tipi evento o audit forense.
- Completamento/undo effettivi, trattati da FEAT-010/011.

## Tracciabilità

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | FLOW-006 | Dettaglio, modifica e cronologia |
| Requisiti | FR-019-FR-023 | Drawer, campi, timeline e conflitto |
| Regole/decisioni | BR-004, BR-009, BR-017, BR-018, BR-037, BR-038; DEC-005, DEC-007, DEC-014, DEC-028 | No-op, save esplicito e refresh |
| Architettura | ADR-007, ADR-014, ADR-017; sezione 6.5 | Stato corrente + timeline e versione |

## Dipendenze

### Feature prerequisite

| Feature | Tipo | Motivo | Output richiesto | Effetto sul parallelismo |
|---|---|---|---|---|
| FEAT-003 - Consultare la lista condivisa paginata | hard | Dettaglio usa item, visibility, ordine, pagina e versione | Modello item e scope server | Inizio dopo FEAT-003 |
| FEAT-014 - Usare un design system condiviso in tutta KinHub | hard | Drawer, form e timeline devono riusare overlay, field e row component condivisi | Primitive drawer/form/state e regole di wrapper specifici del design system | Inizio dopo integrazione FEAT-014 |

### Gate e assunzioni

Nessuno. TECH-003 va completato per categorie e timeline senza cambiare lo scope.

### Parallelismo consentito

Con FEAT-005/007/008 dopo CP-002. Pubblicare CP-003 prima che FEAT-007/010 implementino eventi o transizioni.

## Contratto di consegna

### Comportamento

- Aprire il drawer carica dettaglio e prime pagine senza spostare la riga.
- Il client invia solo nome, categorie, eventuale categoria nuova e versione attesa.
- Il server deriva attore/tempo, riusa categorie equivalenti e committa cambiamento+evento insieme.
- Un salvataggio identico non cambia date/versione/timeline.
- Un conflitto non sovrascrive; mostra stato aggiornato e richiede nuova azione consapevole.

### Touchpoint previsti

- **Dominio/business**: nome/categoria, no-op, evento e conflitto.
- **Persistenza/migration**: timeline append-only, associazioni, token versione e query paged.
- **API/integrazioni**: detail/update con `Family`, Problem Details conflitto e cursori.
- **Frontend/UX**: drawer, form, pagine, focus e i18n costruiti sui componenti overlay/form del design system condiviso.
- **Infrastruttura/configurazione**: Nessuna.
- **Documentazione/operazioni**: guida modifica/timeline e change fragment.

### Errori, sicurezza e osservabilità

- Visibility e membership sono riapplicate a dettaglio, timeline, categorie e scrittura.
- Errori campo restano locali; errori generali preservano input; `403` non rivela l'item.
- Log/metriche usano ID tecnici, esito, durata e conflitto, non nomi/categorie.

## Criteri di accettazione

### AC-050 - Drawer e metadati

- **Dato** un item attivo visibile
- **Quando** il membro lo apre
- **Allora** un drawer da destra mostra nome, categorie e autore/date read-only con focus gestito
- **Fonte**: FR-019, FR-021

### AC-051 - Salvataggio esplicito atomico

- **Dato** modifiche valide a nome/categorie o nuova categoria
- **Quando** il membro salva
- **Allora** item, associazioni ed evento cambiano insieme e la posizione resta invariata
- **Fonte**: FR-020, BR-017, FR-015

### AC-052 - Nessun evento vuoto

- **Dato** dati identici allo stato corrente
- **Quando** il membro salva
- **Allora** versione, ultima modifica e timeline non cambiano
- **Fonte**: BR-009, ADR-007

### AC-053 - Timeline paginata approvata

- **Dato** eventi oltre una pagina
- **Quando** il membro naviga
- **Allora** vede Creazione/Modifica/Completamento/Riattivazione in ordine stabile con autore/data, senza caricamento integrale
- **Fonte**: FR-022, FR-049, DEC-007

### AC-054 - Conflitto esplicito

- **Dato** l'item cambiato dopo l'apertura
- **Quando** si salva con versione precedente
- **Allora** nessun dato viene sovrascritto, input è preservato e si offre refresh/ripetizione consapevole
- **Fonte**: FR-023, BR-018, DEC-014

### AC-055 - Scope su tutte le superfici

- **Dato** item Personal altrui o di altra famiglia
- **Quando** si richiede dettaglio, timeline o modifica
- **Allora** nessun contenuto è restituito e l'operazione è negata
- **Fonte**: FR-003, FR-047, BR-036

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | No-op, validazioni, eventi e conflitto | Test dominio/business |
| Integrazione | Transazione, versioning, pagine e no leak | Test PostgreSQL/API |
| Frontend/component | Drawer, focus, form, errori e pagine | Test componente/accessibilità |
| End-to-end/manuale | Due membri modificano; categoria nuova/equivalente | Conflitto senza perdita |
| Validator repository | Qualità, i18n/docs/release e package | Esiti registrati |

## Definition of Done

- AC-050-AC-055 verificati, FEAT-003 integrata e CP-003 congelato.
- Drawer, form e timeline usano componenti FEAT-014 e non mantengono overlay o field legacy paralleli.
- TECH-003 chiuso per categorie/timeline e migration/rollback documentati.
- Drawer, i18n, accessibilità, temi, help/guida, telemetria e fragment completi.
- Comandi applicabili di `AGENTS.md` eseguiti.
- Nessun autosave, merge implicito, cambio visibility/owner o evento fuori scope.
