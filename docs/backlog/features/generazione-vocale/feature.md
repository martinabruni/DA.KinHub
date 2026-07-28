---
status: Open
---

# FEAT-007 - Aggiungere un gruppo tramite la voce

- **Codice**: `generazione-vocale`
- **Tipo**: `product`
- **Readiness**: `blocked`
- **Wave**: 4
- **Risultato**: un membro registra fino a 60 secondi e ottiene una sola volta un gruppo ordinato di item Shared e categorie nella lingua parlata.

## Contesto autonomo

Questa è la slice centrale `Parla -> Ottieni la lista`. La PWA informa sull'uso della voce prima della prima decisione di permesso, acquisisce audio solo in memoria e invia una richiesta sincrona. Il backend autorizza, chiama un deployment multimodale Foundry pinned con schema strict, valida massimo 1000 item e salva tutto o niente. `RecordingId` rende idempotente il trasporto e recupera un commit la cui risposta sia andata persa senza reinviare audio.

## Scope

### Incluso

- Popup iniziale localizzato quando il permesso non è ancora deciso; chiusura non blocca altre funzioni.
- Capability detection, consenso browser, controllo a un tocco start/secondo stop, stato ascolto esplicito e rilascio microfono.
- Controllo microfono centrale nello stato lista realmente vuota e in basso al centro con item attivi, senza sovrapporsi all'ingranaggio o ai feedback.
- Stop automatico a 60 secondi o 12 MB stimati; audio non vuoto nei formati Opus/MP3/AAC/WAV.
- API audio autenticata con `familyId`, `RecordingId`, timeout end-to-end 90 secondi e budget provider totale 75.
- Modello/versione pinned e Structured Output strict/versionato; lingua automatica, semantica DEC-003 e massimo 1000.
- Un solo retry per guasto transitorio senza risposta; nessun retry su output ricevuto ma invalido.
- Transazione gruppo, item Shared, owner, ordine, categorie riusate/create e timeline; tutto o niente.
- Recupero con solo `RecordingId`, nessun audio/output grezzo persistito o loggato.
- Stati processing, successo, nessun item, errori specifici, perdita rete e aggiornamento PWA non distruttivo.

### Escluso

- Anteprima/riproduzione, conferma trascrizione/item, upload riprendibile, file/Blob/coda o fallback asincrono.
- Creazione manuale, UI Personal e normalizzazione prodotto oltre il contratto agente approvato.

## Tracciabilità

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | FLOW-003, FLOW-004 | Informativa, registrazione, elaborazione e salvataggio |
| Requisiti | FR-007-FR-014, FR-052, FR-053, FR-055 | Stato lista, gesto, limiti, idempotenza, lingua e memoria |
| Regole/decisioni | BR-004-BR-006, BR-014-BR-016, BR-035, BR-039-BR-041; DEC-001, DEC-003, DEC-004, DEC-026, DEC-029, DEC-030, DEC-034, DEC-035 | Contratto voice-to-list |
| Architettura | ADR-005, ADR-006, ADR-009, ADR-014; sezioni 6.1-bis e 6.4 | Provider, budget, privacy e transazione |

## Dipendenze

### Feature prerequisite

| Feature | Tipo | Motivo | Output richiesto | Effetto sul parallelismo |
|---|---|---|---|---|
| FEAT-003 - Consultare la lista condivisa paginata | hard | Il gruppo deve usare modello item, visibilità, ordine e refresh lista | Schema item/gruppo/categorie, predicato e contratto lista | Inizio dopo FEAT-003 e chiusura GATE-001 |
| FEAT-009 - Correggere un item e consultarne la storia | contract | La creazione deve emettere un evento timeline compatibile | CP-003 con tipi evento/versione congelati | Può procedere in parallelo dopo checkpoint |

### Gate e assunzioni

| ID | Stato | Impatto | Evidenza per chiudere |
|---|---|---|---|
| GATE-001 | open, blocking | Senza deployment/modello/versione/regione non esiste un provider eseguibile approvato | Decisione e capacità/RBAC/contratto registrati |
| TECH-004 | open | Verifica timeout host/proxy e MIME effettivi | Test end-to-end ambienti/browser |
| TECH-007 | open | L'header attuale nega il microfono | Permissions-Policy minima verificata |
| TECH-008 | open | Controlli fissi e snackbar devono coesistere | Test safe area/focus/zoom |

### Parallelismo consentito

Dopo GATE-001, con FEAT-005/008/009 usando CP-002/CP-003. Coordinare API client audio, riga lista, categorie/timeline, configuration/Bicep e file i18n.

## Contratto di consegna

### Comportamento

- Il popup appare solo quando la decisione browser non esiste; concedere il permesso vale come consenso descritto.
- L'area attiva non si sposta; ascolto e stop sono annunciati senza dipendere solo da colore/animazione.
- Una registrazione valida produce un gruppo intero una sola volta in cima, mantenendo ordine e lingua.
- Se il commit esiste, il recupero con `RecordingId` restituisce lo stesso gruppo; non richiede audio.
- In ogni esito tracce microfono, buffer e output grezzo vengono rilasciati.

### Touchpoint previsti

- **Dominio/business**: validazione candidati, gruppo/ordine, categorie, idempotenza e transazione.
- **Persistenza/migration**: registrazioni/comandi, gruppo/item/categorie/timeline e vincoli `RecordingId`.
- **API/integrazioni**: Function audio/recupero, adapter Foundry e managed identity.
- **Frontend/UX**: MediaRecorder, popup, macchina stati, lista, connettività e update coordination.
- **Infrastruttura/configurazione**: config/RBAC Foundry esistente, timeout e `staticwebapp.config.json`; nessuna nuova risorsa.
- **Documentazione/operazioni**: guida voce/consenso, provider contract/runbook, troubleshooting e change fragment.

### Errori, sicurezza e osservabilità

- Audio vuoto, limite/formato, device, permission, rete, timeout, no item e output invalido hanno codici stabili e nessun effetto parziale.
- Audio/output/nomi/categorie non compaiono in log, file, cache, DB o code; deployment/versione/contratto sì.
- Metriche separano upload, provider, validazione, persistenza, retry, timeout e cardinalità aggregata.

## Criteri di accettazione

### AC-036 - Informativa iniziale non bloccante

- **Dato** permesso microfono non ancora deciso
- **Quando** il membro apre KinList
- **Allora** vede l'informativa approvata, può chiuderla e continuare senza attivare il microfono
- **Fonte**: FR-055, BR-041, DEC-035

### AC-037 - Gesto e stop sicuri

- **Dato** capacità, rete e permesso disponibili
- **Quando** il membro vede la lista vuota o popolata e tocca una volta e poi di nuovo il controllo
- **Allora** il controllo è rispettivamente al centro o in basso al centro, la registrazione parte/termina, lo stato è esplicito e il microfono viene rilasciato
- **Fonte**: FR-007-FR-011

### AC-038 - Limiti e budget

- **Dato** audio valido o oltre limite
- **Quando** raggiunge 60 secondi/12 MB o viene elaborato
- **Allora** lo stop è automatico e l'intero esito termina entro 90 secondi con provider entro 75 e massimo 1000 item
- **Fonte**: FR-012, DEC-001, DEC-029

### AC-039 - Memoria soltanto

- **Dato** successo, errore, annullamento o timeout
- **Quando** la richiesta termina
- **Allora** audio e output grezzo sono rilasciati e non esistono copie in browser, file, Blob, code o database
- **Fonte**: FR-053, BR-040, NFR-014

### AC-040 - Gruppo atomico Shared

- **Dato** output valido nella lingua parlata
- **Quando** il backend salva
- **Allora** tutti gli item e categorie compaiono una volta, Shared, con owner server-side e ordine riconosciuto oppure nessuno viene creato
- **Fonte**: FR-012, FR-014, FR-048, FR-052

### AC-041 - Retry ristretto

- **Dato** guasto transitorio senza risposta oppure output ricevuto ma invalido
- **Quando** la pipeline decide il recupero
- **Allora** solo il primo caso può avere un unico retry nel budget; il secondo fallisce senza retry/scritture
- **Fonte**: BR-015, BR-039, DEC-029

### AC-042 - Idempotenza e recupero

- **Dato** trasporto duplicato o risposta persa dopo commit
- **Quando** la PWA usa lo stesso `RecordingId`
- **Allora** riceve lo stesso gruppo senza duplicarlo e senza conservare/reinviare audio
- **Fonte**: FR-013, BR-006, DEC-030

### AC-043 - Categorie familiari

- **Dato** categorie equivalenti esistenti e nuove candidate
- **Quando** il gruppo viene salvato
- **Allora** le equivalenti sono riusate e solo le necessarie sono create nel catalogo famiglia
- **Fonte**: DEC-004, BR-016

### AC-044 - Errori recuperabili senza righe vuote

- **Dato** audio vuoto, non supportato, senza item o rete persa
- **Quando** l'elaborazione fallisce
- **Allora** nessuna riga/gruppo parziale appare, l'errore è specifico e una nuova registrazione è possibile
- **Fonte**: FLOW-003, FLOW-004, NFR-006

### AC-045 - Accessibilità e target browser

- **Dato** target Chrome/Edge, temi, lingue e reduced motion
- **Quando** si usa il controllo tramite touch/tastiera/screen reader
- **Allora** focus, annunci, area attiva e aggiornamento PWA non interrompono l'operazione
- **Fonte**: NFR-002, NFR-009, NFR-010, NFR-015

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Limiti, retry, schema, semantica, idempotenza | Test pipeline/business |
| Integrazione | Provider contract, transazione e recupero | Test adapter + PostgreSQL/API |
| Frontend/component | Permesso, macchina stati, errori, offline/accessibilità | Test componenti con browser API simulate |
| End-to-end/manuale | Formati/device, 60s/12MB, risposta persa, PWA target | Evidenze Chrome/Android/Edge; Safari best effort |
| Validator repository | Tutta la qualità, Bicep, docs/release e package | Esiti registrati |

## Definition of Done

- GATE-001 chiuso; TECH-004/007/008 verificati e AC-036-AC-045 passati.
- FEAT-003 integrata e CP-002/CP-003 congelati.
- Nessun payload sensibile persiste o entra nei log; redazione e cleanup buffer sono testati.
- Config/RBAC non contiene secret; help/guide/consenso `it`/`en`, accessibilità, telemetria e fragment completi.
- Build/test/publish/package, frontend validator e Bicep applicabili eseguiti.
- Nessun file/storage/coda, anteprima, UI Personal o comportamento out of scope.
