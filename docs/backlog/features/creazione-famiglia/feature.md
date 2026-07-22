# FEAT-002 - Creare la propria famiglia

- **Codice**: `creazione-famiglia`
- **Tipo**: `product`
- **Readiness**: `ready`
- **Wave**: 2
- **Risultato**: un utente in onboarding crea una sola famiglia con il proprio profilo come unico membro iniziale ed entra in KinList.

## Contesto autonomo

L'utente autenticato senza membership attiva deve poter uscire dall'onboarding tramite il solo nome famiglia. Famiglia e membership devono nascere nello stesso commit; retry o richieste concorrenti non possono creare una seconda famiglia. Il creatore resta un metadato, non un ruolo privilegiato.

## Scope

### Incluso

- Modello e validazione del nome famiglia.
- Comando atomico famiglia + membership del creatore.
- Vincolo dati per una sola membership attiva per utente e idempotenza dell'invio.
- Modulo onboarding con campo unico, errori inline, input preservato e instradamento al successo.
- Telemetria redatta, documentazione, migration e test di concorrenza.

### Escluso

- Inviti, altri membri, ruoli, selettore famiglia e modifica del nome.
- Join con codice, trattato in FEAT-005.

## Tracciabilità

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | FLOW-009 | Creazione famiglia completa |
| Requisiti | FR-031, FR-033 | Famiglia unica e creatore unico membro |
| Regole/decisioni | BR-024, BR-025; DEC-015, DEC-017 | Vincoli di creazione |
| Architettura | ADR-002, ADR-003, ADR-011; sezione 6.1 | Transazione e indice membership attiva |

## Dipendenze

### Feature prerequisite

| Feature | Tipo | Motivo | Output richiesto | Effetto sul parallelismo |
|---|---|---|---|---|
| FEAT-001 - Entrare nel percorso corretto dopo il login | hard | Serve profilo interno, stato onboarding e contratto `ApiAccess` | User ID interno, membership model, bootstrap e error mapping | Inizio solo dopo integrazione FEAT-001 |

### Gate e assunzioni

Nessuno.

### Parallelismo consentito

Nessuno nella wave: la feature consolida lo schema famiglia/membership usato da tutte le slice successive.

## Contratto di consegna

### Comportamento

- L'onboarding mostra il solo campo nome quando l'utente sceglie `Crea una famiglia` e consente di tornare alla scelta.
- Un nome valido crea famiglia e membership del solo creatore nello stesso commit e apre KinList.
- Nome invalido, seconda famiglia, errore o invio ripetuto non lasciano record parziali; l'input non sensibile resta disponibile per il retry.

### Touchpoint previsti

- **Dominio/business**: layer `domains`/`business` per nome, invarianti e caso d'uso atomico.
- **Persistenza/migration**: `KinHubDbContext`, configurazioni shared, indice univoco parziale e migration/rollback.
- **API/integrazioni**: Function protetta da `ApiAccess`, Problem Details stabile e correlation ID.
- **Frontend/UX**: onboarding introdotto da FEAT-001, stato form, i18n `it`/`en`, focus e responsive.
- **Infrastruttura/configurazione**: Nessuna nuova risorsa.
- **Documentazione/operazioni**: help/guida onboarding e change fragment.

### Errori, sicurezza e osservabilità

- Il server deriva user e timestamp dal contesto verificato; non accetta user ID o creatore dal client.
- Concorrenza e retry producono un solo esito; conflitti usano Problem Details senza esporre altri dati.
- Metriche: tentativi, successi, validazioni, conflitti e rollback senza nome famiglia.

## Criteri di accettazione

### AC-007 - Creazione minima

- **Dato** un utente autenticato senza membership attiva
- **Quando** invia un nome famiglia valido
- **Allora** famiglia e membership del solo creatore sono committate insieme e KinList si apre
- **Fonte**: FR-033, BR-025, DEC-017

### AC-008 - Famiglia unica sotto concorrenza

- **Dato** lo stesso utente senza famiglia
- **Quando** due richieste valide tentano di creare contemporaneamente
- **Allora** esiste una sola famiglia attiva e nessuna famiglia orfana
- **Fonte**: FR-031, BR-024, NFR-006

### AC-009 - Validazione e recupero

- **Dato** un nome non valido o un guasto prima del commit
- **Quando** l'utente conferma
- **Allora** vede un errore localizzato, il nome resta nel form e nessun record parziale concede accesso
- **Fonte**: FLOW-009, NFR-001, NFR-006

### AC-010 - Nessun privilegio del creatore

- **Dato** una famiglia appena creata
- **Quando** si legge la membership
- **Allora** il creatore è l'unico membro ma non possiede ruolo o permessi diversi da un futuro membro
- **Fonte**: FR-033, BR-023, DEC-013

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Nome e invarianti famiglia/membership | Test dominio |
| Integrazione | Commit/rollback e creazione concorrente | Test PostgreSQL reale e contratto HTTP |
| Frontend/component | Scelta, form, validazioni, focus e stati | Test componente/accessibilità |
| End-to-end/manuale | Login senza famiglia -> crea -> KinList | Flusso completo senza duplicati |
| Validator repository | Qualità backend/frontend, i18n, docs, route e release | Esiti registrati |

## Definition of Done

- Tutti i criteri di accettazione sono verificati e FEAT-001 è integrata.
- Migration e rollback coprono famiglia, membership e vincolo di unicità.
- Testi, help/guida, accessibilità, telemetria e change fragment sono aggiornati.
- I comandi applicabili di `AGENTS.md` sono eseguiti e riportati.
- Nessun ruolo, invito implicito, seconda famiglia o elemento out of scope è introdotto.
