# FEAT-006 - Lasciare la famiglia in sicurezza

- **Codice**: `uscita-famiglia`
- **Tipo**: `product`
- **Readiness**: `ready`
- **Wave**: 5
- **Risultato**: un membro conferma l'uscita, perde subito accesso e torna all'onboarding senza lasciare inviti o stati parziali.

## Contesto autonomo

L'uscita rende inattiva la membership e revoca gli inviti creati dal membro. Se era l'ultimo membro attivo, la stessa transazione inattiva famiglia e dati KinList collegati. Non avviene hard delete nella richiesta e non esiste recupero utente diretto; un nuovo codice valido può riattivare una membership storica.

## Scope

### Incluso

- Conferma accessibile con conseguenze chiare e annullamento senza effetti.
- Soft delete membership, revoca inviti propri e invalidazione immediata del contesto.
- Rilevamento ultimo membro e soft delete atomico di famiglia/dati KinList collegati.
- Ritorno onboarding, `403` su accessi successivi e owner storico invariato se restano membri.
- Timestamp `InactiveAt`, filtri ordinari dei record inattivi e predisposizione cleanup FEAT-013.

### Escluso

- Rimozione di altri membri, cancellazione account, hard delete sincrono o ripristino UI della famiglia.

## Tracciabilità

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | FLOW-012 | Uscita e ultimo membro |
| Requisiti | FR-041, FR-042 | Revoca accesso e inattivazione famiglia |
| Regole/decisioni | BR-029-BR-031; DEC-022-DEC-024 | Soft delete e ritorno onboarding |
| Architettura | ADR-013; sezioni 6.8, 8 | Transazione lifecycle e scope inattivo |

## Dipendenze

### Feature prerequisite

| Feature | Tipo | Motivo | Output richiesto | Effetto sul parallelismo |
|---|---|---|---|---|
| FEAT-005 - Invitare e unirsi con un codice | hard | Leave deve revocare inviti del membro e preservare la riattivazione storica | Schema inviti/membership e join di riattivazione | Inizio dopo FEAT-005 |

### Gate e assunzioni

Nessuno. L'ordine fisico di cleanup è rinviato a FEAT-013; questa feature deve solo produrre soft delete coerente.

### Parallelismo consentito

Con FEAT-010. Coordinare migration item/shared e non eseguire migration concorrenti sulla stessa sequenza EF.

## Contratto di consegna

### Comportamento

- Senza conferma non cambia nulla; con conferma membership e inviti propri cambiano nello stesso commit.
- Dopo il commit la PWA elimina il contesto in memoria, non conserva dati e torna all'onboarding.
- Se restano membri, famiglia e dati restano attivi con owner invariati; se non restano, famiglia e dati KinList diventano inattivi nello stesso commit.
- Retry/concorrenza restituiscono lo stato autorevole senza doppie transizioni.

### Touchpoint previsti

- **Dominio/business**: leave, conteggio membri attivi, transizioni soft delete e revoca.
- **Persistenza/migration**: shared/kinlist, `InactiveAt`, filtri e transazione multi-entità.
- **API/integrazioni**: endpoint `Family`, idempotenza e Problem Details.
- **Frontend/UX**: azione Family, dialog conferma, invalidazione stato e redirect onboarding.
- **Infrastruttura/configurazione**: Nessuna.
- **Documentazione/operazioni**: guida conseguenze/riattivazione, migration e change fragment.

### Errori, sicurezza e osservabilità

- Nessuna uscita parziale; un errore lascia l'utente nello stato effettivo e non finge il redirect.
- Dopo l'uscita policy e repository negano subito i dati; nessun contenuto compare nei log.
- Metriche distinguono leave ultimo/non ultimo, rollback e inviti revocati in forma aggregata.

## Criteri di accettazione

### AC-031 - Conferma obbligatoria

- **Dato** un membro nella pagina Family
- **Quando** sceglie Lascia famiglia e annulla
- **Allora** membership, inviti e navigazione restano invariati
- **Fonte**: FR-041, DEC-022

### AC-032 - Uscita atomica

- **Dato** un membro con inviti attivi propri
- **Quando** conferma l'uscita
- **Allora** membership e inviti propri diventano inattivi/revocati nello stesso commit e l'utente torna all'onboarding
- **Fonte**: FR-041, BR-029

### AC-033 - Ultimo membro

- **Dato** l'unico membro attivo
- **Quando** lascia la famiglia
- **Allora** membership, famiglia e dati KinList collegati diventano inattivi atomicamente
- **Fonte**: FR-042, BR-030, DEC-023

### AC-034 - Famiglia con altri membri

- **Dato** una famiglia con più membri attivi
- **Quando** uno lascia
- **Allora** dati e owner restano stabili per gli altri e l'ex membro non può più accedere
- **Fonte**: FLOW-012, BR-029, ADR-013

### AC-035 - Errore senza stato parziale

- **Dato** un guasto o cambiamento concorrente durante leave
- **Quando** la transazione non può completare
- **Allora** lo stato autorevole è preservato, la UI non finge l'uscita e offre recupero sicuro
- **Fonte**: FLOW-012, NFR-006

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Ultimo/non ultimo, revoche e transizioni | Test dominio/business |
| Integrazione | Transazione, filtri soft delete e accesso post-leave | Test PostgreSQL/API |
| Frontend/component | Conferma, errori, redirect e focus | Test accessibilità/stato |
| End-to-end/manuale | Leave con altri membri e ultimo membro | Nessun accesso o stato parziale |
| Validator repository | Qualità completa e migration/package | Esiti registrati |

## Definition of Done

- AC-031-AC-035 verificati e FEAT-005 integrata.
- Migration/rollback e filtri soft delete coperti da test negativi.
- UI, traduzioni, help/guida, telemetria e change fragment aggiornati.
- Comandi applicabili di `AGENTS.md` eseguiti e riportati.
- Nessun hard delete sincrono, rimozione altri membri o delete-account introdotto.
