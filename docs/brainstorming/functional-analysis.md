# Analisi funzionale — KinList

## 1. Scopo del documento

Questo documento definisce il comportamento approvato di KinList dal punto di vista degli utenti, dei membri di una famiglia e delle regole del prodotto.

KinList permette ai membri della stessa famiglia di trasformare una registrazione vocale in una lista di item condivisi, correggere nomi e categorie, filtrare la lista e completare uno o più item. L'esperienza principale resta: **Parla → Ottieni la lista → Spunta**.

Il documento include inoltre l'onboarding familiare obbligatorio, la gestione essenziale della famiglia, gli inviti tramite codice, l'uscita dalla famiglia, la selezione multipla e il perimetro di visibilità predisposto per gli item. Consolida lo scope confermato e i documenti in `docs/kinlist/research/tasks/`, senza descrivere soluzioni implementative.

## 2. Glossario

- **Famiglia**: unico perimetro condiviso attivo a cui un utente può appartenere e nel quale vengono gestiti i dati KinList condivisi.
- **Appartenenza**: relazione tra un utente e una famiglia; può essere attiva o inattiva.
- **Membro**: utente autenticato con appartenenza attiva alla famiglia corrente.
- **Onboarding familiare**: passaggio obbligatorio per un utente autenticato senza famiglia attiva, che deve crearne una oppure unirsi mediante codice.
- **Codice d'invito**: valore opaco, temporaneo, monouso e revocabile che consente a un utente autenticato senza famiglia attiva di unirsi alla famiglia che lo ha generato.
- **Invito attivo**: codice non ancora usato, revocato o scaduto; nell'elenco è rappresentato solo dai suoi metadati, mai dal valore segreto.
- **Item**: voce della lista, con nome, categorie, stato, visibilità e informazioni su creazione e ultima modifica.
- **Shared**: visibilità che rende un item accessibile ai membri attivi della stessa famiglia.
- **Personal**: visibilità predisposta che rende un item accessibile soltanto al suo autore; in questa versione non può essere scelta o modificata dall'interfaccia.
- **Registrazione**: singola acquisizione vocale da cui KinList ricava uno o più item ordinati.
- **Gruppo di registrazione**: insieme degli item generati dalla stessa registrazione.
- **Categoria**: etichetta associabile a uno o più item; un item può avere più categorie.
- **Drawer**: pannello laterale usato per dettaglio, modifica e cronologia dell'item.
- **Timeline**: sequenza leggibile degli eventi rilevanti dell'item.
- **Modalità di selezione**: stato della lista in cui compaiono checkbox per scegliere più item visibili.
- **Annulla**: azione temporanea che riporta allo stato attivo uno o più item appena completati.
- **Inattivazione reversibile**: conservazione concettuale di utente, appartenenza o famiglia come inattivi, senza considerarli immediatamente eliminati in modo definitivo.
- **Pagina**: porzione limitata e ordinata di una collezione; non implica che tutti i dati esistenti siano stati caricati.
- **Cursore opaco**: riferimento restituito da KinList per raggiungere la pagina precedente o successiva senza esporre posizione o struttura interna dei dati.

## 3. Attori

### 3.1 Utente autenticato senza famiglia

- **Obiettivo**: ottenere l'accesso a KinList creando una famiglia o unendosi a una famiglia esistente con un codice.
- **Responsabilità**: fornire un nome valido quando crea una famiglia oppure un codice ricevuto manualmente quando si unisce.
- **Permessi**: può completare esclusivamente l'onboarding; non può vedere o usare dati KinList familiari prima di avere un'appartenenza attiva.

### 3.2 Membro della famiglia

- **Obiettivo**: creare e gestire rapidamente la lista condivisa e le impostazioni essenziali della propria famiglia.
- **Responsabilità**: concedere consapevolmente il permesso al microfono, verificare gli item generati, condividere manualmente i codici d'invito e confermare le azioni distruttive.
- **Permessi**: vede i dati consentiti della propria famiglia; può registrare, modificare e completare item Shared, generare e revocare codici d'invito e lasciare la famiglia. Tutti i membri hanno le stesse capacità; non esistono ruoli amministrativi.

### 3.3 Sistema KinList

- **Obiettivo**: riconoscere l'utente, applicare appartenenza e visibilità, trasformare la voce in item validi e mantenere coerenti lista, famiglia e cicli di vita.
- **Responsabilità**: non esporre dati di altre famiglie o item Personal altrui; attribuire correttamente le azioni; evitare risultati parziali; applicare scadenze, revoche, inattivazioni e conservazione approvate.

### 3.4 Responsabile del servizio

- **Obiettivo**: verificare che elaborazioni, errori e pulizie previste funzionino correttamente.
- **Permessi**: consulta esclusivamente informazioni operative aggregate, senza una nuova interfaccia KinList e senza contenuti personali.

## 4. In scope

- Accesso tramite il sistema di identità approvato e creazione di un solo profilo applicativo al primo accesso riconosciuto.
- Una sola famiglia attiva per utente.
- Dopo il login, accesso diretto a KinList per chi è già associato; onboarding obbligatorio per chi non lo è.
- Creazione di una famiglia mediante il solo nome, con il creatore come unico membro iniziale.
- Unione a una famiglia mediante codice d'invito.
- Isolamento dei dati per famiglia e accesso negato esplicito quando l'utente non può accedere al perimetro richiesto.
- Lista condivisa con indicazione visiva dell'autore della creazione tramite avatar circolare con iniziali.
- Predisposizione delle visibilità Personal e Shared; tutti i nuovi item di questa versione sono Shared.
- PWA installabile, responsive e mobile-first, utilizzabile anche dal browser.
- Interfaccia minimale con temi chiaro, scuro e di sistema; italiano predefinito e inglese supportato come fallback, con parità obbligatoria dei testi.
- Registrazione con un tocco per iniziare e un secondo tocco per terminare, senza pressione prolungata e senza anteprima audio.
- Trasformazione sincrona di una registrazione lunga al massimo 60 secondi e di dimensione stimata massima 12 MB in uno o più item ordinati con categorie.
- Popup chiaro all'apertura dell'app per l'utente che non ha ancora concesso o negato il permesso microfono, con spiegazione che la voce viene usata solo per generare tramite IA una lista e che la concessione del permesso vale come consenso a questo uso.
- Aggiunta di nuove registrazioni alla lista esistente e ordinamento stabile per gruppo e ordine riconosciuto.
- Visualizzazione dei soli item attivi e filtro per una categoria alla volta.
- Drawer laterale per dettaglio, modifica di nome e categorie, creazione di categorie, timeline e salvataggio esplicito.
- Completamento singolo con Annulla disponibile per cinque secondi.
- Modalità di selezione multipla con `Seleziona`, checkbox, `Seleziona tutti` sugli item della pagina filtrata corrente, completamento tutti-o-nessuno e un solo `Annulla N` atomico entro cinque secondi.
- Conservazione degli item completati per 30 giorni dal completamento e successiva eliminazione definitiva.
- Pulsante a ingranaggio flottante, fisso in basso a destra e rispettoso della safe area, che apre la pagina Impostazioni generali esistente.
- Aggiunta della voce Famiglia alle Impostazioni generali, senza rimuovere lingua, tema, tutorial o PWA.
- Pagina Famiglia con nome, membri, inviti attivi e relativi metadati, azioni Invita e Lascia famiglia.
- Generazione e revoca dei codici da parte di qualunque membro; condivisione esclusivamente manuale.
- Uscita volontaria confermata, revoca dei codici creati dal membro uscente e ritorno all'onboarding.
- Riattivazione di un'appartenenza storica quando l'ex membro usa un nuovo codice valido.
- Inattivazione della famiglia e dei dati KinList collegati quando esce l'ultimo membro.
- Inattivazione reversibile di utenti, appartenenze e famiglie, con pulizia definitiva non prima di 30 giorni di inattività.
- Feedback per caricamento, successo, assenza di risultati, errori recuperabili, accesso negato e connettività insufficiente.
- Aggiornamento collaborativo tramite refresh manuale e gestione esplicita dei cambiamenti concorrenti.
- Catalogo categorie condiviso per famiglia, senza categorie predefinite, con riuso delle categorie esistenti quando possibile.
- Nessun dato personale mantenuto nelle cache applicative del browser.
- Lettura paginata e limitata delle collezioni, con cursori opachi avanti/indietro e nessun caricamento integrale implicito.
- Shell PWA disponibile offline senza lista o altri dati personali; operazioni remote e registrazione richiedono la rete e non vengono accodate.
- Elaborazione vocale in una richiesta di massimo 90 secondi, con al più un tentativo automatico aggiuntivo dopo un guasto transitorio senza risposta e massimo 1000 item generati.
- Manutenzione giornaliera alle 00:00 UTC per retention e pulizia degli inattivi, come due esiti distinti.

## 5. Out of scope

- Più famiglie attive per lo stesso utente e qualunque selettore o cambio diretto di famiglia.
- Più liste nominabili per la stessa famiglia.
- Ruoli, amministratori, proprietari, cambio proprietario o differenze di permesso tra membri.
- Rimozione di altri membri dalla famiglia.
- Interfaccia per eliminare l'account utente.
- Inviti automatici tramite email, notifica, link inviato dall'app, rubrica o altri canali.
- Ricerca di utenti o famiglie.
- Visualizzazione del valore segreto di un codice dopo la sua creazione.
- Codici permanenti, riutilizzabili o validi per più persone.
- Interfaccia per creare item Personal, scegliere la visibilità o convertire item tra Personal e Shared.
- Creazione manuale di item.
- Completamento multiplo con successo parziale, Annulla per singolo item del gruppo o più notifiche di recupero per lo stesso bulk.
- Schermata degli item completati o recupero dall'interfaccia dopo la scadenza di Annulla.
- Anteprima, modifica o riproduzione dell'audio prima della generazione.
- Conferma della trascrizione o anteprima degli item prima del salvataggio.
- Conservazione permanente dell'audio o della trascrizione.
- Funzionamento offline completo, accodamento offline delle registrazioni o sincronizzazione differita dell'audio.
- Conservazione dell'audio in file locali o temporanei, cache, database browser, storage remoto o code.
- Notifiche, promemoria, analytics di prodotto, gamification e aggiornamento realtime tra dispositivi.
- Nuovi tipi di evento, funzionalità amministrative o altre capacità non richieste dallo scope confermato.

## 6. Flussi utente

### FLOW-001 — Accesso, riconoscimento e instradamento

- **Trigger**: l'utente completa il login.
- **Precondizioni**: l'identità è stata riconosciuta dal sistema approvato.
- **Percorso principale**:
  1. KinHub collega l'identità a un solo profilo applicativo, creandolo se necessario.
  2. Verifica se esiste un'appartenenza familiare attiva.
  3. Se l'appartenenza esiste, apre direttamente KinList nella famiglia associata.
  4. Se non esiste, mostra la scelta obbligatoria tra `Crea una famiglia` e `Unisciti con un codice`.
- **Alternative**: un profilo già esistente viene riutilizzato; un'appartenenza storica inattiva non permette accesso diretto.
- **Errori e recupero**: un errore di sessione richiede un nuovo accesso; un errore di verifica offre Riprova senza mostrare dati familiari.
- **Risultato osservabile**: l'utente entra direttamente nel servizio oppure resta nell'onboarding, mai in una lista apparentemente vuota per mancanza di famiglia.

### FLOW-002 — Apertura della lista

- **Trigger**: il membro associato entra in KinList.
- **Precondizioni**: appartenenza attiva alla famiglia.
- **Percorso principale**:
  1. KinList carica gli item attivi visibili e le categorie disponibili nella famiglia.
  2. Se esistono item, li mostra in ordine deterministico e colloca il microfono in basso al centro.
  3. Ogni riga mostra le informazioni essenziali e le iniziali dell'autore.
  4. Il pulsante Seleziona rende disponibile la modalità di selezione multipla.
- **Alternative**: senza item attivi visibili, KinList mostra il grande pulsante microfono al centro e non offre la selezione multipla.
- **Errori e recupero**: un errore offre Riprova; l'accesso negato non viene rappresentato come lista vuota e non mostra alcun contenuto familiare.
- **Risultato osservabile**: il membro distingue lista popolata, lista realmente vuota, caricamento, errore e accesso negato.

### FLOW-003 — Registrazione vocale

- **Trigger**: l'app si apre per un membro senza una scelta precedente sul permesso microfono oppure il membro tocca il pulsante microfono.
- **Precondizioni**: funzione supportata, connessione disponibile e permesso di registrazione concedibile.
- **Percorso principale**:
  1. Se il permesso microfono non è ancora stato deciso, KinList mostra subito un popup chiaro che spiega che la voce sarà usata solo per generare tramite IA una lista e che concedere il permesso equivale ad acconsentire a questo uso.
  2. Il membro può chiudere il popup senza concedere il permesso e continuare a usare il resto dell'app senza registrare.
  3. Quando il membro sceglie di proseguire con la registrazione, KinList richiede il permesso del microfono.
  4. Al consenso, la registrazione parte.
  5. Il controllo comunica chiaramente che sta ascoltando senza spostare la propria area attiva.
  6. Il membro tocca nuovamente il controllo.
  7. KinList termina la registrazione, rilascia il microfono e avvia l'elaborazione di un audio non vuoto.
- **Alternative**: a 60 secondi o alla dimensione massima stimata di 12 MB, KinList termina automaticamente la registrazione e lo comunica.
- **Errori e recupero**: permesso negato, popup chiuso senza consenso, dispositivo assente o occupato, formato non supportato, audio vuoto, superamento dei limiti o perdita di rete producono messaggi specifici e possibilità di riprovare.
- **Risultato osservabile**: l'utente riceve prima un'informazione chiara sull'uso della voce; il microfono non resta attivo e una sola registrazione passa all'elaborazione.

### FLOW-004 — Generazione e aggiunta degli item

- **Trigger**: KinList riceve una registrazione valida.
- **Precondizioni**: appartenenza ancora attiva e registrazione non già completata.
- **Percorso principale**:
  1. KinList mostra uno stato non ambiguo, per esempio `Creo la lista`, senza percentuali inventate.
  2. Interpreta la registrazione e ricava item e categorie nella lingua parlata.
  3. Verifica che il risultato sia valido e coerente.
  4. Assegna a tutti i nuovi item visibilità Shared e l'autore corrente.
  5. Riusa le categorie della famiglia quando possibile e crea quelle nuove necessarie.
  6. Aggiunge l'intero gruppo mantenendo l'ordine riconosciuto.
  7. Mostra il gruppo in cima alla lista e annuncia l'esito.
- **Alternative**: registrazioni successive aggiungono altri gruppi senza sostituire gli item esistenti.
- **Errori e recupero**: se non vengono riconosciuti item, non vengono create righe vuote; se l'operazione fallisce, non resta un gruppo parziale e il membro può riprovare senza duplicazioni.
- **Risultato osservabile**: l'intero gruppo Shared compare una sola volta, in cima e nell'ordine pronunciato.

### FLOW-005 — Filtro per categoria

- **Trigger**: il membro seleziona una categoria nel carosello superiore.
- **Precondizioni**: esistono categorie e item attivi visibili.
- **Percorso principale**:
  1. KinList evidenzia una sola categoria con più di un indizio cromatico.
  2. Richiede la prima pagina degli item attivi visibili associati alla categoria, applicando il filtro prima della paginazione e senza cambiarne l'ordine.
  3. Il membro naviga tra le pagine disponibili oppure rimuove facilmente il filtro e torna alla prima pagina della lista completa.
- **Alternative**: nessuna selezione equivale alla visualizzazione di tutti gli item attivi visibili.
- **Errori e recupero**: un filtro senza corrispondenze mostra uno stato vuoto dedicato e l'azione per rimuoverlo; un cursore invalido conserva la vista e permette di ripartire dalla prima pagina filtrata.
- **Risultato osservabile**: il filtro modifica soltanto la vista e determina il perimetro di `Seleziona tutti`.

### FLOW-006 — Dettaglio, modifica e cronologia

- **Trigger**: il membro apre il dettaglio di un item attivo visibile.
- **Precondizioni**: il membro può leggere e modificare l'item.
- **Percorso principale**:
  1. Un drawer entra da destra e mostra nome, metadati e le prime pagine di categorie e timeline.
  2. Il membro modifica il nome e/o seleziona più categorie.
  3. Può creare una categoria valida e associarla all'item.
  4. Conferma con un salvataggio esplicito.
  5. KinList aggiorna ultima modifica e timeline solo se i dati sono realmente cambiati.
  6. La riga resta nella posizione originaria.
- **Alternative**: il membro consulta il dettaglio senza modifiche e naviga avanti o indietro nelle collezioni paginabili.
- **Errori e recupero**: errori di validazione appaiono vicino al campo; errori generali preservano l'input; un cambiamento concorrente non viene sovrascritto silenziosamente.
- **Risultato osservabile**: lista, dettaglio e timeline mostrano lo stato aggiornato senza riordinare l'item.

### FLOW-007 — Completamento singolo e Annulla

- **Trigger**: il membro spunta un item attivo.
- **Precondizioni**: item ancora attivo, visibile e accessibile al membro.
- **Percorso principale**:
  1. L'item scompare immediatamente dalla lista.
  2. KinList lo porta nello stato completato e registra momento e autore.
  3. Mostra Annulla per cinque secondi.
  4. Se il membro non annulla, l'item resta completato e non è più recuperabile dall'interfaccia.
- **Alternativa**: se Annulla viene accettato nella finestra disponibile, l'item torna attivo nella posizione determinata dai dati originari.
- **Errori e recupero**: se il completamento fallisce, la riga ricompare; se Annulla non è più disponibile o fallisce, KinList mostra lo stato effettivo senza fingere il ripristino.
- **Risultato osservabile**: non esistono duplicati né cambi d'ordine dovuti al completamento o all'annullamento.

### FLOW-008 — Eliminazione degli item completati

- **Trigger**: controllo periodico degli item completati.
- **Precondizioni**: esistono item completati da almeno 30 giorni.
- **Percorso principale**:
  1. Ogni giorno alle 00:00 UTC KinList individua per pagine limitate gli item ancora completati con data di completamento minore o uguale al limite di 30 periodi continuativi di 24 ore.
  2. Li elimina definitivamente insieme alle informazioni collegate previste mediante un'elaborazione limitata.
  3. Rende verificabile l'esito retention separatamente dalla pulizia degli inattivi, senza contenuti personali.
- **Alternative**: zero item idonei è un esito riuscito.
- **Errori e recupero**: un errore non causa cancellazioni indiscriminate; gli item non eliminati restano idonei a un tentativo successivo.
- **Risultato osservabile**: nessuna nuova UI per il membro; la conservazione approvata viene rispettata.

### FLOW-009 — Creazione della famiglia

- **Trigger**: l'utente senza famiglia sceglie `Crea una famiglia`.
- **Precondizioni**: utente autenticato senza appartenenza attiva.
- **Percorso principale**:
  1. KinHub mostra il solo campo Nome famiglia.
  2. L'utente inserisce un nome valido e conferma.
  3. KinHub crea la famiglia e aggiunge esclusivamente il creatore.
  4. L'utente entra direttamente nel servizio.
- **Alternative**: l'utente può tornare alla scelta e usare un codice.
- **Errori e recupero**: input non valido viene spiegato vicino al campo; un invio ripetuto non crea famiglie duplicate; un errore preserva il nome e offre Riprova.
- **Risultato osservabile**: esiste una sola famiglia attiva per l'utente e il creatore è il solo membro iniziale.

### FLOW-010 — Apertura delle Impostazioni e della pagina Famiglia

- **Trigger**: il membro attiva l'ingranaggio flottante in basso a destra.
- **Precondizioni**: membro nella vista KinList e nessuna superficie modale che richieda il focus esclusivo.
- **Percorso principale**:
  1. L'ingranaggio apre la pagina Impostazioni generali esistente.
  2. La pagina continua a mostrare lingua, tema, tutorial e PWA e include la nuova voce Famiglia.
  3. Il membro apre la route canonica `/settings/family`, raggiungibile anche da URL diretto, refresh e navigazione Indietro/Avanti.
  4. KinHub mostra nome, membri paginati, inviti attivi con metadati, Invita e Lascia famiglia. Per un membro senza nome usa `Membro`/`Member` e iniziale `?`.
- **Alternative**: il membro torna alla vista precedente con la normale navigazione.
- **Errori e recupero**: caricamento ed errore non mostrano dati precedenti non autorizzati; accesso negato non viene confuso con un elenco vuoto.
- **Risultato osservabile**: il membro raggiunge le impostazioni familiari senza che l'ingranaggio copra contenuti, microfono o feedback temporanei.

### FLOW-011 — Generazione, condivisione, uso e revoca di un codice

- **Trigger**: un membro seleziona Invita oppure revoca un invito attivo; un utente senza famiglia sceglie `Unisciti con un codice`.
- **Precondizioni**: per generare o revocare, appartenenza attiva; per usare il codice, autenticazione e nessuna famiglia attiva.
- **Percorso principale — generazione e condivisione**:
  1. Se esistono meno di cinque inviti attivi, il membro genera un codice opaco di 12 caratteri Crockford Base32, visualizzato come `XXXX-XXXX-XXXX`, monouso e valido sette giorni.
  2. KinHub mostra il valore segreto soltanto in questo momento, insieme alla scadenza.
  3. Il membro lo condivide manualmente fuori da KinHub.
  4. Tornando alla pagina Famiglia, vede soltanto i metadati dell'invito attivo, non il codice.
- **Percorso principale — unione**:
  1. L'utente inserisce il codice e conferma.
  2. Se il codice è disponibile, KinHub crea o riattiva l'appartenenza e consuma il codice nello stesso esito indivisibile.
  3. L'utente entra direttamente nel servizio.
- **Alternativa — revoca**: qualunque membro può richiedere e confermare la revoca di un invito attivo, che diventa immediatamente inutilizzabile.
- **Errori e recupero**: spazi, trattini e maiuscole vengono normalizzati. Codice inesistente, scaduto, revocato o già usato produce lo stesso messaggio generico; due usi contemporanei consentono un solo successo. La barriera iniziale per singola istanza limita dopo 5 tentativi in 5 minuti per identità o 20 in 5 minuti per origine di rete attendibile e indica quando riprovare. Nessun errore lascia uno stato parziale.
- **Risultato osservabile**: il codice non viene più mostrato dopo la creazione, non rivela la famiglia prima del successo e non può essere riutilizzato.

### FLOW-012 — Uscita dalla famiglia

- **Trigger**: il membro seleziona Lascia famiglia.
- **Precondizioni**: appartenenza attiva.
- **Percorso principale**:
  1. KinList spiega le conseguenze e richiede conferma.
  2. Alla conferma, rende inattiva l'appartenenza del membro.
  3. Revoca tutti i codici d'invito creati dal membro uscente.
  4. Rimuove immediatamente l'accesso ai dati della famiglia.
  5. Riporta l'utente all'onboarding.
- **Alternativa**: annullando la conferma non cambia nulla.
- **Caso ultimo membro**: se non restano membri attivi, KinHub rende inattivi la famiglia e tutti i dati KinList collegati.
- **Errori e recupero**: un errore non produce un'uscita parziale e mantiene l'utente nello stato effettivo; un accesso successivo a dati non più consentiti mostra accesso negato senza contenuti.
- **Risultato osservabile**: l'ex membro deve creare o unirsi a una famiglia; potrà riattivare una propria appartenenza storica soltanto con un nuovo codice valido.

### FLOW-013 — Selezione multipla, completamento e Annulla N

- **Trigger**: il membro preme Seleziona nella lista con item attivi visibili.
- **Precondizioni**: esiste almeno un item visibile e completabile.
- **Percorso principale**:
  1. KinList entra in modalità di selezione e mostra una checkbox per ogni item visibile.
  2. Il membro seleziona singoli item oppure usa Seleziona tutti, che include soltanto gli item della pagina filtrata corrente, fino al limite di lettura di 5000.
  3. KinList mostra il conteggio e l'azione `Completa N`.
  4. Alla conferma, completa tutti gli item selezionati oppure nessuno, con un unico esito osservabile.
  5. Gli item scompaiono insieme e compare un unico `Annulla N` per cinque secondi.
  6. Se Annulla N viene accettato, tutti gli item del gruppo tornano attivi insieme nelle rispettive posizioni.
- **Alternative**: il membro esce dalla modalità con Annulla selezione senza modificare item; cambiare filtro aggiorna il perimetro visibile e la selezione viene resa coerente con la vista.
- **Errori e recupero**: se anche un solo item non è più disponibile, visibile o modificabile, nessun item viene completato; KinList aggiorna la lista e spiega il cambiamento. Un Annulla N fallito non produce ripristini parziali.
- **Risultato osservabile**: selezione, completamento e recupero hanno sempre un esito unico e comprensibile.

### FLOW-014 — Inattivazione e pulizia definitiva

- **Trigger**: un utente, un'appartenenza o una famiglia entra nello stato inattivo secondo i flussi approvati.
- **Precondizioni**: lo stato non deve più concedere accesso ordinario.
- **Percorso principale**:
  1. KinList conserva concettualmente l'elemento come inattivo, permettendo i soli recuperi esplicitamente previsti, come la riattivazione di un'appartenenza storica tramite nuovo codice.
  2. Ogni giorno alle 00:00 UTC, trascorsi almeno 30 periodi continuativi di 24 ore dall'inattivazione, l'elemento può essere incluso in pagine limitate nella pulizia definitiva insieme ai dati collegati previsti.
  3. Prima della cancellazione KinList ricontrolla che l'elemento sia ancora inattivo e privo di collegamenti attivi.
- **Alternative**: una riattivazione valida prima della pulizia interrompe l'idoneità alla cancellazione definitiva.
- **Errori e recupero**: nessuna pulizia anticipata; un errore lascia gli elementi ancora inattivi per un controllo successivo.
- **Risultato osservabile**: l'inattivazione è immediata per l'accesso, mentre l'eliminazione definitiva non avviene prima della soglia approvata.

## 7. Requisiti funzionali

| ID | Descrizione verificabile | Attore | Valore/risultato | Origine | Flussi |
|---|---|---|---|---|---|
| FR-001 | KinHub deve riconoscere l'utente mediante la coppia obbligatoria issuer e identificativo oggetto del token validato, senza usare nome o email come fallback identificativo. | Utente | Accesso con identità stabile e non ambigua. | Decisione approvata | FLOW-001 |
| FR-002 | Al primo accesso KinHub deve creare un solo profilo applicativo collegato all'identità e riutilizzarlo agli accessi successivi. | Utente | Identità applicativa stabile e non duplicata. | Decisione approvata | FLOW-001 |
| FR-003 | KinHub deve autorizzare il contesto famiglia e ogni KinService deve applicare anche le proprie regole di visibilità dei dati. | Utente | Azioni circoscritte al perimetro consentito. | Scope confermato | FLOW-001, FLOW-002, FLOW-006, FLOW-007, FLOW-010–FLOW-013 |
| FR-004 | Un membro deve vedere gli item Shared della propria famiglia e gli eventuali item Personal di cui è autore, mai dati non consentiti. | Membro | Isolamento e visibilità corretti. | Scope confermato | FLOW-002, FLOW-006 |
| FR-005 | Tutti i membri della stessa famiglia devono poter modificare e completare gli item Shared, indipendentemente dall'autore. | Membro | Collaborazione familiare senza ruoli. | Scope confermato | FLOW-006, FLOW-007, FLOW-013 |
| FR-006 | Ogni riga deve identificare l'autore della creazione con un avatar circolare contenente le iniziali. | Membro | Provenienza comprensibile. | Richiesta approvata | FLOW-002 |
| FR-007 | In assenza di item attivi visibili KinList deve mostrare al centro il controllo microfono, senza elementi superflui. | Membro | Avvio semplice. | Richiesta approvata | FLOW-002 |
| FR-008 | Con item attivi KinList deve mostrare la lista e mantenere il controllo microfono in basso al centro. | Membro | Registrazioni successive sempre disponibili. | Richiesta approvata | FLOW-002, FLOW-004 |
| FR-009 | Un tocco deve avviare la registrazione e un secondo tocco deve terminarla, senza pressione prolungata. | Membro | Gesto mobile immediato. | Richiesta approvata | FLOW-003 |
| FR-010 | Durante la registrazione KinList deve comunicare chiaramente lo stato di ascolto senza affidarsi soltanto a colore o animazione. | Membro | Prevenzione di registrazioni involontarie. | Miglioria qualitativa approvata | FLOW-003 |
| FR-011 | KinList non deve mostrare un'anteprima audio o una conferma intermedia. | Membro | Flusso breve. | Richiesta approvata | FLOW-003, FLOW-004 |
| FR-012 | KinList deve trasformare in una richiesta sincrona di massimo 90 secondi una registrazione valida, lunga al massimo 60 secondi e con dimensione stimata massima di 12 MB, in non più di 1000 item distinti con categorie e aggiungerli direttamente. | Membro | Creazione vocale in un solo passaggio limitato. | Decisione approvata | FLOW-003, FLOW-004 |
| FR-013 | Una duplicazione di trasporto della stessa registrazione non deve creare un secondo gruppo; se la risposta si perde dopo il salvataggio, la PWA deve recuperare l'esito usando il solo identificativo della registrazione. | Membro | Recupero sicuro senza conservare o reinviare audio. | Decisione approvata | FLOW-004 |
| FR-014 | L'aggiunta di un gruppo deve essere completa: tutti gli item validi oppure nessuno. | Membro | Nessun gruppo parziale. | Miglioria qualitativa approvata | FLOW-004 |
| FR-015 | I gruppi più recenti devono apparire in cima; nel gruppo deve restare l'ordine riconosciuto; una modifica non deve cambiare posizione. | Membro | Ordine stabile e prevedibile. | Richiesta approvata | FLOW-002, FLOW-004, FLOW-006 |
| FR-016 | La lista principale deve mostrare soltanto pagine limitate di item attivi visibili al membro. | Membro | Vista focalizzata, riservata e limitata. | Scope confermato | FLOW-002, FLOW-005 |
| FR-017 | Il carosello superiore deve consentire di selezionare una categoria alla volta, evidenziarla e rimuovere facilmente il filtro; il filtro deve essere applicato prima della paginazione. | Membro | Navigazione rapida e coerente per categoria. | Decisione approvata | FLOW-005 |
| FR-018 | Un filtro senza risultati deve essere distinto dalla lista realmente vuota. | Membro | Stato comprensibile e recuperabile. | Miglioria qualitativa approvata | FLOW-005 |
| FR-019 | Il dettaglio deve aprirsi in un drawer laterale, non in un popup tradizionale. | Membro | Modifica contestuale. | Richiesta approvata | FLOW-006 |
| FR-020 | Il drawer deve permettere di modificare il nome, selezionare più categorie, creare una categoria valida e salvare solo con azione esplicita. | Membro | Correzione senza salvataggi impliciti. | Decisione approvata | FLOW-006 |
| FR-021 | Il drawer deve mostrare in sola lettura autore e data di creazione e ultima modifica. | Membro | Tracciabilità leggibile. | Richiesta approvata | FLOW-006 |
| FR-022 | La timeline deve mostrare in ordine creazione, modifica, completamento e riattivazione con autore e data/ora. | Membro | Storia essenziale dell'item. | Decisione approvata | FLOW-006, FLOW-007, FLOW-013 |
| FR-023 | Un cambiamento concorrente non deve essere sovrascritto silenziosamente da un'altra sessione. | Membro | Prevenzione della perdita di modifiche. | Miglioria qualitativa approvata | FLOW-006, FLOW-013 |
| FR-024 | Spuntare un singolo item deve nasconderlo, completarlo e mostrare Annulla per cinque secondi. | Membro | Completamento rapido e reversibile. | Decisione approvata | FLOW-007 |
| FR-025 | Annulla, se accettato, deve riportare l'item ad attivo nella posizione originaria senza duplicarlo. | Membro | Recupero dall'azione accidentale. | Richiesta approvata | FLOW-007 |
| FR-026 | Il controllo giornaliero delle 00:00 UTC deve individuare e processare gli item ancora completati oltre il limite di 30 periodi continuativi di 24 ore; quelli non eliminati entro il budget operativo devono restare idonei ai controlli successivi. | Sistema | Nessuna eliminazione anticipata e avanzamento verificabile della conservazione. | Decisione approvata | FLOW-008 |
| FR-027 | KinList deve essere installabile come PWA e restare utilizzabile dal browser in layout responsive mobile-first. | Membro | Accesso coerente su dispositivi diversi. | Richiesta approvata | Tutti i flussi utente |
| FR-028 | L'interfaccia deve supportare i temi previsti e avere italiano come lingua predefinita e inglese come lingua supportata e fallback, con parità obbligatoria dei testi. | Membro | Esperienza coerente e bilingue. | Regola autorevole di KinHub | Tutti i flussi utente |
| FR-029 | Senza connettività KinList deve mostrare soltanto la shell pubblica, senza dati personali, impedire operazioni remote e registrazione e spiegarne brevemente il motivo. | Membro | Nessun falso successo o dato personale persistito offline. | Decisione approvata | FLOW-001–FLOW-004, FLOW-009–FLOW-013 |
| FR-030 | Il responsabile del servizio deve poter conoscere esiti, errori, conteggi e durate aggregate delle operazioni senza contenuti personali. | Responsabile del servizio | Verificabilità operativa rispettosa della privacy. | Decisione approvata | FLOW-004, FLOW-008, FLOW-014 |
| FR-031 | Ogni utente deve avere al massimo una famiglia attiva e non deve poter creare o selezionare una seconda famiglia. | Utente | Perimetro familiare univoco. | Scope confermato | FLOW-001, FLOW-009, FLOW-011 |
| FR-032 | Dopo il login, KinHub deve risolvere il contesto famiglia condiviso: l'utente associato puo entrare nel KinService richiesto, mentre quello non associato deve scegliere obbligatoriamente se creare una famiglia o unirsi con un codice. | Utente | Accesso senza passaggi inutili e onboarding non eludibile. | Scope confermato | FLOW-001 |
| FR-033 | La creazione deve richiedere il nome della famiglia e aggiungere soltanto il creatore come membro iniziale. | Utente senza famiglia | Famiglia minima senza inviti impliciti. | Scope confermato | FLOW-009 |
| FR-034 | Un ingranaggio flottante, fisso in basso a destra e dentro la safe area, deve aprire la pagina Impostazioni generali esistente senza coprire controlli o contenuti. | Membro | Accesso discreto e sempre riconoscibile. | Scope confermato | FLOW-010 |
| FR-035 | Le Impostazioni generali devono aggiungere la voce Famiglia senza rimuovere lingua, tema, tutorial o PWA. | Membro | Estensione coerente delle impostazioni esistenti. | Scope confermato | FLOW-010 |
| FR-036 | La route `/settings/family` deve mostrare nome, membri paginati con soli nome e iniziali, inviti attivi con creatore, creazione e scadenza, oltre a Invita e Lascia famiglia; deve funzionare con URL diretto, refresh e cronologia browser. | Membro | Gestione familiare essenziale e navigazione ricostruibile. | Decisione approvata | FLOW-010–FLOW-012 |
| FR-037 | Tutti i membri devono poter generare e revocare codici d'invito. | Membro | Collaborazione senza ruoli amministrativi. | Scope confermato | FLOW-011 |
| FR-038 | Ogni codice deve contenere 12 caratteri Crockford Base32, essere mostrato come `XXXX-XXXX-XXXX`, essere opaco, monouso, revocabile, valido sette giorni e condiviso esclusivamente in modo manuale; una famiglia può averne al massimo cinque attivi. | Membro, utente senza famiglia | Invito pragmatico e limitato. | Decisione approvata | FLOW-011 |
| FR-039 | Il valore segreto del codice deve essere visibile soltanto alla creazione; successivamente l'elenco deve mostrare solo metadati non segreti. | Membro | Riduzione dell'esposizione del codice. | Scope confermato | FLOW-010, FLOW-011 |
| FR-040 | L'uso riuscito di un codice deve creare una nuova appartenenza o riattivarne una storica e consumare il codice come unico esito indivisibile. | Utente senza famiglia | Nessun uso doppio o stato parziale. | Scope confermato | FLOW-011 |
| FR-041 | Ogni membro deve poter lasciare la famiglia dopo conferma; l'uscita deve revocare i codici creati da chi esce e riportarlo all'onboarding. | Membro | Uscita consapevole e accesso revocato. | Scope confermato | FLOW-012 |
| FR-042 | Se esce l'ultimo membro, la famiglia e tutti i dati KinList collegati devono diventare inattivi. | Sistema | Chiusura coerente del perimetro senza membri. | Scope confermato | FLOW-012, FLOW-014 |
| FR-043 | Utenti, appartenenze e famiglie devono supportare inattivazione reversibile; la pulizia giornaliera può eliminarli soltanto dopo almeno 30 periodi continuativi di 24 ore e dopo aver ricontrollato inattività e assenza di collegamenti attivi. | Sistema | Recupero controllato e nessuna eliminazione anticipata. | Decisione approvata | FLOW-012, FLOW-014 |
| FR-044 | Il pulsante Seleziona deve attivare checkbox e azioni essenziali per scegliere più item attivi visibili. | Membro | Selezione multipla esplicita e reversibile. | Scope confermato | FLOW-013 |
| FR-045 | Seleziona tutti deve includere esclusivamente gli item della pagina filtrata corrente, fino a 5000. | Membro | Perimetro della selezione prevedibile. | Decisione approvata | FLOW-005, FLOW-013 |
| FR-046 | Completa N e il relativo Annulla N entro cinque secondi devono essere tutti-o-nessuno, con un solo feedback e senza esiti per singolo item. | Membro | Operazione bulk ampia ma indivisibile per l'utente. | Decisione approvata | FLOW-013 |
| FR-047 | Un item Personal deve essere accessibile soltanto al suo autore; un item Shared ai membri attivi della stessa famiglia. | Membro | Predisposizione coerente della visibilità. | Scope confermato | FLOW-002, FLOW-006, FLOW-007, FLOW-013 |
| FR-048 | In questa versione tutti i nuovi item devono essere Shared e non deve esistere una UI per creare Personal o convertire la visibilità. | Membro | Nessun ampliamento dell'esperienza corrente. | Scope confermato | FLOW-004, FLOW-006 |
| FR-049 | Ogni lettura di una collezione deve essere limitata e paginata mediante cursori opachi avanti/indietro; nessun flusso deve richiedere il caricamento integrale. | Tutti | Uso prevedibile di memoria e dati anche con collezioni grandi. | Decisione approvata | FLOW-002, FLOW-005, FLOW-006, FLOW-008, FLOW-010, FLOW-014 |
| FR-050 | La dimensione effettiva di una pagina deve essere il minore tra quella richiesta e il limite configurato, inizialmente e in assoluto pari a 5000; configurazioni non positive o superiori devono essere rifiutate. | Sistema | Limiti autorevoli e verificabili. | Decisione approvata | Tutti i flussi con collezioni |
| FR-051 | Un cursore invalido o non più coerente deve produrre un errore recuperabile senza mostrare dati fuori perimetro e permettere di ripartire dalla prima pagina. | Utente, membro | Navigazione sicura dopo cambiamenti concorrenti. | Miglioria qualitativa approvata | FLOW-002, FLOW-005, FLOW-006, FLOW-010 |
| FR-052 | KinList deve rilevare automaticamente la lingua parlata e mantenere la stessa lingua nei nomi e nelle categorie generate. | Membro | Risultato coerente con la registrazione senza scelta preliminare. | Decisione approvata | FLOW-004 |
| FR-053 | Audio e output grezzo devono esistere soltanto in memoria durante la richiesta ed essere rilasciati dopo successo, errore, annullamento o timeout. | Membro | Nessuna persistenza del contenuto vocale. | Decisione approvata | FLOW-003, FLOW-004 |
| FR-054 | Il tentativo di unione deve applicare, per singola istanza del servizio, una barriera iniziale di 5 tentativi in 5 minuti per identità e 20 in 5 minuti per origine di rete attendibile, indicando quando riprovare. | Utente senza famiglia | Riduzione dei tentativi abusivi senza rivelare lo stato del codice. | Decisione approvata | FLOW-011 |
| FR-055 | Quando all'apertura dell'app il permesso microfono non è ancora stato deciso, KinList deve mostrare un popup chiaro che spiega che la voce viene usata soltanto per generare tramite IA una lista e che la concessione del permesso microfono equivale al consenso per questo uso. | Membro | Consenso informato prima della prima registrazione, senza impedire l'uso delle altre funzioni. | Decisione approvata | FLOW-003 |

## 8. Regole di business

| ID | Condizione | Comportamento atteso | Eccezioni/nota |
|---|---|---|---|
| BR-001 | Un'identità accede per la prima volta con issuer e identificativo oggetto validi. | Viene creato un solo profilo applicativo stabile per la coppia. | Claim mancanti falliscono chiusi; nome ed email non sono fallback. |
| BR-002 | Un utente richiede dati o azioni familiari. | Appartenenza attiva, famiglia e visibilità vengono verificate prima di mostrare dati o applicare l'azione. | Essere autenticati non basta per accedere a una famiglia. |
| BR-003 | Due membri appartengono attivamente alla stessa famiglia. | Entrambi vedono e gestiscono gli stessi item Shared. | Gli item Personal restano visibili solo al rispettivo autore. |
| BR-004 | Un item viene creato o modificato. | Autore e momento derivano dall'identità e dal tempo riconosciuti da KinList, non da valori liberamente dichiarati. | L'autore originario resta stabile. |
| BR-005 | Una registrazione genera più item. | Tutti appartengono allo stesso gruppo, mantengono l'ordine riconosciuto e vengono aggiunti insieme. | Un gruppo non può essere aggiunto parzialmente. |
| BR-006 | Lo stesso identificativo di registrazione ricompare per duplicazione di trasporto o recupero dell'esito. | KinList restituisce il risultato già completato senza richiedere o conservare nuovamente l'audio. | Ogni nuova registrazione usa un nuovo identificativo; il recupero invia soltanto quello già assegnato. |
| BR-007 | La lista viene ordinata. | Il gruppo più recente appare prima e gli item restano nell'ordine originario del gruppo. | L'ultima modifica non cambia posizione. |
| BR-008 | Viene applicato un filtro categoria. | Il filtro server viene applicato prima della paginazione; un item compare se è attivo, visibile e associato alla categoria selezionata. | Il filtro riparte dalla prima pagina; nessun risultato è uno stato valido. |
| BR-009 | Un salvataggio non cambia nome né categorie. | Metadati e timeline non vengono modificati. | Nessun evento vuoto. |
| BR-010 | Un item attivo viene completato. | Diventa completato con momento e autore dell'azione e relativo evento. | Ripetere la stessa intenzione non duplica l'evento. |
| BR-011 | Annulla viene accettato. | L'item torna attivo nella posizione originaria e la riattivazione viene registrata. | Il ripristino non crea duplicati. |
| BR-012 | La finestra Annulla è scaduta. | L'item resta completato e non è recuperabile tramite KinList. | La durata approvata è cinque secondi. |
| BR-013 | Un item è ancora completato quando `CompletedAt` è minore o uguale al cutoff UTC di 30 periodi di 24 ore. | Diventa idoneo all'eliminazione definitiva nel controllo giornaliero. | Stato e cutoff sono ricontrollati; un item tornato attivo non viene eliminato. |
| BR-014 | Un'operazione coinvolge contenuti personali. | Audio, output AI, nomi, categorie, codici e altri contenuti sensibili non compaiono nelle informazioni operative ordinarie; audio e output grezzo non vengono persistiti. | La timeline mostra solo quanto approvato per il membro. |
| BR-015 | L'interpretazione vocale produce item e categorie. | Il risultato viene accettato soltanto se valido, coerente e non superiore a 1000 item. | Un risultato non valido o oltre limite non crea item parziali e non viene ritentato dopo una risposta ricevuta. |
| BR-016 | KinList propone o crea categorie. | Riusa una categoria equivalente della famiglia quando possibile; altrimenti ne crea una nuova nello stesso catalogo. | Nessuna categoria predefinita globale. |
| BR-017 | Il membro salva modifiche nel drawer. | Le modifiche diventano effettive solo dopo salvataggio esplicito riuscito. | Nessun salvataggio automatico. |
| BR-018 | Un membro agisce su dati cambiati nel frattempo. | KinList non sovrascrive il cambiamento e richiede di aggiornare e ripetere consapevolmente l'azione. | Nessuna fusione automatica implicita. |
| BR-019 | La PWA gestisce contenuti sul dispositivo. | Conserva soltanto quanto serve ad avviare la shell pubblica; richieste autenticate e dati personali richiedono la rete e non vengono conservati o accodati sul dispositivo. | Offline non viene mostrata una copia della lista. |
| BR-020 | Più item vengono completati rapidamente con azioni singole. | Ogni azione singola mantiene il proprio riferimento e recupero senza essere confusa con un bulk. | Il bulk usa invece un unico Annulla N. |
| BR-021 | L'interfaccia mostra testi visibili o accessibili. | Ogni testo esiste in italiano e inglese; italiano è predefinito e inglese è supportato e fallback. | La parità tra lingue è obbligatoria. |
| BR-022 | Un item completato viene eliminato dopo la conservazione. | Item, timeline e informazioni collegate previste vengono eliminate in modo coerente. | Le categorie ancora usate da altri item restano disponibili. |
| BR-023 | Un utente diventa membro. | Ottiene le stesse capacità di ogni altro membro. | Non esistono ruoli, amministratori o proprietari nello scope. |
| BR-024 | Un utente possiede già una famiglia attiva. | Non può crearne o attivarne una seconda. | Deve prima lasciare la famiglia corrente. |
| BR-025 | Un utente crea una famiglia. | Il nome è obbligatorio e il solo membro iniziale è il creatore. | Nessun altro membro viene aggiunto o invitato implicitamente. |
| BR-026 | Viene generato un codice d'invito e la famiglia ne ha meno di cinque attivi. | Il codice casuale contiene 12 caratteri Crockford Base32, è mostrato in gruppi di quattro, monouso, revocabile e scade sette giorni dopo la creazione. | La generazione non invia messaggi; il sesto invito attivo è rifiutato. |
| BR-027 | Un codice è stato mostrato alla creazione. | Il valore segreto non viene più mostrato; restano visibili solo creatore, creazione, scadenza e stato attivo. | Per condividerlo di nuovo serve averlo conservato fuori da KinList oppure generarne uno nuovo. |
| BR-028 | Un utente senza famiglia usa un codice disponibile. | Spazi, trattini e maiuscole vengono normalizzati; appartenenza e consumo avvengono insieme e una precedente appartenenza inattiva viene riattivata. | Un solo utente può consumare il codice; i tentativi sono limitati per identità e origine attendibile. |
| BR-029 | Un membro lascia la famiglia. | La sua appartenenza diventa inattiva e tutti i codici da lui creati vengono revocati. | L'uscita richiede conferma e riporta all'onboarding. |
| BR-030 | Esce l'ultimo membro attivo. | Famiglia e dati KinList collegati diventano inattivi. | Nessun membro conserva accesso. |
| BR-031 | Utente, appartenenza o famiglia sono inattivi. | Non concedono accesso ordinario e non sono eliminati definitivamente prima di 30 periodi di 24 ore; inattività e collegamenti attivi sono ricontrollati nella cancellazione. | Una riattivazione valida interrompe la soglia. |
| BR-032 | Il membro conferma Completa N. | Tutti gli item selezionati vengono completati insieme oppure nessuno cambia. | Un solo item non più valido o un errore annulla l'intero gruppo. |
| BR-033 | Il membro usa Seleziona tutti con un filtro attivo. | Sono selezionati soltanto gli item della pagina filtrata corrente, fino a 5000. | Item di altre pagine o nascosti dal filtro non sono inclusi. |
| BR-034 | Il membro usa Annulla N entro cinque secondi. | Tutti gli item del bulk tornano attivi insieme oppure nessuno viene ripristinato. | Non esiste recupero per singolo item del bulk. |
| BR-035 | Viene creato un nuovo item in questa versione. | La visibilità è Shared e l'autore originario viene mantenuto. | Personal è predisposto ma non selezionabile o convertibile dalla UI. |
| BR-036 | Un utente tenta di accedere a famiglia o item non consentiti. | KinHub nega l'accesso alla famiglia e KinList nega quello ai propri item senza mostrare nome, membri, item, categorie, timeline o altri dettagli. | L'accesso negato non è rappresentato come stato vuoto. |
| BR-037 | KinList mostra una collezione. | Applica perimetro, filtro e ordine stabile prima di mostrare una pagina non superiore al limite approvato, con navigazione precedente/successiva. | Non carica implicitamente l'intera collezione. |
| BR-038 | Un cursore viene usato con filtro, direzione o stato non più compatibili. | KinList rifiuta il cursore senza restituire dati e offre di ripartire dalla prima pagina. | Il contenuto del cursore non viene mostrato o interpretato dal client. |
| BR-039 | L'elaborazione vocale non riceve alcuna risposta per un guasto transitorio. | Può essere eseguito un solo tentativo automatico aggiuntivo entro il tempo residuo. | Una risposta ricevuta ma invalida non viene elaborata nuovamente. |
| BR-040 | Una richiesta vocale termina per qualsiasi esito. | Tracce del microfono, buffer audio e output grezzo vengono rilasciati. | Può restare il solo `RecordingId` necessario a recuperare un risultato già salvato. |
| BR-041 | All'apertura dell'app il permesso microfono non è ancora stato deciso. | KinList mostra un popup informativo prima della prima registrazione; la chiusura del popup non abilita il microfono, mentre la successiva concessione del permesso vale come consenso all'uso della voce per generare la lista tramite IA. | L'utente può continuare a usare l'app senza registrare finché non concede il permesso. |

## 9. Dati concettuali

| Informazione | Chi la fornisce | Chi può vederla | Regole di validità |
|---|---|---|---|
| Identità esterna | Sistema di identità | KinHub; utente per i dati del proprio profilo quando previsto | Coppia univoca issuer e identificativo oggetto; entrambi obbligatori; nessun fallback su nome o email. |
| Profilo utente | KinHub al primo accesso | Utente interessato e membri solo nella misura richiesta dalla UI | Stabile; può essere attivo o inattivo; nessuna UI di eliminazione account. |
| Famiglia | Creatore per il nome; KinHub per stato e date | Membri attivi della famiglia | Nome obbligatorio; stato attivo o inattivo; un utente ha al massimo una famiglia attiva. |
| Appartenenza | Creazione, codice o uscita | Utente interessato e membri nella lista membri | Attiva o inattiva; può essere riattivata da un nuovo codice valido; conserva la storia necessaria. |
| Membro visibile | Profilo e appartenenza | Membri attivi della stessa famiglia | Solo nome e iniziali; fallback `Membro`/`Member` e `?`; collezione paginata. |
| Invito | Membro che lo genera e KinHub | Membri attivi della famiglia per i metadati | 12 caratteri Crockford Base32, formato visivo a gruppi di quattro, monouso, revocabile, sette giorni; massimo cinque attivi; segreto visibile soltanto alla creazione. |
| Metadati invito | KinHub | Membri attivi della famiglia | Creatore, creazione, scadenza e stato attivo; mai il valore segreto. |
| Item | Interpretazione della voce, poi membro in modifica | Secondo visibilità Personal o Shared | Nome valido; stato attivo o completato; autore originario stabile; nuovi item sempre Shared. |
| Visibilità item | KinList alla creazione | Membri che possono vedere l'item | Personal solo autore; Shared famiglia; nessuna modifica dalla UI in questa versione. |
| Categoria | KinList durante la generazione o membro nel drawer | Membri attivi della famiglia, nei limiti degli item visibili | Nome non vuoto; catalogo per famiglia paginato; riuso delle equivalenti. |
| Registrazione e gruppo | Membro e KinList | Nessuna UI di consultazione dell'audio | Audio valido e non vuoto soltanto in memoria; massimo 1000 item; `RecordingId` può recuperare il risultato già salvato. |
| Evento timeline | KinList dopo un'azione riuscita | Chi può vedere l'item | Tipo approvato, autore e data/ora; nessun evento vuoto; collezione paginata. |
| Selezione multipla | Membro nella pagina corrente | Solo il membro durante l'interazione | Contiene item senza duplicati della pagina filtrata corrente, fino a 5000; esito unico e atomico. |
| Pagina e cursori | KinList | Utente della collezione autorizzato | Limite effettivo non oltre 5000; cursori opachi legati a ordine, filtro e direzione; nessun contenuto personale nel cursore. |
| Stato di inattività | KinList | Utente interessato nella misura necessaria; responsabile del servizio in forma aggregata | Impedisce l'accesso ordinario; pulizia definitiva non prima di 30 giorni continuativi. |
| Informazioni operative | KinList | Responsabile del servizio autorizzato | Solo aggregati e riferimenti non sensibili. |

## 10. Stati ed esperienza utente

- **Verifica dopo login**: nessun dato familiare appare finché KinList non ha determinato se aprire il servizio o l'onboarding.
- **Onboarding obbligatorio**: due azioni chiare, Crea una famiglia e Unisciti con un codice; si mostra soltanto il modulo scelto.
- **Stato iniziale lista**: microfono centrale senza item attivi visibili; lista e microfono in basso in caso contrario.
- **Caricamento**: indicatore locale e onesto; nessun dato di una famiglia precedente viene mostrato durante verifiche o cambi di stato.
- **Navigazione pagine**: la pagina corrente resta visibile durante il caricamento della successiva; fine collezione, prima pagina e cursore non più valido hanno feedback distinti.
- **Registrazione**: stati distinguibili per non supportato, pronto, attesa permesso, ascolto, arresto ed errore; area premibile stabile e rilevamento preventivo delle capacità.
- **Popup voce e consenso**: quando il permesso microfono non è ancora stato deciso, il popup iniziale è chiaro, chiudibile, non ambiguo e non blocca definitivamente l'uso delle altre funzioni.
- **Elaborazione**: feedback indeterminato; lista esistente leggibile; nuova registrazione impedita fino all'esito o al timeout; dopo errore si registra di nuovo.
- **Stato vuoto reale**: non coincide con assenza di famiglia, accesso negato, errore o filtro senza risultati.
- **Stato vuoto del filtro**: messaggio specifico e azione per rimuovere il filtro.
- **Modalità di selezione**: checkbox, conteggio e sole azioni essenziali; non è disponibile senza item visibili.
- **Successo bulk**: tutte le righe selezionate scompaiono insieme e un solo feedback presenta Annulla N.
- **Errore bulk**: dichiara che nessun item è stato completato o ripristinato e aggiorna la lista senza riapplicare alla cieca la selezione.
- **Impostazioni**: l'ingranaggio resta secondario rispetto al microfono, rispetta la safe area e non viene coperto da notifiche o contenuti.
- **Pagina Famiglia**: `/settings/family` è ricostruibile da URL e cronologia; durante il caricamento non mostra dati residui; membri paginati e fallback minimi; zero membri in una famiglia accessibile è trattato come stato incoerente.
- **Codice appena creato**: valore leggibile e disponibile soltanto in questa superficie; uscire dalla superficie ne impedisce la successiva visualizzazione.
- **Inviti attivi vuoti**: mostra Invita senza inventare destinatari, cronologie o canali di condivisione.
- **Codice rifiutato**: messaggio generico che non distingue inesistente, scaduto, revocato o consumato.
- **Uscita dalla famiglia**: conferma obbligatoria con conseguenze comprensibili; al successo ritorno all'onboarding.
- **Accesso negato**: messaggio essenziale e nessun contenuto della famiglia o dell'item.
- **Errore recuperabile**: input non sensibile preservato e azione coerente; l'audio non viene conservato. Un esito vocale perso dopo il commit viene recuperato con `RecordingId`.
- **Offline**: soltanto la shell pubblica; nessuna copia di lista, categorie o famiglia e nessuna promessa di operazioni accodate.
- **Errore non recuperabile**: spiegazione essenziale e ritorno a uno stato sicuro.
- **Conferme**: richieste per revoca invito e uscita dalla famiglia; non richieste per generazione, copia, creazione famiglia o generazione degli item.
- **Accessibilità**: tastiera e tecnologie assistive, focus visibile e prevedibile, nomi accessibili, aree tattili adeguate, stato non comunicato con il solo colore, date localizzate e movimento riducibile.
- **Temi e lingua**: contrasto mantenuto nei temi previsti; italiano predefinito, inglese supportato e fallback, testi equivalenti in entrambe le lingue.

## 11. Casi limite

- Due richieste contemporanee tentano di creare una famiglia per lo stesso utente: ne risulta una sola.
- Un utente con famiglia attiva tenta di creare o unirsi a una seconda famiglia: l'azione è negata e resta nella famiglia corrente.
- Un'appartenenza storica esiste ma è inattiva: non dà accesso diretto; un nuovo codice valido per la stessa famiglia può riattivarla.
- Due utenti usano contemporaneamente lo stesso codice monouso: uno solo entra nella famiglia.
- Un codice scade durante l'inserimento: l'unione viene rifiutata con il messaggio generico.
- Il membro chiude la superficie che mostra il codice appena creato: il segreto non è più recuperabile dalla pagina Famiglia.
- Un membro revoca un codice creato da un altro membro: la revoca è consentita dopo conferma.
- Un membro lascia la famiglia mentre esistono codici da lui creati: tutti diventano inutilizzabili.
- L'ultimo membro lascia la famiglia: famiglia e dati KinList collegati diventano inattivi e l'utente torna all'onboarding.
- Un ex membro tenta un accesso diretto dopo l'uscita: nessun dato viene mostrato.
- Un elemento inattivo viene riattivato prima di 30 giorni: non è più idoneo alla pulizia definitiva.
- Il membro rifiuta o ignora il permesso microfono: KinList non resta bloccata e spiega come riprovare.
- Il membro chiude il popup iniziale sulla voce senza concedere il permesso: l'app resta utilizzabile ma la registrazione non parte finché il permesso non viene concesso.
- L'audio è silenzioso o non contiene item: nessuna riga vuota; nuova registrazione disponibile.
- La risposta di una generazione si perde dopo il successo: riprovare restituisce lo stesso gruppo.
- Una categoria filtrata non contiene item: stato vuoto del filtro, non stato vuoto reale.
- Un item appartiene a più categorie: compare con qualunque singolo filtro corrispondente.
- Due membri modificano lo stesso item: il secondo non sovrascrive silenziosamente il primo.
- Seleziona tutti con filtro attivo non include gli item nascosti dal filtro.
- Il filtro cambia durante la selezione: gli item non più visibili non restano selezionati implicitamente.
- Un item selezionato viene completato da un altro membro prima di Completa N: nessun item del bulk viene completato.
- Il bulk comprende gli ultimi item visibili: compare lo stato vuoto ma Annulla N resta disponibile.
- La rete cade durante Annulla o Annulla N: KinList non dichiara un ripristino senza un esito effettivo.
- Un item Personal altrui viene richiesto direttamente o incluso in un bulk: nessun contenuto viene rivelato e l'operazione non procede.
- Una nuova registrazione tenta di indicare Personal: tutti i nuovi item restano Shared in questa versione.
- Un aggiornamento della PWA diventa disponibile durante registrazione, modifica o selezione: non interrompe l'operazione in corso.
- Un cursore viene riutilizzato con un filtro o una direzione diversi: KinList lo rifiuta e permette di ripartire dalla prima pagina senza mostrare dati estranei.
- Item vengono inseriti o eliminati tra due pagine: l'ordinamento stabile evita duplicazioni o salti causati da offset numerici.
- Una pagina contiene 5000 item e il membro usa Seleziona tutti: l'intera pagina viene completata oppure nessun item cambia.
- Un candidato alla pulizia viene riattivato prima della cancellazione: viene ricontrollato e non eliminato.
- L'elaborazione restituisce un risultato invalido: nessun tentativo automatico aggiuntivo e nessun item creato.
- L'elaborazione non riceve alcuna risposta per un guasto transitorio: è ammesso al massimo un tentativo automatico aggiuntivo nel tempo residuo.

## 12. Vincoli e requisiti non funzionali

- **NFR-001 — Semplicità**: l'interazione normale usa il minimo testo e il minimo numero di azioni compatibili con comprensione e accessibilità.
- **NFR-002 — Responsive**: priorità a smartphone, senza impedire l'uso da schermi più ampi; controlli fissi rispettano safe area e contenuti.
- **NFR-003 — Installabilità**: la PWA può essere installata quando il browser lo consente e resta pienamente utilizzabile nel browser.
- **NFR-004 — Privacy**: audio, trascrizione, codici e dati familiari sono minimizzati e non compaiono nelle informazioni operative ordinarie.
- **NFR-005 — Sicurezza**: identità, appartenenza, visibilità e permesso dell'azione vengono verificati prima di mostrare o cambiare dati; la sola interfaccia non determina l'accesso.
- **NFR-006 — Coerenza**: retry, concorrenza, unione, uscita e operazioni multiple non generano duplicati o stati parziali.
- **NFR-007 — Verificabilità**: esito, durata e categoria d'errore delle operazioni principali e delle pulizie sono conoscibili in forma aggregata.
- **NFR-008 — Localizzazione**: ogni testo visibile e accessibile ha versioni italiana e inglese equivalenti; italiano è predefinito e inglese è supportato e fallback.
- **NFR-009 — Accessibilità**: navigazione da tastiera, focus visibile e non coperto, nomi accessibili, contrasto, zoom e feedback non dipendente dal solo colore.
- **NFR-010 — Movimento**: le animazioni sono funzionali e riducibili; nessuna informazione dipende unicamente dall'animazione.
- **NFR-011 — Riservatezza degli accessi negati**: un rifiuto non rivela esistenza o contenuti di famiglie, inviti o item non consentiti.
- **NFR-012 — Riduzione dell'inquinamento visivo**: microfono, ingranaggio, selezione e feedback temporanei hanno gerarchia chiara e non si sovrappongono.
- **NFR-013 — Accesso limitato ai dati**: ogni collezione viene letta per pagine con limite iniziale e assoluto di 5000; non esistono letture integrali implicite.
- **NFR-014 — Memoria e persistenza vocale**: audio e output grezzo vivono solo in memoria per la richiesta e vengono rilasciati in ogni esito; non vengono creati file, code o copie browser.
- **NFR-015 — Compatibilità browser**: target primari Chrome desktop, Chrome Android, relative PWA installate ed Edge equivalente; Safari/iOS riceve verifica secondaria best effort senza promessa di parità.

## 13. Ipotesi

Le ipotesi precedenti su famiglia unica, filtro singolo, catalogo categorie, completamento immediato, connettività, conservazione audio, semantica dei 30 giorni, membri visibili e perimetro della selezione sono state risolte dalle decisioni approvate e non sono più aperte.

| ID | Ipotesi ancora valida | Conseguenza | Modalità di conferma |
|---|---|---|---|
| ASM-004 | I profili dispongono di un nome visualizzabile sufficiente a ricavare iniziali. | Avatar testuale senza immagini profilo. | Verifica dei dati disponibili. |
| ASM-007 | La pulizia definitiva può avvenire dopo la soglia minima senza garantire l'eliminazione nell'istante esatto del trentesimo giorno. | La regola vieta l'anticipo ma ammette un ritardo operativo. | Conferma prodotto/privacy. |

## 14. Decisioni approvate e decisioni aperte

### 14.1 Decisioni approvate

| ID | Decisione approvata | Esito |
|---|---|---|
| DEC-001 | Limite registrazione e modalità di risposta | 60 secondi, 12 MB stimati, richiesta sincrona massima 90 secondi. |
| DEC-002 | Lingue UI | Italiano predefinito; inglese supportato e fallback; parità obbligatoria. |
| DEC-003 | Interpretazione degli elementi pronunciati | Gli elementi riconosciuti vengono mantenuti e aggiunti direttamente; duplicati pronunciati, quantita e frasi ambigue vengono gestiti a livello di prompt dell'agente responsabile della restituzione della lista, senza introdurre una normalizzazione separata nel prodotto. |
| DEC-004 | Catalogo categorie | Catalogo per famiglia con creazione libera e riuso delle equivalenti. |
| DEC-005 | Salvataggio modifiche | Salvataggio esplicito, nessun salvataggio automatico. |
| DEC-006 | Finestra Annulla | Cinque secondi. |
| DEC-007 | Evento di Annulla | Evento `Riattivato`. |
| DEC-008 | Completamenti singoli ravvicinati | Restano azioni singole distinte; non vengono trasformati implicitamente in bulk. |
| DEC-009 | Aggiornamento collaborativo | Refresh manuale. |
| DEC-010 | Persistenza PWA | Offline resta disponibile soltanto la shell pubblica; richieste autenticate e dati personali richiedono la rete e non vengono conservati o accodati sul dispositivo. |
| DEC-011 | Priorità esperienza | Chrome desktop, Chrome Android, PWA installate ed Edge equivalente; Safari/iOS best effort secondario. |
| DEC-012 | Eliminazione item completati | Controllo giornaliero alle 00:00 UTC, cutoff inclusivo dopo 30 periodi di 24 ore e cancellazione limitata dei dati collegati. |
| DEC-013 | Capacità dei membri | Tutti i membri hanno le stesse capacità; nessun ruolo. |
| DEC-014 | Conflitti | Aggiornamento dello stato e ripetizione consapevole dell'azione. |
| DEC-015 | Famiglia attiva | Una sola famiglia attiva per utente; nessun selettore famiglia. |
| DEC-016 | Instradamento dopo login | Associato: accesso diretto; non associato: scelta obbligatoria tra creazione e codice. |
| DEC-017 | Creazione famiglia | Nome obbligatorio e solo creatore come membro iniziale. |
| DEC-018 | Accesso alle impostazioni | Ingranaggio fisso in basso a destra, dentro la safe area, verso le Impostazioni generali esistenti. |
| DEC-019 | Impostazioni e pagina Famiglia | Aggiunta della voce Famiglia senza rimuovere preferenze; route `/settings/family` con nome, membri paginati, inviti, Invita e Lascia famiglia, compatibile con URL e cronologia browser. |
| DEC-020 | Codici d'invito | Tutti i membri possono generarli e revocarli; 12 caratteri Crockford Base32, formato `XXXX-XXXX-XXXX`, monouso, sette giorni, massimo cinque attivi e condivisione manuale. |
| DEC-021 | Esposizione del codice | Segreto visibile solo alla creazione; successivamente solo metadati non segreti. |
| DEC-022 | Uscita dalla famiglia | Conferma, revoca dei codici creati da chi esce, appartenenza inattiva e ritorno all'onboarding. |
| DEC-023 | Ultimo membro | Famiglia e dati KinList collegati diventano inattivi. |
| DEC-024 | Ciclo di vita inattivo | Inattivazione reversibile; pulizia giornaliera dopo 30 periodi di 24 ore con ricontrollo di stato e collegamenti attivi. |
| DEC-025 | Selezione multipla | Seleziona tutti sulla pagina filtrata fino a 5000; unico esito atomico e Annulla N entro cinque secondi. |
| DEC-026 | Visibilità item | Personal solo autore e Shared famiglia; in questa versione tutti i nuovi item sono Shared e non esiste UI Personal. |
| DEC-027 | Accesso negato | Nessun dato non consentito viene mostrato; l'esito è distinto da lista o famiglia vuota. |
| DEC-028 | Accesso alle collezioni | Tutte le collezioni sono paginabili con cursori opachi avanti/indietro; limite lettura iniziale e assoluto 5000; nessun `Get All`. |
| DEC-029 | Elaborazione vocale | Audio solo in memoria, supporto ai formati Opus, MP3, AAC e WAV, massimo 1000 item e un solo tentativo automatico aggiuntivo, esclusivamente dopo un guasto transitorio senza risposta. |
| DEC-030 | Recupero risposta vocale | Se il commit riesce ma la risposta si perde, la PWA recupera il risultato con il solo `RecordingId`; non conserva né reinvia l'audio. |
| DEC-031 | Identità applicativa | La chiave esterna canonica è la coppia `(iss, oid)`; entrambi i claim sono obbligatori e non hanno fallback su dati di profilo. |
| DEC-032 | Protezione tentativi join | Barriera iniziale per istanza: 5 tentativi in 5 minuti per identità e 20 in 5 minuti per origine di rete attendibile, con indicazione temporale per riprovare. |
| DEC-033 | Coordinamento manutenzione | Un solo avvio giornaliero alle 00:00 UTC tenta retention e cleanup come casi distinti, con esiti e metriche separati. |
| DEC-034 | Lingua parlata | Rilevamento automatico e nomi/categorie nella lingua riconosciuta. |
| DEC-035 | Informativa e consenso per la voce | Se all'apertura dell'app il permesso microfono non è ancora stato deciso, KinList mostra un popup chiaro che spiega che la voce viene usata solo per generare tramite IA una lista; la concessione del permesso microfono vale come consenso a questo uso. |

### 14.2 Decisioni aperte

Non restano decisioni aperte nel perimetro funzionale approvato.

Limite audio, semantica demandata al prompt dell'agente lista, formati audio supportati, informativa e consenso per la voce, rilevamento automatico della lingua, recupero tramite `RecordingId` e paginazione sono decisioni approvate e non vengono riaperte dalle formulazioni residuali della ricerca.

## 15. Matrice di tracciabilità

| Area | Flussi | Requisiti | Regole | Decisioni/ipotesi | Fonti |
|---|---|---|---|---|---|
| Identità e onboarding | FLOW-001, FLOW-009 | FR-001–FR-003, FR-031–FR-033 | BR-001, BR-002, BR-024, BR-025 | DEC-015–DEC-017, DEC-031 | Scope confermato; research su onboarding e autorizzazione familiare |
| Famiglia e autorizzazione | FLOW-001, FLOW-010–FLOW-012 | FR-003–FR-005, FR-031, FR-036, FR-041–FR-043 | BR-002, BR-003, BR-023, BR-024, BR-029–BR-031, BR-036 | DEC-013, DEC-015, DEC-019, DEC-022–DEC-024, DEC-027 | Scope confermato; research su impostazioni, membri e autorizzazione familiare |
| Impostazioni | FLOW-010 | FR-034–FR-036, FR-049–FR-051 | BR-002, BR-036–BR-038 | DEC-018, DEC-019, DEC-028 | Scope confermato; research `floating-settings-entry`, `family-settings-members` |
| Inviti | FLOW-011 | FR-037–FR-040, FR-054 | BR-026–BR-028 | DEC-020, DEC-021, DEC-032 | Scope confermato; research `family-invite-code` |
| Lista, filtro e autore | FLOW-002, FLOW-005 | FR-004–FR-008, FR-015–FR-018, FR-049–FR-051 | BR-003, BR-004, BR-007, BR-008, BR-016, BR-037, BR-038 | DEC-004, DEC-028, ASM-004 | Idea e research `active-list-filtering`, `data-access-limits-pagination` |
| Registrazione e generazione | FLOW-003, FLOW-004 | FR-009–FR-015, FR-029, FR-048, FR-052, FR-053, FR-055 | BR-004–BR-006, BR-014–BR-016, BR-035, BR-039–BR-041 | DEC-001, DEC-003, DEC-026, DEC-029, DEC-030, DEC-034, DEC-035 | Idea e research `voice-recording`, `voice-to-list-ai`, `item-visibility-scope`; checkpoint umano |
| Drawer e timeline | FLOW-006 | FR-019–FR-023, FR-049–FR-051 | BR-004, BR-009, BR-017, BR-018, BR-037, BR-038 | DEC-005, DEC-007, DEC-014, DEC-028 | Idea e research `item-edit-history`, `data-access-limits-pagination` |
| Completamento singolo | FLOW-007 | FR-024, FR-025 | BR-010–BR-012, BR-020 | DEC-006–DEC-008 | Idea e research `complete-item-undo` |
| Completamento multiplo | FLOW-005, FLOW-013 | FR-023, FR-044–FR-046, FR-050 | BR-018, BR-032–BR-034 | DEC-025, DEC-028 | Scope confermato; research `bulk-item-completion`, `data-access-limits-pagination`; checkpoint umano |
| Visibilità Personal/Shared | FLOW-002, FLOW-004, FLOW-006, FLOW-007, FLOW-013 | FR-003–FR-005, FR-016, FR-047, FR-048 | BR-002, BR-003, BR-035, BR-036 | DEC-026, DEC-027 | Scope confermato; research `item-visibility-scope` |
| Conservazione e inattività | FLOW-008, FLOW-012, FLOW-014 | FR-026, FR-030, FR-042, FR-043, FR-049, FR-050 | BR-013, BR-014, BR-022, BR-030, BR-031, BR-037 | DEC-012, DEC-023, DEC-024, DEC-028, DEC-033, ASM-007 | Scope confermato; research `completed-item-retention`, `inactive-data-cleanup` |
| PWA, temi, lingua e qualità | Tutti | FR-027–FR-030, FR-034, FR-035, FR-053 | BR-014, BR-019, BR-021, BR-040 | DEC-002, DEC-010, DEC-011, DEC-018, DEC-029 | Regole KinHub; research `pwa-shell-connectivity`, `floating-settings-entry` |

Tutti i requisiti derivano dallo scope confermato, dalle decisioni approvate o da migliorie qualitative necessarie a rendere verificabili accessibilità, privacy, stati ed errori. Nessuna possibilità descritta nella ricerca è stata promossa automaticamente a funzionalità.

## 16. Criterio di approvazione

Il comportamento e i confini funzionali sono sufficientemente definiti per creare il backlog senza inventare nuove funzionalità: famiglia, identità, inviti, paginazione, voce, persistenza, manutenzione, visibilità e completamento multiplo hanno regole ed esiti espliciti. Le ipotesi residue e gli elementi out of scope non autorizzano ampliamenti del prodotto.
