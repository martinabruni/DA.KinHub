## description

Questo task studia la voce `Famiglia` nelle Impostazioni di KinHub/KinList. Il problema concreto e permettere a un membro di riconoscere il proprio nucleo condiviso, vedere chi ne fa parte e avviare un invito, mantenendo l'interfaccia essenziale e senza trasformarla in una console amministrativa.

La pagina mostra soltanto tre contenuti funzionali: nome della famiglia, lista dei membri e azione `Invita`. Non vengono introdotti rimozione dei membri, assegnazione o visualizzazione di ruoli, cambio proprietario o altre azioni amministrative. `Invita` avvia la generazione del codice condiviso manualmente studiato in `family-invite-code`; questa ricerca rappresenta il punto di ingresso senza duplicarne sicurezza e ciclo di vita.

La richiesta dei dati include sempre `familyId` nella query string ed e protetta dalla policy esattamente `Family`. Il server ricava lo user ID dai claim e verifica tramite servizio/repository l'associazione nel database. Un esito falso restituisce `403`; la UI non puo sostituire questo controllo mostrando la pagina soltanto agli utenti che ritiene membri.

Input: identita autenticata e `familyId` della famiglia corrente. Output: nome e membri minimi necessari alla visualizzazione, oppure uno stato di caricamento, vuoto anomalo, accesso negato o errore. Risultato atteso: il membro comprende a quale famiglia appartiene e chi condivide KinList, con una sola azione primaria pertinente.

### Fatti noti

- Nelle Impostazioni esiste la voce `Famiglia`.
- La pagina mostra nome della famiglia, lista membri e azione `Invita`.
- Non sono richieste azioni di rimozione e non sono richiesti ruoli.
- Tutti i membri autorizzati della famiglia condividono gli item KinList.
- La lettura usa la policy `Family`, `familyId` in query string e verifica dell'associazione nel database.
- Associazione falsa significa `403 Forbidden`.

### Ipotesi prudenti

- Ogni membro associato puo vedere il nome e l'elenco dei membri della propria famiglia.
- Ogni riga membro mostra soltanto un nome visualizzabile e, se gia disponibile, iniziali coerenti con l'avatar usato in KinList; email e altri dati non sono automaticamente necessari.
- L'azione `Invita` apre il flusso separato di generazione del codice d'invito.

### Decisioni aperte

- Quali membri possono invitare: tutti i membri oppure un sottoinsieme non ancora definito.
- Durata, numero di utilizzi, revoca e stati del codice, definiti come decisioni aperte in `family-invite-code`.
- Dati minimi visibili per ciascun membro e fallback quando manca il nome visualizzabile.
- Ordinamento della lista membri.
- Comportamento se il database restituisce una famiglia valida ma zero membri, stato incoerente rispetto al chiamante autorizzato.
- Destinazione UI dopo un `403` e dopo la scoperta di un'associazione cambiata.

## best practices microsoft ux

La pagina deve privilegiare riconoscimento e scansione. Il nome della famiglia e il contenuto principale; sotto, una lista verticale di persone con struttura ripetuta. Fluent 2 descrive una lista come una raccolta di elementi simili facile da scorrere e raccomanda il ruolo semantico `list` quando non esiste selezione ([Fluent 2 List](https://fluent2.microsoft.design/components/web/react/core/list/usage)). Qui le righe non sono selezionabili e non aprono azioni: non devono sembrare pulsanti.

`Invita` e l'unica azione della pagina e puo avere evidenza primaria senza aggiungere menu o icone per riga. Fluent 2 raccomanda un solo pulsante primario per layout e un'etichetta attiva che descriva l'azione ([Fluent 2 Button](https://fluent2.microsoft.design/components/web/react/core/button/usage)). La label localizzata `Invita` e breve; se il contesto non e sufficiente per tecnologie assistive, il nome accessibile puo esplicitare `Invita un membro`.

Stati necessari:

- **Loading iniziale:** scheletro breve per nome e righe, senza mostrare dati di una famiglia precedentemente visitata.
- **Successo:** nome come heading, conteggio solo se utile e lista semantica dei membri.
- **Lista vuota anomala:** non inventare un invito come soluzione automatica; il chiamante autorizzato dovrebbe comparire tra i membri, quindi offrire `Riprova` e registrare l'incoerenza.
- **`403 Forbidden`:** nessun nome o membro visibile; messaggio di accesso negato distinto dall'empty state.
- **Errore tecnico:** messaggio essenziale e `Riprova`, conservando soltanto dati gia autorizzati se la strategia del prodotto lo consente.
- **Invito:** il pulsante apre la generazione del codice; se l'utente non possiede il permesso ancora da definire, non deve promettere un'azione che il server rifiutera sistematicamente.

Microsoft considera nomi accessibili, tastiera, screen reader, zoom, contrasto e piu indizi oltre al colore requisiti di base ([Microsoft Accessibility overview](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility-overview)). Gli avatar con iniziali sono quindi decorativi se il nome e gia testo; non possono essere l'unico modo per identificare la persona. Su mobile la lista resta a una colonna; su desktop non serve una tabella, perche non esistono colonne di ruolo o azioni.

Alternative considerate:

- **Card per ogni membro:** offre spazio per molte azioni, ma qui aumenterebbe l'inquinamento visivo senza dati o comandi che lo giustifichino.
- **Tabella con ruoli e menu:** utile in una console amministrativa, ma ruoli e rimozione non sono nello scope.
- **Lista semplice con un solo pulsante Invita:** raccomandata perche corrisponde esattamente ai dati e alle azioni richiesti.
- **`404` per accesso estraneo:** riduce la possibilita di enumerare famiglie, ma non e la scelta adottata; il contratto richiesto usa `403`, senza includere dettagli della famiglia nella risposta.

## best practices microsoft backend

La UI ha bisogno di una proiezione minima: identificativo tecnico necessario al client, nome famiglia e membri con i soli campi approvati per la presentazione. Una **proiezione** e una risposta costruita per la schermata invece di esporre tutte le colonne del database. Nel caso concreto evita di inviare email, claim, associazioni, permessi o metadati che la pagina non mostra.

Il flusso autorevole e:

1. il client autenticato richiede le impostazioni con `familyId` nella query string;
2. l'endpoint applica la policy esattamente `Family`;
3. l'handler ricava lo user ID dai claim verificati;
4. il servizio/repository conferma l'associazione nel database;
5. se l'associazione e falsa, la richiesta termina con `403` e nessun dato;
6. se e vera, il caso d'uso legge nome e membri limitandosi allo stesso `familyId`;
7. la risposta non include ruoli o azioni di rimozione.

Microsoft descrive una policy ASP.NET Core come un insieme di requisiti valutati da handler riusabili; tutti i requisiti devono riuscire prima di autorizzare ([policy-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0)). L'appartenenza non va dedotta dal fatto che il client conosce il `familyId`, ne da un valore liberamente inviato come user ID. I claim identificano il chiamante, mentre il database prova l'associazione corrente.

L'azione `Invita` deve essere separata dalla query di lettura: leggere la pagina non deve generare alcun codice. L'attivazione avvia il caso d'uso descritto in `family-invite-code`, senza destinatario interno e senza invio automatico. Prima dell'implementazione va confermato se tutti i membri possano invitare; la sola appartenenza soddisfatta dalla policy `Family` non inventa automaticamente quel permesso di prodotto.

La lista membri deve usare ordinamento deterministico una volta scelta la regola, cosi refresh e dispositivi non cambiano casualmente la sequenza. Se il nome manca, il backend non deve sostituirlo silenziosamente con email o claim sensibili senza decisione. Un insieme vuoto per una famiglia autorizzata segnala incoerenza, perche l'associazione appena verificata implica almeno il chiamante; non va presentato come normale successo vuoto.

Errori coerenti in Problem Details distinguono input non valido, `403`, incoerenza dati e dipendenza indisponibile, includendo `code` e `traceId`. Log e trace usano family/user ID tecnici secondo la classificazione concordata, correlation ID, durata e conteggio membri; non registrano nomi, email, token o claim completi. Non serve un generic repository, CQRS o un servizio membri separato: un caso d'uso di lettura e repository di dominio/infrastruttura sono sufficienti.

## best practices microsoft infrastructure

Non sono necessarie nuove risorse Azure. La pagina riusa Azure Static Web Apps per la SPA, la Function App .NET condivisa per l'API, PostgreSQL per famiglia/appartenenze e Application Insights/OpenTelemetry per telemetria. Il modello Functions isolated supporta dependency injection standard, utile per comporre handler, servizio e repository senza accoppiarli all'endpoint ([Azure Functions .NET isolated worker](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide)).

Configurazione iniziale proporzionata:

- accesso PostgreSQL tramite managed identity e privilegi minimi sugli schemi condivisi necessari;
- indice coerente con ricerca associazione e lettura membri, da definire sul modello fisico reale;
- nessuna cache di membri nel service worker, in coerenza con il divieto di dati personali nelle cache applicative;
- HTTPS, CORS per origini note e token validati dall'API;
- metriche per durata, `403`, errori e cardinalita aggregata, senza nomi o recapiti;
- alert su errori sistemici ripetuti, non sul singolo accesso negato atteso.

Una cache distribuita non e giustificata: il dataset familiare e piccolo e l'appartenenza deve essere corrente. Una cache introdurrebbe invalidazione quando un membro viene associato o quando cambia nome, pur senza un problema prestazionale misurato. Non servono Microsoft Graph per leggere i membri applicativi, API Management dedicato, Service Bus, realtime o una risorsa separata per gli inviti prima che il flusso invito sia definito.

## flow chart

```mermaid
flowchart TD
    A["Membro apre Impostazioni > Famiglia"] --> B["Client richiede dati con familyId in query"]
    B --> C{"Token valido?"}
    C -- No --> D["Richiede nuovo accesso"]
    C -- Si --> E["Applica policy Family"]
    E --> F["Handler ricava user ID dai claim"]
    F --> G["Repository verifica associazione DB"]
    G --> H{"Associazione esistente?"}
    H -- No --> I["403 senza dati famiglia"]
    H -- Si --> J["Legge nome e membri della stessa famiglia"]
    J --> K{"Lettura riuscita?"}
    K -- No --> L["Errore con Riprova"]
    K -- Si --> M{"Esiste almeno un membro?"}
    M -- No --> N["Stato incoerente e Riprova"]
    M -- Si --> O["Mostra nome, lista membri e Invita"]
    O --> P{"Utente attiva Invita?"}
    P -- No --> Q["Resta nella pagina"]
    P -- Si --> R["Apre il flusso di generazione del codice"]
```

## user experience

La voce Famiglia e una pagina essenziale dentro Impostazioni. Non contiene selettori di ruolo, menu contestuali, pulsanti di rimozione o dettagli amministrativi. Il `PageHelpAccordion` previsto dalle regole generali resta subito dopo il titolo quando la route verra progettata, senza duplicarne qui l'implementazione.

```text
MOBILE
┌──────────────────────────────┐
│ Famiglia                     │
│ [ Aiuto                         ]│
│                              │
│ Casa Rossi                   │
│                              │
│ Membri                       │
│ (MR) Martina Rossi           │
│ (LR) Luca Rossi              │
│ (AR) Anna Rossi              │
│                              │
│ [ Invita ]                   │
└──────────────────────────────┘
```

```text
LOADING                        ACCESSO NEGATO
┌──────────────────────────┐   ┌──────────────────────────┐
│ Famiglia                 │   │ Famiglia                 │
│ [ Aiuto              ]   │   │ [ Aiuto              ]   │
│                          │   │                          │
│ ███████████              │   │ Accesso non consentito   │
│ ○ ████████               │   │ Nessun membro mostrato   │
│ ○ ██████████             │   │                          │
└──────────────────────────┘   └──────────────────────────┘
```

```text
STATO INCOERENTE / ERRORE
┌──────────────────────────┐
│ Famiglia                 │
│ [ Aiuto              ]   │
│                          │
│ Impossibile caricare i   │
│ membri in questo momento │
│                          │
│ [ Riprova ]              │
└──────────────────────────┘
```

- **Loading:** scheletro locale e nessun dato di un'altra famiglia.
- **Empty:** zero membri non e uno stato normale; viene trattato come incoerenza recuperabile.
- **Errore:** `403` separato dal guasto tecnico; nessun dettaglio familiare esposto.
- **Successo:** nome e lista leggibili, righe non interattive e una sola azione `Invita`.
