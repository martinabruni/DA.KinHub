---
status: Open
---

# FEAT-005 - Invitare e unirsi con un codice

- **Codice**: `inviti-e-join`
- **Tipo**: `product`
- **Readiness**: `ready`
- **Wave**: 4
- **Risultato**: un membro genera o revoca un invito e un utente senza famiglia lo consuma una sola volta per diventare membro.

## Contesto autonomo

Gli inviti sono credenziali temporanee manuali, non messaggi. Tutti i membri hanno le stesse capacità. Il codice di 12 caratteri Crockford Base32 è mostrato una sola volta, conservato a riposo solo tramite HMAC versionato, valido sette giorni, revocabile e monouso. Join e membership sono un unico esito atomico e anti-enumeration.

## Scope

### Incluso

- Generazione crittografica, formato `XXXX-XXXX-XXXX`, massimo cinque inviti attivi e segreto one-time.
- Elenco dei soli metadati, revoca confermata consentita a ogni membro e stato aggiornato.
- Onboarding join con normalizzazione di spazi, trattini e maiuscole.
- Consumo atomico e creazione/riattivazione membership storica, con un solo vincitore concorrente.
- Risposta generica per codice inesistente/scaduto/revocato/usato.
- Rate limit per istanza 5/5 minuti per `(iss, oid)` e 20/5 minuti per origine attendibile, con `Retry-After`.
- Rotazione/versione HMAC verificabile, telemetria redatta, paginazione inviti e test di concorrenza.

### Escluso

- Email, link, notifiche, rubrica, ricerca, codice recuperabile o invito riutilizzabile.
- Rate limit distribuito, Redis, APIM o ruoli amministrativi.

## Tracciabilità

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | FLOW-011 | Generazione, condivisione manuale, join e revoca |
| Requisiti | FR-037-FR-040, FR-054 | Ciclo invito e protezione tentativi |
| Regole/decisioni | BR-023, BR-026-BR-028; DEC-013, DEC-020, DEC-021, DEC-032 | Capacità uniforme, segreto e consumo |
| Architettura | ADR-011, ADR-012; sezioni 6.7, 8, 9 | HMAC, transazione, anti-enumeration e metriche |

## Dipendenze

### Feature prerequisite

| Feature | Tipo | Motivo | Output richiesto | Effetto sul parallelismo |
|---|---|---|---|---|
| FEAT-004 - Consultare le impostazioni della famiglia | hard | Generazione/revoca e metadati vivono nella pagina Family; join riusa onboarding | Route Family, proiezione inviti, onboarding e contratti pagina | Inizio dopo FEAT-004 |

### Gate e assunzioni

Nessuno. L'origine attendibile e la rotazione chiavi sono dettagli tecnici da verificare nella feature senza cambiare i limiti approvati.

### Parallelismo consentito

Con FEAT-007/008/009 dopo CP-002. Coordinare onboarding, pagina Family, API client, schema shared, migration e codici Problem Details.

## Contratto di consegna

### Comportamento

- Sotto cinque inviti attivi, il codice appare solo nella risposta/superficie di creazione con scadenza; chiuderla lo rende non recuperabile.
- L'elenco successivo espone creatore, creazione, scadenza e stato, mai codice o HMAC.
- Revoca richiede conferma e rende il codice immediatamente inutilizzabile.
- Join normalizza input, non rivela la famiglia prima del successo e consuma invito più membership nello stesso commit.
- Una membership storica della stessa famiglia viene riattivata; una membership attiva impedisce join a una seconda famiglia.

### Touchpoint previsti

- **Dominio/business**: invito, validità, normalizzazione, limiti, join/riattivazione e revoca.
- **Persistenza/migration**: schema shared, HMAC versionato, vincoli consumo/membership e transazioni concorrenti.
- **API/integrazioni**: generate/revoke con `Family`; join con `ApiAccess`; rate limit e `Retry-After`.
- **Frontend/UX**: Family e onboarding, conferma revoca, superficie one-time e i18n.
- **Infrastruttura/configurazione**: chiave HMAC nel sistema sicuro esistente; origine proxy attendibile configurata; nessuna nuova risorsa.
- **Documentazione/operazioni**: guida inviti, rotazione chiave, troubleshooting rate limit e change fragment.

### Errori, sicurezza e osservabilità

- Codice, HMAC, motivo preciso del rifiuto e nome famiglia non sono loggati o rivelati.
- Il limite attivo e il consumo sono ricontrollati in transazione; errori non lasciano membership/inviti parziali.
- Metriche aggregano generazione, revoca, consumo, rifiuto e rate limit senza origine o identità in chiaro.

## Criteri di accettazione

### AC-023 - Codice conforme e one-time

- **Dato** una famiglia con meno di cinque inviti attivi
- **Quando** un membro genera un invito
- **Allora** riceve una sola volta 12 caratteri Crockford formattati, con scadenza a sette giorni e storage senza chiaro
- **Fonte**: FR-038, FR-039, BR-026, BR-027

### AC-024 - Limite inviti

- **Dato** cinque inviti ancora attivi
- **Quando** un membro tenta il sesto
- **Allora** la richiesta è rifiutata senza creare record e senza privilegi basati sul creatore
- **Fonte**: FR-037, FR-038, BR-023, BR-026

### AC-025 - Revoca da qualunque membro

- **Dato** un invito attivo creato da un altro membro
- **Quando** il membro conferma la revoca
- **Allora** l'invito diventa inutilizzabile e il segreto non viene mostrato
- **Fonte**: FR-037, FLOW-011, DEC-020

### AC-026 - Join atomico e riattivazione

- **Dato** un utente senza famiglia e un codice valido normalizzato
- **Quando** conferma il join
- **Allora** il codice è consumato e la membership è creata o riattivata nello stesso commit, poi si apre KinList
- **Fonte**: FR-040, BR-028

### AC-027 - Consumo concorrente singolo

- **Dato** lo stesso codice valido usato contemporaneamente da due utenti
- **Quando** entrambe le transazioni tentano il consumo
- **Allora** una sola riesce e l'altra riceve il rifiuto generico senza stato parziale
- **Fonte**: FR-038, FR-040, NFR-006

### AC-028 - Anti-enumeration

- **Dato** un codice inesistente, scaduto, revocato o usato
- **Quando** viene inviato
- **Allora** risposta e UI sono indistinguibili e non rivelano famiglia o stato del codice
- **Fonte**: FLOW-011, NFR-011, ADR-012

### AC-029 - Rate limit approvato

- **Dato** tentativi ripetuti dalla stessa identità o origine attendibile
- **Quando** supera rispettivamente 5 o 20 tentativi in 5 minuti sull'istanza
- **Allora** il join è limitato con `Retry-After` e nessun dettaglio aggiuntivo
- **Fonte**: FR-054, DEC-032

### AC-030 - Nessuna seconda famiglia

- **Dato** un utente con membership attiva
- **Quando** tenta di usare un codice valido
- **Allora** non consuma il codice e resta nella famiglia corrente
- **Fonte**: FR-031, BR-024, BR-028

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Formato, normalizzazione, scadenza, limiti e messaggi pubblici | Test deterministici |
| Integrazione | HMAC, limite/consumo concorrente, rollback e riattivazione | Test PostgreSQL/API reali |
| Frontend/component | One-time, conferma revoca, form join e rate limit | Test stato/accessibilità/i18n |
| End-to-end/manuale | Genera -> condivide manualmente -> join; revoca; doppio consumo | Esiti senza leak |
| Validator repository | Backend/frontend/docs/i18n/routes/release/package | Esiti registrati |

## Definition of Done

- AC-023-AC-030 verificati, FEAT-004 integrata e CP-002 rispettato.
- Rotazione HMAC e origine attendibile sono documentate e testate senza secret nel repository.
- Migration/rollback, telemetria, help/guide `it`/`en`, accessibilità e change fragment completi.
- Comandi applicabili di `AGENTS.md` eseguiti e riportati.
- Nessun canale automatico, ruolo, storage in chiaro o rate limit distribuito introdotto.
