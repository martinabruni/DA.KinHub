---
status: In review
---

# FEAT-003 - Consultare la lista condivisa paginata

- **Codice**: `lista-condivisa-paginata`
- **Tipo**: `product`
- **Readiness**: `ready`
- **Wave**: 4
- **Risultato**: un membro vede una pagina ordinata di soli item attivi consentiti, con autore e stati vuoto/errore/accesso negato distinti.

## Contesto autonomo

Questa slice introduce il modello persistente minimo della lista e il contratto di accesso limitato riusato da filtro, drawer, completamenti e manutenzione. I dati devono essere isolati per famiglia e visibilità: Shared per membri attivi, Personal solo per owner, anche se in questa versione le creazioni saranno esclusivamente Shared.

## Scope

### Incluso

- Item, gruppo registrazione, categoria, associazioni, owner, visibility, stato, ordine immutabile, versione concorrente e metadati necessari.
- Lista di soli item Active e visibili, ordinata per gruppo recente e posizione riconosciuta.
- Proiezione minima con nome, categorie essenziali e avatar testuale dell'autore.
- Keyset pagination avanti/indietro, cursori opachi e limiti configurati validati con massimo assoluto 5000.
- Stato vuoto reale e lista popolata con layout stabile; FEAT-007 aggiunge il controllo microfono senza lasciare controlli non funzionanti in questa slice.
- Refresh manuale, loading, errore, cursore invalido e `403` senza dati residui.
- Contratto condiviso di pagina/cursori e predicato visibilità, senza generic repository o `Get All`.

### Escluso

- Registrazione/generazione reale (FEAT-007), filtro categoria (FEAT-008), drawer (FEAT-009) e completamento (FEAT-010/011).
- UI o comando per creare Personal, cambiare visibility o creare item manualmente.

## Tracciabilità

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | FLOW-002 | Apertura e refresh lista |
| Requisiti | FR-004-FR-006, FR-015, FR-016, FR-047-FR-051 | Visibilità, autore, ordine e paginazione |
| Regole/decisioni | BR-003, BR-004, BR-007, BR-035-BR-038; DEC-004, DEC-009, DEC-026-DEC-028 | Predicati, ordine e accesso limitato |
| Architettura | ADR-002, ADR-006, ADR-014, ADR-017; sezioni 6.3, 8 | Schema kinlist e query keyset |

## Dipendenze

### Feature prerequisite

| Feature | Tipo | Motivo | Output richiesto | Effetto sul parallelismo |
|---|---|---|---|---|
| FEAT-002 - Creare la propria famiglia | hard | La lista richiede una famiglia attiva ottenibile nel prodotto | Famiglia, membership e contesto autorizzato | Inizio dopo integrazione FEAT-002 |
| FEAT-014 - Usare un design system condiviso in tutta KinHub | hard | La prima UI reale di lista deve riusare shell, card, stati e navigazione condivisi | Floating bars, state panels, row/card primitives e regole di riuso frontend | Inizio dopo integrazione FEAT-014 |
| FEAT-015 - Raggiungere i servizi attivi della famiglia | contract | La route KinList deve rispettare il catalogo e il guard KinService senza duplicare controlli | Contratto catalogo/accesso diretto, `familyId` e confine del gate KinList congelati in CP-001 | Può procedere dopo CP-001; coordinare Home, client API, migration shared e gate KinList |

### Gate e assunzioni

| ID | Stato | Impatto | Evidenza per chiudere |
|---|---|---|---|
| ASM-004 | open, non bloccante | Può migliorare le iniziali dell'avatar | Verifica del nome disponibile; fallback `Membro`/`Member` e `?` obbligatorio |
| TECH-003 | open | Definisce dettagli tecnici di cursori, ordini e indici | Contratto congelato e test query/cursori |

### Parallelismo consentito

Con FEAT-004 e FEAT-015 dopo CP-001. Coordinare `KinHubDbContext`, migration, client API e gate KinList; assegnare ownership distinta a schema `kinlist` e schema shared.

## Contratto di consegna

### Comportamento

- La query applica famiglia, Active e visibility prima di ordine, proiezione e pagina.
- Shared è visibile ai membri attivi della famiglia; Personal solo all'owner. Conteggi/categorie non rivelano item invisibili.
- La pagina effettiva è il minore tra richiesta e limite configurato; configurazioni non positive o oltre 5000 impediscono l'avvio.
- Cursori stale o incompatibili non restituiscono dati e permettono di ripartire dalla prima pagina conservando la vista corrente.
- Nessun item produce uno stato vuoto reale, non confuso con onboarding, filtro vuoto, errore o accesso negato.

### Touchpoint previsti

- **Dominio/business**: layer KinList nei root `domains`/`business` per stati, visibility, ordine e pagina.
- **Persistenza/migration**: schema `kinlist` nel `KinHubDbContext`, vincoli/indici e repository mirati keyset.
- **API/integrazioni**: Function lista con `Family`, `familyId` query e contratti pagina/Problem Details.
- **Frontend/UX**: route/pagina KinList in `App.tsx`, `route-registry.json`, API client, lista responsive e refresh costruiti sul design system condiviso.
- **Infrastruttura/configurazione**: opzioni lettura validate; nessuna nuova risorsa.
- **Documentazione/operazioni**: guida KinList `it`/`en`, help, migration runbook e change fragment.

### Errori, sicurezza e osservabilità

- Scope e visibility sono server-side e applicati anche a categorie/aggregati; il client non decodifica cursori.
- `403` non è uno stato vuoto; durante loading/errore non restano dati di una famiglia precedente.
- Metriche includono durata, page size richiesta/effettiva e cursore invalido, mai contenuto del cursore o item.

## Criteri di accettazione

### AC-011 - Lista attiva ordinata

- **Dato** un membro con gruppi e item attivi nella propria famiglia
- **Quando** apre o aggiorna KinList
- **Allora** vede prima i gruppi recenti e l'ordine originario nel gruppo, indipendentemente dalle modifiche successive
- **Fonte**: FR-015, FR-016, BR-007

### AC-012 - Autore comprensibile

- **Dato** un item visibile creato da un membro
- **Quando** viene mostrata la riga
- **Allora** un avatar circolare presenta iniziali accessibili oppure il fallback localizzato e `?`
- **Fonte**: FR-006, ASM-004, NFR-009

### AC-013 - Nessun leak di visibilità

- **Dato** item Shared, Personal propri, Personal altrui e di altre famiglie
- **Quando** lista, conteggi o categorie sono richiesti
- **Allora** compaiono solo Shared della famiglia e Personal propri, senza indizi sugli altri
- **Fonte**: FR-004, FR-047, BR-003, BR-036

### AC-014 - Capacità uniforme sugli Shared

- **Dato** un item Shared creato da un altro membro
- **Quando** la proiezione espone le azioni consentite
- **Allora** il membro ha la stessa capacità prevista per propri e altrui Shared, senza ruoli
- **Fonte**: FR-005, BR-023

### AC-015 - Pagine limitate e stabili

- **Dato** una collezione oltre la dimensione richiesta
- **Quando** si naviga avanti e indietro mentre avvengono inserimenti o cancellazioni
- **Allora** ogni pagina rispetta il limite, usa l'ordine totale e non deriva da una lettura integrale o offset numerico
- **Fonte**: FR-049, FR-050, BR-037, ADR-017

### AC-016 - Nuove creazioni predisposte come Shared

- **Dato** il modello item di questa versione
- **Quando** viene creato un item tramite una feature autorizzata
- **Allora** owner e Shared sono assegnati dal server e nessun contratto UI consente Personal o conversione
- **Fonte**: FR-047, FR-048, BR-035

### AC-017 - Stati sicuri e recuperabili

- **Dato** lista vuota, caricamento, errore, `403` o cursore invalido
- **Quando** il membro apre o pagina la lista
- **Allora** ogni stato è distinto, localizzato e recuperabile senza dati fuori perimetro o controlli non funzionanti
- **Fonte**: FR-016, FR-051, DEC-027

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Visibility, ordine, limiti e validazione opzioni | Test dominio/business |
| Integrazione | Query/indici keyset, isolamento, filtri e cursori stale | Test PostgreSQL reale con piani/query rappresentativi |
| Frontend/component | Stati lista, avatar, pagine, refresh, responsive/accessibilità | Test componenti e audit |
| End-to-end/manuale | Due famiglie, Shared/Personal, prima/ultima pagina | Nessun leak e ordine stabile |
| Validator repository | Backend, frontend, i18n, route/docs/release, migration/package | Esiti registrati |

## Definition of Done

- Tutti i criteri di accettazione sono verificati; FEAT-002 è integrata e CP-001/CP-002 sono pubblicati.
- Lista, shell e stati usano solo componenti FEAT-014 senza mantenere card/stati/list row legacy equivalenti.
- TECH-003 è chiuso per la lista e il predicato visibilità è riusabile senza generic repository.
- Migration contiene verifica/rollback e non espone `Get All`.
- Route, `PageScaffold`, help/guide `it`/`en`, accessibilità, temi, telemetria e change fragment sono completi.
- Comandi applicabili di `AGENTS.md` eseguiti, incluso publish/package backend.
- Nessuna creazione manuale, UI Personal, cache dati o elemento out of scope.
