## description

Questo task studia la selezione multipla di item attivi e il loro completamento con una sola azione esplicita. Il problema concreto non e soltanto aggiornare piu righe: l'utente deve capire quali item sta selezionando, il server deve verificare ogni identificativo e l'esito deve restare comprensibile se nel frattempo un familiare modifica o completa uno degli item.

Input del flusso: un insieme senza duplicati di identificativi degli item selezionati e, per rilevare cambiamenti concorrenti, la versione osservata di ciascun item. Output: tutti gli item accettati passano da `Active` a `Completed`, con autore, data e timeline coerenti, oppure il server restituisce un errore strutturato senza fingere un successo. Il client non invia famiglia, autore o timestamp come valori autorevoli.

### Fatti noti

- KinList usa una lista familiare, aggiornamento collaborativo tramite refresh manuale e concorrenza ottimistica.
- Il completamento singolo e persistito subito, nasconde l'item e registra stato, `CompletedAt`, autore e timeline.
- La nuova esigenza comprende selezione multipla e completamento bulk; non definisce ancora il comportamento di undo del gruppo.
- Backend .NET, EF Core e PostgreSQL sono gia il confine transazionale previsto.
- Autenticazione e autorizzazione devono essere applicate dal server, non dedotte dalla UI.

### Ipotesi prudenti

- Il bulk riguarda item attivi mostrati nella stessa vista KinList, non un processo lungo o una selezione che attraversa dataset non caricati.
- Il numero di item per richiesta puo essere limitato per mantenere breve la transazione; il valore non e ancora deciso.
- Ogni item conserva la propria chiave d'ordine, quindi un eventuale ripristino non richiede un indice visuale.

### Decisioni aperte

- Come si entra ed esce dalla modalita di selezione su mobile e desktop.
- Se «Seleziona tutti» riguarda gli item visibili dopo il filtro, tutti quelli caricati o tutti quelli esistenti sul server.
- Numero massimo di item in una richiesta bulk.
- Semantica definitiva: tutti o nessuno oppure successo parziale.
- Comportamento quando un item cambia versione o stato dopo la selezione.
- Se il bulk offre undo aggregato, undo per singolo item oppure nessun undo aggiuntivo; durata e regole server della finestra restano da decidere.
- Rappresentazione nella timeline: un evento per item e l'eventuale identificativo comune del comando, senza introdurre un nuovo tipo di evento non approvato.

## best practices microsoft ux

La selezione multipla deve essere una fase riconoscibile e reversibile prima del completamento. Fluent 2 descrive le checkbox come controlli adatti a scegliere piu opzioni e precisa che la selezione non dovrebbe produrre da sola l'effetto finale: serve un'azione di conferma, qui «Completa N» ([Fluent 2 Checkbox](https://fluent2.microsoft.design/components/web/react/core/checkbox/usage)). Questo separa due intenzioni: scegliere gli item e completarli.

Quando la modalita e attiva, ogni riga mostra un controllo con nome accessibile che include l'item, mentre una barra azioni comunica sempre il conteggio selezionato. Colore, segno di spunta e testo devono concordare; il solo cambio di colore non e sufficiente. Il focus non deve saltare quando una riga viene selezionata e l'azione bulk deve essere raggiungibile da tastiera senza percorrere nuovamente tutta la lista.

Stati necessari:

- **Nessuna selezione:** azione di completamento disabilitata o assente, senza barra vuota che aumenti l'inquinamento visivo.
- **Selezione attiva:** conteggio aggiornato e azioni essenziali «Annulla selezione» e «Completa N».
- **Invio:** controlli temporaneamente non modificabili; feedback indeterminato breve, senza percentuali inventate.
- **Successo:** gli item completati scompaiono insieme e un feedback annuncia il numero effettivo.
- **Conflitto o errore:** nessun messaggio generico; la UI ricarica lo stato autorevole e spiega che la lista e cambiata o che l'operazione non e riuscita.
- **Stato vuoto:** se il gruppo conteneva gli ultimi item visibili, mostrare il normale stato vuoto senza perdere l'eventuale feedback o azione di recupero approvata.

### Alternative di esito

Con **tutti o nessuno**, l'utente esprime una sola intenzione: se uno degli item non puo essere completato, nessuno cambia. Il vantaggio UX e un risultato facile da spiegare e da riconciliare; il costo e dover ripetere la selezione dopo un solo conflitto. Con **successo parziale**, gli item validi cambiano e gli altri restano: e utile per batch grandi o con alta contesa, ma richiede un riepilogo per item, una selezione residua e regole di undo piu complesse.

La raccomandazione pragmatica iniziale e tutti-o-nessuno, a condizione che il batch sia piccolo e limitato. KinList privilegia semplicita e una singola lista familiare; il costo di ripetere una selezione breve e probabilmente inferiore al costo cognitivo di un esito misto. Questa e una raccomandazione, non una decisione gia approvata. Se i volumi reali o i conflitti frequenti smentiscono l'ipotesi, il successo parziale diventa piu adatto.

Fluent 2 usa i toast per feedback temporaneo non critico e raccomanda una posizione prevedibile e pochi messaggi simultanei ([Fluent 2 Toast](https://fluent2.microsoft.design/components/web/react/core/toast/usage)). Un successo complessivo puo quindi usare un solo feedback. Un errore che richiede una nuova scelta non deve sparire troppo rapidamente. L'undo non va dedotto dal completamento singolo: un'unica azione «Annulla N» e semplice, ma deve essere approvata insieme alle regole per item modificati dopo il bulk; una coda di N snackbar sarebbe invece rumorosa e poco proporzionata.

## best practices microsoft backend

Il server riceve gli ID selezionati e non presume che siano sicuri perche provenienti dalla lista mostrata. Per ogni ID deve verificare nello stesso flusso: esistenza, appartenenza alla famiglia corrente, visibilita per l'utente, permesso di completamento, stato `Active` e versione attesa. Un ID non autorizzato non puo essere ignorato silenziosamente in un'operazione tutti-o-nessuno, altrimenti il client potrebbe credere completato un insieme diverso da quello richiesto.

Il problema dell'autorizzazione dipende dai dati dell'item, non soltanto dal fatto che l'utente sia autenticato. Microsoft definisce questo controllo **autorizzazione basata sulla risorsa**: la decisione usa insieme utente, operazione e risorsa caricata ([ASP.NET Core resource-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based?view=aspnetcore-10.0)). Nel bulk la verifica va applicata a ogni risorsa, anche se una query gia circoscritta riduce il set candidato. Confrontare il numero di ID distinti richiesti con il numero di item autorizzati e validi impedisce di accettare accidentalmente un sottoinsieme.

Per l'esito tutti-o-nessuno, le modifiche di stato, `CompletedAt`, autore, metadati e un evento timeline per ciascun item appartengono alla stessa transazione EF Core. Una transazione applica tutte le scritture o nessuna; EF Core garantisce gia l'atomicita di una singola `SaveChanges` con provider relazionale, mentre una transazione manuale serve soltanto se il caso d'uso richiede piu salvataggi ([EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions)). La transazione deve essere breve: caricare e validare il batch, applicare le transizioni e salvare, senza attese utente o chiamate remote al suo interno.

La transazione non risolve da sola la concorrenza. Ogni item porta una versione osservata; l'update riesce soltanto se quella versione e ancora corrente. EF Core chiama questo meccanismo **concorrenza ottimistica** e segnala il conflitto quando una riga non corrisponde piu alla versione letta ([EF Core concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)). Nell'opzione raccomandata, un conflitto su un solo item provoca rollback dell'intero bulk e restituisce un codice stabile che invita a ricaricare. Un retry automatico con dati nuovi cambierebbe implicitamente l'intenzione dell'utente e non e raccomandato.

### Concetti spiegati

- **Tutti-o-nessuno o atomicita:** tutte le transizioni del gruppo diventano visibili insieme oppure il database resta invariato.
- **Transazione:** confine breve nel database che realizza tale atomicita; non sostituisce autorizzazione o controllo delle versioni.
- **Concorrenza ottimistica:** non blocca gli item mentre l'utente sceglie; al salvataggio rileva se qualcuno li ha cambiati.
- **Successo parziale:** ogni item puo avere un esito diverso; richiede un contratto di risposta e una riconciliazione UI per elemento.

Il successo parziale e tecnicamente valido, ma non dovrebbe essere simulato con piu chiamate indipendenti dal browser: una caduta di rete lascerebbe un esito difficile da ricostruire e moltiplicherebbe autorizzazione e feedback. Se venisse scelto, il backend dovrebbe comunque ricevere un solo comando, restituire esiti strutturati per ID e non rivelare se un ID estraneo esiste. Questa complessita e giustificata solo da volumi o contesa osservati.

Gli errori devono distinguere input vuoto o oltre limite, ID duplicati normalizzati, elemento non disponibile, accesso non consentito, transizione non valida e conflitto. I log possono includere command ID, numero richiesto, numero elaborato, durata e categoria d'errore; non nomi o categorie degli item. Un command ID puo rendere sicuro il retry dopo una risposta persa, ma la durata di conservazione dell'esito e una decisione tecnica successiva, non una nuova funzione utente.

## best practices microsoft infrastructure

Non servono nuove risorse Azure. Function App, PostgreSQL e Application Insights/OpenTelemetry condivisi sono sufficienti. Service Bus, Durable Functions, lock distribuiti e orchestratori aggiungerebbero stato e recupero per un'operazione breve che resta nello stesso database.

PostgreSQL deve essere l'arbitro tra istanze della Function App: condizioni su stato/versione, transazione e vincoli sono affidabili anche quando due richieste arrivano a istanze diverse. Lock in memoria e session affinity non lo sono. Indici su perimetro famiglia, ID e stato possono aiutare la lettura del batch, ma vanno verificati sui piani di esecuzione e sui volumi reali.

L'osservabilita minima comprende dimensione del batch, durata, esito atomico, conflitti, rifiuti di autorizzazione e rollback, sempre in forma aggregata. Alert solo su crescita sostenuta di errori server o latenza, non su un singolo conflitto utente. Il limite massimo del batch protegge tempo di transazione, payload e consumo database; il valore va misurato e approvato, non inventato nella ricerca.

## flow chart

```mermaid
flowchart TD
    A["Membro attiva la selezione multipla"] --> B["Seleziona uno o piu item visibili"]
    B --> C{"Conferma Completa N?"}
    C -- No --> D["Annulla la selezione senza modifiche"]
    C -- Si --> E["Client invia ID distinti e versioni"]
    E --> F["Server ricava identita, famiglia e permesso"]
    F --> G["Carica e autorizza ogni ID"]
    G --> H{"Tutti esistono, sono visibili, Active e alla versione attesa?"}
    H -- No --> I["Nessuna modifica; errore strutturato"]
    H -- Si --> J["Aggiorna item e timeline nella transazione"]
    J --> K{"Commit riuscito senza conflitti?"}
    K -- No --> L["Rollback completo e ricarica richiesta"]
    K -- Si --> M["Tutti gli item risultano Completed"]
    M --> N{"Undo bulk previsto dal prodotto?"}
    N -- No --> O["Feedback finale con conteggio"]
    N -- Si --> P["Mostra unica azione di recupero secondo regole da approvare"]
```

## user experience

La vista principale resta riconoscibile. La modalita di selezione aggiunge soltanto controlli necessari e una barra azioni compatta; non apre una nuova schermata. Il filtro corrente rimane visibile, ma il perimetro di «Seleziona tutti» deve essere dichiarato prima di offrire quel comando.

```text
+--------------------------------+
| [Tutte] [Spesa] [Casa]         |
|                                |
| [x] Latte                      |
| [ ] Pasta                      |
| [x] Lamette                    |
|                                |
| 2 selezionati                  |
| [Annulla]        [Completa 2]  |
+--------------------------------+
```

```text
+--------------------------------+
| La lista e cambiata            |
| Nessun item e stato completato |
|                                |
| [Ricarica lista]               |
+--------------------------------+
```

- **Loading:** barra azioni in stato occupato, lista ancora leggibile e nessuna seconda conferma inviabile.
- **Empty:** se non esistono item attivi, la modalita di selezione non e disponibile; dopo successo sugli ultimi item compare lo stato vuoto normale.
- **Errore:** nell'esito tutti-o-nessuno si dichiara esplicitamente che nessun item e cambiato; dopo ricarica la selezione non viene riapplicata alla cieca.
- **Successo:** le righe scompaiono insieme, il conteggio e annunciato e il focus passa a una posizione prevedibile.
- **Undo:** wireframe e comportamento finali dipendono dalla decisione aperta tra recupero aggregato, individuale o assente; non mostrare N snackbar individuali per impostazione predefinita senza una scelta prodotto.
