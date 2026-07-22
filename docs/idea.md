# KinList — Riassunto funzionale

## 1. Visione generale

**KinList** è un componente dell’hub di servizi **Kin Hub**.

L’applicazione permette all’utente di creare e gestire liste attraverso la voce: l’utente registra un audio, l’intelligenza artificiale interpreta quanto detto e genera automaticamente gli elementi della lista.

L’esperienza deve essere estremamente semplice, immediata e priva di inquinamento visivo.

---

## 2. Tecnologia

KinList sarà sviluppata utilizzando:

- React
- Vite
- TypeScript

L’applicazione sarà una **Progressive Web App — PWA**:

- utilizzabile direttamente dal browser;
- installabile sul dispositivo;
- accessibile tramite un’icona come una normale applicazione;
- completamente responsive;
- progettata con approccio **mobile-first**.

---

## 3. Principi dell’interfaccia

L’interfaccia deve essere:

- minimale;
- intuitiva;
- ottimizzata per smartphone;
- priva di pulsanti superflui;
- priva di testi non necessari;
- composta solo da azioni immediatamente comprensibili;
- caratterizzata da animazioni semplici e funzionali.

---

## 4. Stato iniziale della lista

Quando l’utente apre KinList e non esistono item attivi, al centro dello schermo viene mostrato un grande pulsante con l’icona di un microfono.

Non devono essere presenti testi o altri elementi visivi superflui.

Il microfono rappresenta l’azione principale dell’applicazione.

---

## 5. Registrazione vocale

Il comportamento del pulsante del microfono è il seguente:

1. L’utente tocca una volta il pulsante.
2. La registrazione audio viene avviata.
3. L’icona cambia aspetto o mostra un’animazione per comunicare chiaramente che l’app sta ascoltando.
4. L’utente non deve mantenere premuto il pulsante.
5. L’utente tocca nuovamente il pulsante per terminare la registrazione.

Non viene mostrata un’anteprima intermedia dell’audio.

---

## 6. Elaborazione tramite intelligenza artificiale

Dopo la conclusione della registrazione:

1. l’audio viene elaborato dall’intelligenza artificiale;
2. il contenuto viene interpretato;
3. vengono individuati gli elementi distinti;
4. viene generata direttamente la lista.

Esempio:

```text
Audio: "Devo comprare latte, pasta e lamette"

Item generati:
- Latte
- Pasta
- Lamette
```

Il meccanismo tecnico utilizzato per l’elaborazione tramite intelligenza artificiale è ancora da definire.

---

## 7. Transizione alla lista

Quando gli item sono stati generati:

- il pulsante del microfono si sposta tramite un’animazione dal centro dello schermo alla parte inferiore centrale;
- la parte principale della pagina viene occupata dalla lista;
- il microfono rimane disponibile per effettuare nuove registrazioni.

Ogni nuova registrazione può aggiungere ulteriori item alla lista esistente.

---

## 8. Ordinamento degli item

La lista è preordinata mostrando gli elementi più recenti in cima.

L’ordinamento principale è:

```text
CreatedAt DESC
```

Regole:

- gli item appena creati vengono inseriti in cima alla lista;
- gli item generati dalla stessa registrazione restano raggruppati;
- all’interno dello stesso gruppo viene mantenuto l’ordine con cui sono stati riconosciuti nell’audio;
- la modifica di un item non lo riporta in cima;
- l’ordinamento non dipende dalla data dell’ultima modifica.

---

## 9. Struttura degli item

Ogni item contiene almeno:

- nome;
- una o più categorie;
- stato;
- data di creazione;
- data dell’ultima modifica;
- autore della creazione;
- autore dell’ultima modifica.

Nella lista principale vengono mostrate solo le informazioni necessarie per identificare e utilizzare rapidamente l’item.

Le informazioni aggiuntive vengono mostrate nel pannello laterale di dettaglio.

---

## 10. Categorie e tag

Durante la generazione automatica della lista, l’intelligenza artificiale assegna a ogni item una o più categorie.

Le categorie vengono mostrate sotto forma di tag.

Un item può quindi essere associato a più categorie contemporaneamente.

Esempio:

```text
Lamette

Categorie:
- Cura personale
- Bagno
- Spesa
```

---

## 11. Filtro per categoria

Nella parte superiore della lista è sempre presente un carosello orizzontale contenente i tag disponibili.

Il carosello viene utilizzato per filtrare gli item.

Quando l’utente seleziona un tag:

- la lista mostra solamente gli item associati a quella categoria;
- il tag selezionato deve risultare visivamente evidenziato;
- il filtro deve poter essere rimosso facilmente.

Il carosello deve essere utilizzabile comodamente tramite gesture orizzontale su smartphone.

---

## 12. Modifica di un item

Ogni item dispone di un’azione che permette di aprire il relativo dettaglio.

Non devono essere utilizzati popup tradizionali.

La modifica avviene attraverso un **drawer laterale** che entra da destra verso sinistra.

Il drawer rappresenta contemporaneamente:

- il pannello di modifica;
- il pannello di dettaglio;
- la cronologia dell’item.

---

## 13. Campi modificabili nel drawer

Nel pannello laterale l’utente può modificare:

### Nome

Il nome dell’item può essere sostituito con qualsiasi altra stringa di testo.

### Categorie

L’utente può:

- visualizzare le categorie già esistenti;
- selezionare più categorie;
- deselezionare le categorie;
- associare più tag allo stesso item;
- creare una nuova categoria.

Le categorie esistenti vengono mostrate come un carosello o un insieme di tag selezionabili.

La selezione è multipla:

- un tocco seleziona il tag;
- un nuovo tocco sul tag selezionato lo deseleziona.

Per creare una nuova categoria, l’utente può inserire il nome in un campo dedicato e confermare l’inserimento, ad esempio premendo Invio.

---

## 14. Informazioni di dettaglio

Nel drawer vengono mostrate anche le seguenti informazioni:

- autore della creazione;
- data di creazione;
- autore dell’ultima modifica;
- data dell’ultima modifica.

Queste informazioni sono in sola lettura.

---

## 15. Timeline delle modifiche

Le informazioni storiche vengono rappresentate attraverso una timeline verticale.

La timeline deve ricordare visivamente il tronco principale di un Git graph:

- una linea verticale;
- nodi circolari;
- nessuna diramazione;
- un nodo per ogni evento rilevante.

Ogni nodo mostra:

- tipo di evento;
- autore;
- data e ora.

Eventi inizialmente previsti:

- creazione dell’item;
- modifica dell’item;
- completamento dell’item.

Esempio:

```text
● Creato
  Martina
  15 luglio 2026, 18:42

│

● Modificato
  Martina
  15 luglio 2026, 18:45

│

● Completato
  Martina
  15 luglio 2026, 19:03
```

---

## 16. Completamento degli item

Ogni item può essere spuntato come completato.

Quando l’utente completa un item:

1. l’item scompare immediatamente dalla lista principale;
2. il suo stato passa da `Active` a `Completed`;
3. viene mostrata una snackbar con l’azione **Annulla**;
4. la snackbar rimane disponibile per 5 secondi.

Se l’utente preme **Annulla** entro 5 secondi:

- lo stato torna `Active`;
- l’item ricompare nella posizione precedente.

Se l’utente non esegue alcuna azione:

- l’item rimane `Completed`;
- non viene più mostrato nell’interfaccia principale.

---

## 17. Stati degli item

Per la prima versione sono previsti solamente due stati:

```text
Active
Completed
```

### Active

- visibile nella lista;
- modificabile;
- filtrabile tramite categorie.

### Completed

- non visibile nella lista;
- mantenuto temporaneamente nel database;
- non recuperabile tramite l’interfaccia dopo la scadenza dell’azione Annulla.

Non è prevista, per il momento, una schermata dedicata agli item completati.

---

## 18. Eliminazione automatica

Gli item in stato `Completed` vengono eliminati definitivamente dal database dopo 30 giorni.

Il periodo deve essere calcolato utilizzando il campo:

```text
CompletedAt
```

Non deve essere utilizzato `UpdatedAt`, perché eventuali aggiornamenti tecnici potrebbero posticipare involontariamente l’eliminazione.

L’eliminazione viene eseguita tramite un processo pianificato.

Flusso:

```text
Active
  ↓
Completed
  ↓
Conservazione per 30 giorni
  ↓
Eliminazione definitiva dal database
```

---

## 19. Dati minimi dell’item

Il modello dati dell’item deve contenere almeno:

```text
Id
Name
Status
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
CompletedAt
Categories
RecordingId
```

`RecordingId` permette di identificare e raggruppare gli item creati dalla stessa registrazione.

---

## 20. Flusso principale

```text
Apertura di KinList
        ↓
Lista vuota?
   ┌────┴────┐
   │         │
  Sì        No
   │         │
Microfono   Visualizzazione lista
al centro        ↓
   │        Microfono in basso
   └────┬────────┘
        ↓
Tocco sul microfono
        ↓
Avvio registrazione
        ↓
Animazione di ascolto
        ↓
Secondo tocco
        ↓
Fine registrazione
        ↓
Elaborazione tramite AI
        ↓
Estrazione degli item
        ↓
Assegnazione automatica delle categorie
        ↓
Inserimento dei nuovi item in cima
        ↓
Visualizzazione della lista aggiornata
```

---

## 21. Obiettivo dell’esperienza utente

L’utente deve poter:

1. aprire KinList;
2. toccare il microfono;
3. pronunciare ciò che deve ricordare o acquistare;
4. interrompere la registrazione;
5. vedere immediatamente gli item organizzati;
6. completarli con un semplice tocco;
7. modificarli solo quando necessario.

L’interazione principale deve quindi essere:

```text
Parla → Ottieni la lista → Spunta
```
