---
status: Open
---

# FEAT-004 - Consultare le impostazioni della famiglia

- **Codice**: `impostazioni-famiglia`
- **Tipo**: `product`
- **Readiness**: `ready`
- **Wave**: 3
- **Risultato**: un membro raggiunge una pagina Famiglia ricostruibile che mostra nome, membri e inviti attivi senza alterare le preferenze esistenti.

## Contesto autonomo

KinHub possiede già `SettingsPage` con lingua, tema, tutorial e PWA. La feature aggiunge un ingranaggio secondario nella vista KinList, una voce Famiglia nelle impostazioni e la route canonica `/settings/family`. Membri e inviti sono collezioni paginate; il codice segreto non compare mai in questa pagina.

## Scope

### Incluso

- Ingranaggio flottante in basso a destra, dentro safe area e separato da microfono/snackbar.
- Estensione della `SettingsPage` esistente senza rimuovere sezioni.
- Route `/settings/family` con `PageScaffold`, help, guida, URL diretto, refresh, cronologia e focus prevedibile.
- Nome famiglia, membri paginati con nome/iniziali/fallback e inviti attivi paginati con soli metadati.
- Contratti e aree di integrazione per le azioni `Invita` e `Lascia famiglia`; i controlli diventano visibili solo nelle rispettive slice FEAT-005/006, senza affordance non funzionanti.
- Loading, empty inviti, errore, stato incoerente zero membri, cursore invalido e `403` distinti.

### Escluso

- Generazione/revoca/uso inviti e uscita effettiva.
- Modifica nome famiglia, ruoli, rimozione membri o visualizzazione del codice segreto.

## Tracciabilità

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | FLOW-010 | Navigazione e lettura Family |
| Requisiti | FR-034-FR-036, FR-049-FR-051 | Settings, route e paginazione |
| Regole/decisioni | BR-002, BR-027, BR-036-BR-038; DEC-018, DEC-019, DEC-021, DEC-028 | Contenuti minimi e segreto assente |
| Architettura | ADR-012, ADR-016, ADR-017; sezioni 6.8 e 8 | Integrazione Settings e query protette |

## Dipendenze

### Feature prerequisite

| Feature | Tipo | Motivo | Output richiesto | Effetto sul parallelismo |
|---|---|---|---|---|
| FEAT-002 - Creare la propria famiglia | hard | Serve una famiglia reale e membership attiva | Schema shared e contesto famiglia | Inizio dopo FEAT-002 |
| FEAT-003 - Consultare la lista condivisa paginata | contract | Ingranaggio e lista condividono layout/safe area e convenzione pagina/cursori | CP-001/CP-002 congelati, area inferiore e contratto pagina | Può procedere nella stessa wave con ownership file coordinata |

### Gate e assunzioni

| ID | Stato | Impatto | Evidenza per chiudere |
|---|---|---|---|
| ASM-004 | open, non bloccante | Qualità delle iniziali membro | Verifica profilo e fallback obbligatorio |
| TECH-003 | open | Cursori/ordine membri e inviti | Contratti e test avanti/indietro |
| TECH-008 | open | Posizionamento e focus dei controlli fissi | Verifica responsive/safe area/tastiera |

### Parallelismo consentito

Con FEAT-003 dopo CP-001. Non modificare senza coordinamento `App.tsx`, `SettingsPage.tsx`, route registry, API client, shared schema e risorse i18n centrali.

## Contratto di consegna

### Comportamento

- L'ingranaggio apre Settings senza coprire contenuti e il ritorno ripristina il focus quando possibile.
- Settings conserva lingua, tema, tutorial e PWA e aggiunge Famiglia.
- `/settings/family` carica dati minimi con `Family` e `familyId`; membri/inviti usano pagine e cursori opachi.
- I membri senza nome sono `Membro`/`Member` con `?`; zero membri in una famiglia accessibile è errore incoerente.
- Inviti vuoti mostrano lo stato dedicato; inviti presenti non espongono codice o impronta.

### Touchpoint previsti

- **Dominio/business**: query famiglia/membri/inviti e proiezioni minime nei layer esistenti.
- **Persistenza/migration**: repository shared paginati e indici; nessun caricamento integrale.
- **API/integrazioni**: endpoint Family protetti e contratti pagina.
- **Frontend/UX**: `SettingsPage.tsx`, `App.tsx`, `route-registry.json`, `PageScaffold`, stili safe area e risorse `it`/`en`.
- **Infrastruttura/configurazione**: fallback SPA esistente da verificare, nessuna nuova risorsa.
- **Documentazione/operazioni**: guide Family/Settings bilingui, help e change fragment.

### Errori, sicurezza e osservabilità

- Nessun dato precedente resta visibile durante cambio famiglia/stato, errore o `403`.
- Proiezioni membro e invito sono minime; codice/HMAC e contenuti personali non sono loggati.
- Metriche registrano durata, page size, cursori invalidi e stato incoerente in forma aggregata.

## Criteri di accettazione

### AC-018 - Accesso discreto alle impostazioni

- **Dato** KinList su schermo mobile o desktop
- **Quando** il membro usa l'ingranaggio
- **Allora** apre Settings, non copre microfono/contenuti/focus e rispetta safe area
- **Fonte**: FR-034, DEC-018, NFR-012

### AC-019 - Settings preservate

- **Dato** la pagina Settings esistente
- **Quando** viene integrata Famiglia
- **Allora** lingua, tema, tutorial e PWA restano disponibili e la nuova voce apre `/settings/family`
- **Fonte**: FR-035, DEC-019

### AC-020 - Route ricostruibile e documentata

- **Dato** un membro attivo
- **Quando** apre direttamente, aggiorna o naviga avanti/indietro su `/settings/family`
- **Allora** titolo, help e dati vengono ricostruiti e il focus resta prevedibile
- **Fonte**: FR-036, ADR-016, NFR-009

### AC-021 - Dati minimi paginati

- **Dato** membri e inviti oltre una pagina
- **Quando** si naviga avanti e indietro
- **Allora** compaiono solo nome/iniziali e metadati invito, entro 5000 e senza codice segreto
- **Fonte**: FR-036, FR-039, FR-049, FR-050

### AC-022 - Stati sicuri Family

- **Dato** loading, inviti vuoti, errore, zero membri, cursore invalido o accesso revocato
- **Quando** la pagina carica o pagina
- **Allora** gli stati sono distinti, non mostrano dati stale e consentono il recupero appropriato
- **Fonte**: FR-036, FR-051, BR-036, BR-038

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Proiezioni/fallback e mapping stati | Test business |
| Integrazione | Scope, pagine membri/inviti e nessun segreto | Test DB/API `Family` |
| Frontend/component | Route, Settings preservata, focus, safe area, stati | Test componenti/accessibilità |
| End-to-end/manuale | URL/refresh/history e target mobile | Evidenza Chrome/Edge/PWA |
| Validator repository | i18n, routes, docs sync/validate, lint/typecheck/build e backend | Esiti registrati |

## Definition of Done

- Tutti i criteri sono verificati; FEAT-002 integrata e checkpoint rispettati.
- TECH-003/TECH-008 hanno evidenza per le superfici interessate.
- Route registry, `PageScaffold`, help e guide `it`/`en`, Settings e change fragment sono completi.
- Test dimostrano assenza del segreto invito e dei dati stale.
- Comandi applicabili di `AGENTS.md` eseguiti e riportati.
- Nessuna preferenza esistente, ruolo o funzionalità out of scope viene rimossa o aggiunta.
