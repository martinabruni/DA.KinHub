---
status: In review
---

# BUG-FEAT-004-001 - Correggere azioni e identita visualizzata nella pagina Famiglia

- **Feature interessata**: FEAT-004 `impostazioni-famiglia`
- **Tipo**: correzione frontend/backend, presentazione profilo
- **Readiness**: `ready`
- **Stato**: `In review`
- **Breaking change prodotto**: no
- **Risultato**: la card del nome famiglia non mostra un refresh non richiesto e ogni membro attivo usa lo stesso nome profilo applicativo mostrato nel menu utente della floating navigation.

## Segnalazione

La pagina `/settings/family` presenta due comportamenti errati:

- nella card del nome famiglia compare un pulsante `Refresh now`, che non è un'azione voluta per quella informazione;
- la lista dei membri attivi restituisce erroneamente lo username dell'utente autenticato e il client mostra il fallback invece del valore di profilo già visibile aprendo l'icona utente nella floating navigation, sopra il pulsante di sign out.

Il valore da mostrare per il membro autenticato deve coincidere con quello passato a `UserMenu` come `accountName`. Non deve essere ricavato nuovamente da email, `preferred_username`, claim grezzi o token.

## Scope

### Incluso

- Rimuovere dalla card del nome famiglia il pulsante di refresh non richiesto, senza rimuovere il recupero iniziale o gli stati di errore della pagina.
- Verificare e correggere la proiezione API/business dei membri attivi affinché il profilo restituito sia quello applicativo approvato e coerente con il nome usato dalla floating navigation.
- Correggere il mapping/rendering client affinché il valore ricevuto venga mostrato e il fallback resti riservato alla reale assenza di un nome approvato.
- Mantenere invariati autorizzazione `Family`, paginazione, privacy, localizzazione e comportamento degli altri contenuti Family.
- Aggiungere test per membro autenticato, altro membro, nome assente e card senza affordance di refresh.

### Escluso

- Modifica del profilo applicativo, del contratto di autenticazione o della UI globale della floating navigation.
- Nuova persistenza di username o dati personali.
- Modifica del nome famiglia, della paginazione, degli inviti o delle azioni `Invita`/`Lascia famiglia`.

## Tracciabilità

| Tipo | Riferimenti | Contributo del bug |
|---|---|---|
| Flussi | FLOW-010 | Corregge la consultazione della pagina Family |
| Requisiti | FR-036, FR-049-FR-051 | Mantiene la pagina Family ricostruibile e la lista membri corretta |
| Regole/decisioni | BR-036, BR-038; DEC-019, DEC-021, DEC-028 | Preserva dati minimi, fallback solo quando necessario e assenza di azioni non approvate |
| Architettura | ADR-012, ADR-016, ADR-017; `feature.plan.md` sezioni 6, 9 e 10 | Mantiene profilo applicativo, confine Family e rendering condiviso |

## Dipendenze

### Feature prerequisite

Nessuna. La correzione può essere sviluppata su un ramo dedicato usando i contratti già presenti.

### Gate e assunzioni

| ID | Stato | Impatto | Evidenza per chiudere |
|---|---|---|---|
| TECH-004-BUG | closed, verificato | `accountName` è condiviso dal layout con la pagina; l'API identifica il membro corrente tramite l'application user autorizzato senza esporre claim | Test business/frontend e confronto esplicito con `UserMenu` |

### Parallelismo consentito

Con altre attività Family solo dopo aver concordato il campo di profilo applicativo e senza modifiche concorrenti a `FamilySettingsPage`, `api.ts`, projection membri, `FloatingBars.tsx` o traduzioni condivise. La floating navigation non va modificata salvo coordinamento esplicito.

## Contratto di consegna

### Comportamento

- La card del nome famiglia mostra nome e descrizione, ma nessun pulsante `Refresh now` o equivalente.
- Il caricamento iniziale e il recupero dagli stati di errore restano disponibili secondo il contratto FEAT-004.
- Un membro con nome profilo applicativo disponibile mostra quel nome nella lista Family.
- Per il membro autenticato, il nome mostrato coincide testualmente con quello esposto dal menu della floating navigation sopra `Sign out`.
- Un membro senza nome approvato continua a mostrare il fallback localizzato e `?`.
- Nessun nome viene derivato da username tecnico, email, `preferred_username` o token quando il profilo applicativo non lo fornisce.

### Touchpoint previsti

- **Dominio/business**: verificare il mapping del profilo membro e preservare il valore approvato, oppure Non pertinente se il difetto è solo nella projection/API esistente.
- **Persistenza/migration**: nessuna migration; verificare soltanto la projection/read model esistente.
- **API/integrazioni**: endpoint membri Family e DTO `displayName`/`initials`; mantenere policy `Family`, `familyId` e Problem Details.
- **Frontend/UX**: `FamilySettingsPage.tsx`, `src/frontend/src/lib/api.ts`, `FloatingBars.tsx` solo come riferimento del valore `accountName`, componenti MemberRow e risorse `it`/`en`.
- **Infrastruttura/configurazione**: Nessuna.
- **Documentazione/operazioni**: aggiornare help/guida Family soltanto se il testo descrive il refresh o il fallback in modo non più vero; aggiungere change fragment se richiesto dalla modifica significativa.

### Errori, sicurezza e osservabilità

- La correzione non amplia i dati restituiti dalla projection membro e non espone claim o identificativi tecnici.
- Autenticazione, policy `Family`, `Cache-Control: no-store, private`, correlation ID e Problem Details restano invariati.
- Log e telemetria non includono nomi, username, email, claim o payload personali.

## Criteri di accettazione

### AC-004-001 - Nessun refresh sul nome famiglia

- **Dato** un membro autorizzato sulla pagina `/settings/family`
- **Quando** osserva la card del nome famiglia
- **Allora** non trova il pulsante `Refresh now` né un controllo equivalente; la pagina conserva il caricamento iniziale e il recupero previsto per gli errori
- **Fonte**: segnalazione bug; FR-036; DEC-019

### AC-004-002 - Nome coerente con il menu utente

- **Dato** un membro autenticato con nome profilo applicativo disponibile
- **Quando** consulta la lista dei membri attivi e apre l'icona profilo nella floating navigation
- **Allora** il nome della propria riga coincide con il nome sopra `Sign out` nel menu utente
- **Fonte**: segnalazione bug; `feature.plan.md` sezioni 6, 9 e 10

### AC-004-003 - Altri membri e fallback

- **Dato** un altro membro con nome approvato e un membro senza nome approvato
- **Quando** viene caricata la lista membri
- **Allora** il primo mostra il proprio nome, mentre il secondo mostra esclusivamente il fallback localizzato e `?`, senza derivazioni da username o claim
- **Fonte**: FR-036; BR-036; DEC-021

### AC-004-004 - Contratto e sicurezza invariati

- **Dato** una richiesta autenticata e una richiesta senza accesso alla famiglia
- **Quando** viene letto l'elenco membri
- **Allora** il percorso autorizzato mantiene paginazione e proiezione minima, mentre il percorso negato mantiene il comportamento `401`/`403` esistente senza leak di dati
- **Fonte**: ADR-012, ADR-017; `AGENTS.md` regole API e sicurezza

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Mapping del profilo membro e fallback | Test business con nome presente/assente e senza claim come fonte |
| Integrazione | Projection endpoint membri, autorizzazione e paginazione | Test API/DB senza dati oltre la projection approvata |
| Frontend/component | Card Family, riga autenticata, riga altro membro, fallback e accessibilità | Test componenti con confronto al valore passato a `UserMenu` |
| End-to-end/manuale | Pagina Family e popover profilo su desktop/mobile, italiano/inglese | Evidenza del nome identico e assenza del pulsante refresh |
| Validator repository | i18n, docs, lint, typecheck, test e build applicabili | Esiti registrati senza dichiarazioni non verificate |

## Definition of Done

- Tutti i criteri di accettazione sono verificati.
- La card non presenta l'azione refresh non approvata e il nome membro è coerente con il menu utente.
- API, frontend, test, localizzazione e documentazione pertinente sono aggiornati senza introdurre dati personali o fallback impropri.
- I comandi di qualità richiesti da `AGENTS.md` e quelli applicabili alla superficie modificata sono eseguiti e riportati.
- Il bug è in stato `In review`; la transizione a `Completed` resta riservata alla responsabile umana.
