## description

Questo task studia il confine di autorizzazione per tutte le API di famiglia e KinList. Il problema concreto e distinguere due domande che non sono equivalenti: «chi sta chiamando?» e «questa persona appartiene alla famiglia indicata?». Microsoft Entra External ID e la validazione dell'access token rispondono alla prima; l'associazione conservata nel database applicativo risponde alla seconda.

Il client invia sempre `familyId` nella query string. Per esempio, una richiesta concettuale usa `...?familyId=<id>`, non una route con l'identificativo e non un valore di famiglia dedotto dal browser. L'handler della policy ricava lo user ID esclusivamente dai claim dell'identita autenticata, poi chiede a un servizio/repository se nel database esiste l'associazione tra quello user ID e il `familyId` richiesto. Soltanto una risposta positiva soddisfa la policy.

La policy deve chiamarsi esattamente `Family` ed e applicata a tutte le API di famiglia e KinList, senza affiancarle nomi alternativi. L'unica eccezione e la creazione della famiglia: l'eccezione riguarda il controllo di appartenenza a una famiglia gia esistente, non l'autenticazione. La creazione rimane quindi accessibile solo a un utente autenticato e deve verificare nel database che l'utente non appartenga gia a una famiglia. Questa lettura risolve l'ambiguita di «tranne creazione famiglia»: una persona anonima non puo creare una famiglia, mentre una persona gia associata non puo crearne una seconda.

### Fatti noti

- Tutte le API che operano nel perimetro di una famiglia esistente usano la policy con nome esattamente `Family`.
- `familyId` e sempre nella query string.
- L'handler ricava lo user ID dai claim verificati, mai dal body, dalla query string o da un header scelto dal client.
- Un servizio/repository verifica nel database l'associazione user-famiglia; esito falso significa `403 Forbidden`.
- La creazione della famiglia richiede autenticazione e rifiuta un utente gia associato a una famiglia.
- KinList condivide dati soltanto tra membri della stessa famiglia.
- La verifica iniziale dell'onboarding e il consumo del codice devono essere invocabili da un utente autenticato ancora privo di famiglia e quindi non possono soddisfare il requisito ordinario della policy `Family`.

### Ipotesi prudenti

- Lo user ID ricavato dai claim identifica l'identita esterna e viene risolto nell'identita applicativa stabile gia prevista dall'architettura.
- Ogni utente puo appartenere al massimo a una famiglia, coerentemente con il controllo richiesto in creazione; il vincolo definitivo resta da confermare nel modello dati.
- Un `familyId` assente o sintatticamente non valido e un errore di input distinto da un'associazione inesistente.

### Decisioni aperte

- Claim esatto e coppia issuer/claim da usare per identificare stabilmente l'utente External ID.
- Stato HTTP e codice Problem Details per `familyId` assente o malformato.
- Stato HTTP e codice applicativo quando la creazione trova un'appartenenza gia esistente, per esempio conflitto `409` oppure richiesta non valida.
- Comportamento in caso di indisponibilita del repository durante l'autorizzazione: deve fallire in modo chiuso, ma va definito il Problem Details operativo.
- Vincolo database che garantisce una sola famiglia per utente e gestione di due creazioni concorrenti.
- Confermare l'eccezione minima alla frase «tutte le API tranne creazione»: oltre alla creazione, anche verifica onboarding e unione tramite codice devono usare la sola autenticazione `ApiAccess` e i propri controlli applicativi, oppure la policy `Family` dovrebbe acquisire una semantica speciale non raccomandata.

## best practices microsoft ux

Questo task non introduce una nuova schermata: definisce il comportamento osservabile quando una pagina KinList o Famiglia chiama l'API. La UI non deve decidere l'autorizzazione nascondendo controlli; nascondere un'azione puo ridurre confusione, ma il server resta il confine autorevole perche richieste HTTP possono essere costruite fuori dall'interfaccia.

Gli stati rilevanti sono distinti:

- **Sessione assente o non valida:** chiedere un nuovo accesso senza mostrare dati familiari.
- **Verifica in corso:** mantenere la pagina in caricamento, senza far apparire per un istante dati della famiglia precedente.
- **`403 Forbidden`:** mostrare un messaggio essenziale di accesso negato e non rappresentarlo come famiglia o lista vuota.
- **Errore tecnico:** offrire `Riprova` senza affermare che l'utente non appartiene alla famiglia.
- **Creazione consentita:** mostrare il flusso di creazione soltanto dopo autenticazione; il controllo server sull'appartenenza viene comunque ripetuto al comando.
- **Creazione rifiutata per appartenenza esistente:** indirizzare l'utente alla propria famiglia, se la navigazione e disponibile, senza suggerire che possa crearne un'altra.

Microsoft raccomanda nomi accessibili, uso da tastiera, focus visibile e messaggi comprensibili anche senza il solo colore ([Microsoft Accessibility overview](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility-overview)). Per KinList significa che «Accesso negato» e testo leggibile, riceve focus quando sostituisce il contenuto e non e soltanto un'icona o un colore rosso.

Alternative considerate:

- **UI come unico controllo:** evita una chiamata che fallira, ma non protegge dati o comandi. Non e valida come autorizzazione.
- **`404 Not Found` anti-enumeration:** nasconde se una famiglia esiste a chi non vi appartiene e puo essere appropriato quando l'esistenza della risorsa e sensibile. Non e la scelta presa qui perche il requisito stabilisce esplicitamente `403` quando l'associazione e falsa.
- **`403 Forbidden` richiesto:** comunica che una richiesta autenticata non e autorizzata e permette alla UI di distinguere accesso negato da dato mancante. Il costo e che conferma l'esistenza del confine famiglia con maggiore chiarezza rispetto al `404`; per ridurre enumerazione, la risposta non deve includere nome, membri o altri dettagli.

La scelta `403` deve restare coerente su letture e scritture. Restituire a volte lista vuota o `404` produrrebbe stati indistinguibili e renderebbe piu difficile spiegare e testare il prodotto.

## best practices microsoft backend

Il token identifica il chiamante, ma non dimostra che appartenga al `familyId` richiesto. Microsoft spiega che i claim sono coppie nome-valore sull'identita e che una policy combina requisiti e handler per prendere una decisione riusabile e testabile ([claim in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims?view=aspnetcore-10.0), [policy-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0)). Nel caso concreto:

1. l'API valida l'access token per firma, issuer, audience e scadenza;
2. la richiesta espone `familyId` dalla query string;
3. la policy `Family` avvia il proprio requisito;
4. l'handler ricava dai claim lo user ID verificato;
5. il servizio/repository interroga il database per l'associazione user-famiglia;
6. solo l'associazione esistente soddisfa il requisito; il valore `false` produce `403`;
7. il caso d'uso parte soltanto dopo l'esito positivo e continua a limitare query e scritture allo stesso `familyId`.

Questo e un controllo basato sulla risorsa richiesta: l'identita da sola non basta, perche la decisione dipende dalla famiglia. Microsoft descrive l'**autorizzazione basata su risorsa** come la verifica congiunta di utente e risorsa tramite `IAuthorizationService`/handler ([resource-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based?view=aspnetcore-10.0)). Qui il dato necessario e il `familyId`, non l'intera famiglia caricata con i suoi dettagli.

### Concetti spiegati

- **Claim:** informazione emessa da un'identita attendibile, per esempio l'identificativo del soggetto. Identifica il chiamante, non prova l'appartenenza corrente nel database.
- **Policy:** regola nominata applicata uniformemente agli endpoint. `Family` evita che ogni Function riscriva il controllo in modo diverso.
- **Handler:** componente che valuta il requisito. Legge il chiamante e il `familyId`, delegando l'accesso dati a un servizio/repository invece di contenere SQL.
- **Fail closed:** se claim, `familyId` o database non permettono di provare l'accesso, la richiesta non procede. Un guasto tecnico non deve trasformarsi in autorizzazione.

Lo user ID non deve arrivare dal client: accettarlo consentirebbe a chi chiama di scegliere l'identita da verificare. Analogamente, un claim contenente un `familyId` non sostituisce il database, perche l'appartenenza puo cambiare prima della scadenza del token. Microsoft indica che e la Web API a validare l'access token e che il client deve trattarlo come opaco ([access token Microsoft identity platform](https://learn.microsoft.com/en-us/entra/identity-platform/access-tokens)).

La creazione famiglia segue un controllo diverso perche per definizione non esiste ancora un `familyId` a cui appartenere. Il backend autentica l'utente, risolve lo user ID dai claim e interroga il repository con la domanda inversa: «esiste gia una qualsiasi associazione?». Se si, non crea; se no, crea famiglia e associazione nello stesso confine transazionale, cosi due richieste concorrenti non possono produrre due famiglie.

La stessa impossibilita logica riguarda due chiamate dell'onboarding: chiedere se l'utente possiede gia un'appartenenza e consumare un codice quando non la possiede. Applicare loro la policy `Family` ordinaria le renderebbe sempre inaccessibili, perche manca proprio l'associazione che devono verificare o creare. La soluzione piu piccola e mantenerle autenticate con `ApiAccess` e far eseguire ai rispettivi casi d'uso controlli specifici: la query di stato legge soltanto il profilo corrente; il join valida il codice e crea l'associazione. Inserire eccezioni dentro l'handler `Family` lo renderebbe dipendente dal nome dell'endpoint e piu facile da applicare in modo errato.

Le risposte usano Problem Details con `code` e `traceId`. `403` non include informazioni sulla famiglia. I log possono contenere esito policy, identificativi tecnici redatti, durata repository e correlation ID, ma non token, claim completi, nome famiglia o dati dei membri. Non servono CQRS, mediator o un motore autorizzativo esterno: un requisito, un handler e un repository risolvono il problema in modo proporzionato.

## best practices microsoft infrastructure

Non servono nuove risorse Azure. La policy vive nella Function App .NET isolata esistente, mentre la verifica riusa PostgreSQL condiviso e il modulo di identita/famiglia. Il modello isolated worker consente dependency injection e configurazione ASP.NET Core tramite `ConfigureFunctionsWebApplication()`, quindi handler e servizi possono essere composti senza un servizio separato ([Azure Functions .NET isolated worker](https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide)).

La persistenza deve supportare una ricerca efficiente dell'associazione per user ID e `familyId`; il vincolo di unicita coerente con «una famiglia per utente» va confermato prima di considerarlo definitivo. L'indice riduce latenza su ogni richiesta, ma la sua forma deve seguire il modello fisico reale e non essere inventata nella ricerca.

Sicurezza e affidabilita iniziali:

- riusare managed identity della Function App verso PostgreSQL con privilegi minimi;
- non conservare appartenenze in cache distribuite o nel token finche non esiste un requisito misurato, evitando autorizzazioni stale;
- propagare `CancellationToken` alla verifica I/O;
- misurare durata, successo, `403`, claim mancanti ed errori repository senza PII;
- distinguere nelle metriche un rifiuto atteso da un guasto del database;
- mantenere CORS restrittivo e HTTPS, senza considerare CORS una misura di autorizzazione.

Application Insights/OpenTelemetry gia condiviso e sufficiente. Non sono giustificati Azure API Management, Redis, Service Bus, un microservizio identita o una Function App separata. Un eventuale aumento della latenza di autorizzazione va prima osservato e corretto con query/indici; una cache di appartenenza richiederebbe invalidazione affidabile e introdurrebbe una finestra di accesso non piu valido.

## flow chart

```mermaid
flowchart TD
    A["Client chiama API famiglia o KinList con familyId in query"] --> B{"Access token valido?"}
    B -- No --> C["401: autenticazione richiesta"]
    B -- Si --> D["Applica policy Family"]
    D --> E{"familyId presente e valido?"}
    E -- No --> F["Problem Details di input"]
    E -- Si --> G["Ricava user ID dai claim"]
    G --> H{"User ID disponibile?"}
    H -- No --> I["Accesso non concesso"]
    H -- Si --> J["Servizio/repository verifica associazione DB"]
    J --> K{"Associazione esistente?"}
    K -- No --> L["403 Forbidden senza dettagli famiglia"]
    K -- Si --> M["Esegue il caso d'uso nel perimetro familyId"]
    J --> N{"Database indisponibile?"}
    N -- Si --> O["Errore tecnico; nessun accesso"]
```

```mermaid
flowchart TD
    A["Utente chiede di creare una famiglia"] --> B{"Utente autenticato?"}
    B -- No --> C["401: autenticazione richiesta"]
    B -- Si --> D["Ricava user ID dai claim"]
    D --> E["Repository cerca qualsiasi associazione famiglia"]
    E --> F{"Associazione gia esistente?"}
    F -- Si --> G["Rifiuta la seconda famiglia"]
    F -- No --> H["Crea famiglia e associazione in modo atomico"]
    H --> I{"Scrittura riuscita?"}
    I -- No --> J["Problem Details; nessuno stato parziale"]
    I -- Si --> K["Restituisce la famiglia creata"]
```

## user experience

La superficie rappresentata e lo stato di una pagina che necessita dati familiari. Non viene aggiunta una pagina di amministrazione. Loading, accesso negato ed errore tecnico non devono mai essere resi come una lista vuota.

```text
CARICAMENTO                    ACCESSO NEGATO
┌──────────────────────────┐   ┌──────────────────────────┐
│ Famiglia                 │   │ Famiglia                 │
│                          │   │                          │
│ Verifico l'accesso...    │   │ Accesso non consentito   │
│          [....]          │   │ Nessun dato mostrato     │
│                          │   │                          │
└──────────────────────────┘   └──────────────────────────┘
```

```text
ERRORE TECNICO
┌──────────────────────────┐
│ Famiglia                 │
│                          │
│ Impossibile verificare   │
│ l'accesso in questo      │
│ momento                  │
│                          │
│ [ Riprova ]              │
└──────────────────────────┘
```

- **Loading:** nessun dato familiare precedente appare prima dell'autorizzazione.
- **Empty:** non esiste uno stato vuoto che sostituisca il `403`; assenza di dati e accesso negato hanno significati diversi.
- **Errore:** `401`, `403`, input non valido e indisponibilita tecnica producono recuperi distinti.
- **Successo:** soltanto dopo la policy `Family` la pagina riceve dati circoscritti al `familyId` richiesto.
