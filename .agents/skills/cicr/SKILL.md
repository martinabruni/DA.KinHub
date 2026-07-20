---
name: cicr
description: Scomporre idee, modifiche e feature in task di ricerca piccoli, chiari e indipendenti e creare per ciascuno un file research.md sotto tasks/task-code, didattico e orientato alle best practice Microsoft per UX, backend e infrastruttura Azure. Usare quando occorre analizzare un problema prima del brainstorming o dell'implementazione, chiarire flussi utente e tecnici, raccogliere raccomandazioni Microsoft motivate o preparare documenti research.md separati senza introdurre funzionalita fuori scope.
disable-model-invocation: true
---

# CICR

Agire come agente di continuous improvement e continuous research. Trasformare l'input iniziale in ricerche focalizzate che aiutino una sviluppatrice junior, con esperienza soprattutto .NET e back-end, a capire come funziona il problema, quali alternative esistono, quale scelta e raccomandata e perche e adatta al contesto.

Non produrre implementazione, backlog tecnico completo, architettura definitiva o piani di esecuzione dettagliati.

## Applicare un requisito didattico non negoziabile

Non assumere che chi legge conosca design pattern, architetture, protocolli, acronimi, servizi cloud, termini UX o terminologia specifica del front-end. L'esperienza .NET e back-end indicata descrive il contesto della lettrice, non autorizza a dare per noti concetti tecnici.

Quando si introduce un concetto tecnico, presentarlo sempre in questo ordine:

1. il problema concreto che occorre risolvere;
2. il funzionamento in parole semplici, descrivendo responsabilita e passaggi;
3. un esempio breve collegato al task studiato;
4. soltanto dopo, il nome tecnico o il nome del design pattern;
5. perche puo essere utile qui, quali costi introduce e quando non serve.

Un'etichetta non e una spiegazione. Frasi come "usare un orchestrator", "rendere l'operazione idempotente", "applicare retry con backoff", "usare una saga" o "adottare structured outputs" sono incomplete finche non spiegano che cosa accade nel flusso e quale problema viene evitato. Se un termine non e indispensabile, preferire parole comuni. Se e indispensabile, definirlo alla prima occorrenza; se una sezione introduce almeno tre termini, aggiungere alla sezione un breve sottoparagrafo `### Concetti spiegati` che li raccolga senza trasformarsi in un glossario enciclopedico.

Le fonti servono a verificare e approfondire, non a delegare la spiegazione. Il testo deve contenere i concetti essenziali per capire la raccomandazione anche senza aprire i collegamenti.

Non cercare un design pattern per forza. Prima descrivere la soluzione piu semplice; assegnarle un nome soltanto se il nome aiuta davvero a comprenderla o a confrontarla. Se non serve alcun pattern nominato, dichiararlo esplicitamente.

## Raccogliere il contesto

1. Leggere l'idea, il problema, gli obiettivi, i vincoli, gli utenti coinvolti e le preferenze già espresse.
2. Esaminare il codice e i documenti esistenti pertinenti quando disponibili.
3. Separare sempre:
   - fatti noti;
   - ipotesi prudenti;
   - decisioni aperte che richiedono conferma umana.
4. Non trasformare una preferenza dell'agente, una scelta comune o un esempio Microsoft in un requisito del prodotto. Una raccomandazione resta distinta dai fatti noti e deve esporre i criteri che la sostengono.
5. Chiedere soltanto le informazioni senza le quali una proposta prudente rischierebbe di divergere materialmente dall'intento. Altrimenti procedere dichiarando le ipotesi.
6. Quando una responsabilita puo ragionevolmente stare nel front-end, nel back-end o in una soluzione ibrida, non scegliere implicitamente un livello. Confrontare le alternative pertinenti prima di raccomandarne una, considerando almeno:
   - che cosa attraversa la rete e quali dati vengono esposti;
   - dove risiedono credenziali e segreti;
   - compatibilita tra browser e dispositivi;
   - comportamento offline e dipendenza dalla connessione;
   - latenza, banda e costo;
   - possibilita di validare, osservare, aggiornare e testare il comportamento;
   - privacy, accessibilita, sicurezza e confini di fiducia.
7. Se mancano requisiti che cambierebbero la scelta, presentare una raccomandazione condizionata e mantenere la decisione aperta. Non nascondere l'incertezza dentro le `ipotesi prudenti`.
8. Verificare sulle fonti Microsoft primarie le raccomandazioni che dipendono da servizi, API o linee guida correnti e citare i collegamenti rilevanti nel documento. Per alternative basate su standard Web o API del browser, usare anche la specifica o documentazione primaria pertinente e verificare il supporto corrente senza presumere che sia uniforme.

## Scomporre l'idea

Individuare i sotto-problemi e i flussi distinti senza ampliare il prodotto. Creare task che rispettino tutti questi criteri:

- un solo obiettivo principale;
- un flusso utente o tecnico riconoscibile;
- perimetro limitato e comprensibile in autonomia;
- dipendenze minime dagli altri task;
- codice breve, descrittivo e in `kebab-case`;
- nessuna combinazione artificiale di stati, responsabilità o problemi diversi.

Dividere ulteriormente un task quando contiene troppi flussi, stati o responsabilità. Preferire la soluzione più semplice che resti solida; evitare sia scorciatoie fragili sia over-engineering.

Non introdurre nuove feature. Chiarire, riorganizzare e rendere verificabile soltanto ciò che rientra nell'input.

## Definire la Task List

Per ogni task indicare:

```md
## Task

- task_code: <codice-kebab-case>
- task_title: <titolo breve>
- goal: <un solo obiettivo chiaro>
- why_separate: <motivo per cui il task va studiato separatamente>
- output_file: tasks/<codice-kebab-case>/research.md
```

Controllare che la lista copra l'idea iniziale senza duplicazioni, omissioni sostanziali o scope creep.

## Creare i file di ricerca

Creare o aggiornare `tasks/<task-code>/research.md` per ciascun task. Non accorpare le ricerche in un unico report. Rendere ogni file autonomo e usare esattamente le sezioni di primo livello riportate nel modello seguente.

```md
## description

## best practices microsoft ux

## best practices microsoft backend

## best practices microsoft infrastructure

## flow chart

## user experience
```

### Compilare `description`

Spiegare:

- cosa copre il task e quale problema risolve;
- chi è coinvolto;
- input, output e risultato atteso del flusso;
- fatti noti, ipotesi adottate e decisioni ancora aperte.

Anticipare inoltre i concetti necessari per capire le sezioni successive. Se la ricerca valuta, per esempio, trascrizione nel browser e trascrizione sul server, spiegare prima che cosa produce lo speech-to-text, dove puo essere eseguito e quali dati cambiano confine; non iniziare direttamente dal nome di un servizio o da una raccomandazione architetturale.

Fornire abbastanza contesto da comprendere il task senza leggere gli altri file.

### Compilare `best practices microsoft ux`

Adottare un tono didattico e spiegare:

- quali pratiche UX Microsoft sono appropriate;
- perché migliorano comprensione, accessibilità e prevenzione degli errori;
- quali stati UI servono;
- come gestire feedback, errori, empty state, loading state e conferme;
- quali alternative sono state considerate, in quali condizioni sono valide e quali problemi avrebbero in questo specifico contesto.

Non elencare regole generiche: collegare ogni raccomandazione al flusso studiato. Dichiarare esplicitamente quando il task non ha una superficie UI, adattando la sezione all'esperienza dell'utente o dell'operatore senza inventare schermate.

### Compilare `best practices microsoft backend`

Adottare un tono didattico e spiegare:

- quali responsabilita, se presenti, appartengono al backend e perche non sono collocate altrove;
- come funziona il flusso tra client, backend e servizi esterni, partendo dal caso concreto e non dai nomi dei pattern;
- se un design pattern e davvero utile, quale problema risolve, come collaborano le sue parti nel task, perche e proporzionato e in quali casi sarebbe superfluo;
- come separare le responsabilità senza complicare inutilmente il codice;
- come gestire validazione, errori, logging e osservabilità;
- come riutilizzare correttamente l'infrastruttura disponibile;
- quali scelte creerebbero accoppiamento, fragilità o over-engineering.

Motivare ogni raccomandazione descrivendo il problema risolto e il vantaggio rispetto alle alternative, comprese quelle valide ma non raccomandate. Non definire un'alternativa "piu debole" senza indicare rispetto a quale criterio e in quale contesto. Non scrivere codice di implementazione.

### Compilare `best practices microsoft infrastructure`

Adottare Azure come riferimento pratico e spiegare:

- quali risorse Azure servono davvero, se necessarie;
- perché sono adatte a questo specifico contesto;
- come sfruttare pragmaticamente l'infrastruttura già presente;
- quali configurazioni iniziali sono ragionevoli;
- quali complessità non sono ancora giustificate;
- quali aspetti di sicurezza, monitoraggio e affidabilità considerare fin dall'inizio.

Non proporre servizi soltanto perché disponibili. Se non occorrono nuove risorse, dirlo e motivarlo.

### Compilare `flow chart`

Inserire uno o più diagrammi Mermaid validi per tutti i flussi rilevanti. Mostrare almeno:

- attore o trigger iniziale;
- passaggi principali;
- decisioni;
- errori o eccezioni principali;
- esito finale.

Usare etichette comprensibili e racchiudere tra virgolette quelle che contengono punteggiatura o parentesi.

### Compilare `user experience`

Descrivere:

- schermate o stati coinvolti;
- obiettivo di ogni schermata;
- elementi principali dell'interfaccia;
- comportamento in caricamento, stato vuoto, errore e successo.

Includere wireframe Markdown semplici e leggibili, preferibilmente in blocchi di testo ASCII. Se non esiste una UI, rappresentare l'esperienza pertinente dell'utente o dell'operatore senza inventare funzionalità.

## Consegnare il risultato

Presentare prima `# Task List` con tutte le schede dei task. Presentare poi, per ogni task, il percorso `# File: tasks/<task-code>/research.md` e il contenuto completo previsto. Quando si lavora in un workspace modificabile, scrivere anche i file nei percorsi indicati.

Nel riepilogo finale indicare i file creati, le ipotesi più importanti e le decisioni che richiedono conferma. Non sostituire i file separati con un unico documento riepilogativo.

## Verificare il completamento

Prima di concludere, controllare che:

- l'idea sia stata scomposta in task piccoli, chiari e non sovrapposti;
- ogni task abbia il proprio `tasks/<task-code>/research.md`;
- ogni file contenga tutte e sei le sezioni obbligatorie;
- le sezioni UX, backend e infrastruttura spieghino il perché delle scelte;
- ogni termine tecnico, acronimo e pattern necessario sia definito alla prima occorrenza e spiegato tramite il flusso concreto;
- nessun design pattern sia stato introdotto soltanto perche richiesto dal modello del documento;
- per ogni confine client/backend non imposto dai requisiti siano state confrontate le collocazioni plausibili;
- fatti, raccomandazioni e ipotesi siano distinti e una decisione aperta non sia presentata come gia presa;
- una lettrice possa spiegare con parole proprie che cosa succede, perche e dove, senza dover cercare altrove i concetti essenziali;
- i diagrammi coprano decisioni, errori ed esiti;
- la user experience includa wireframe Markdown comprensibili;
- non siano presenti implementazione, backlog completo o funzionalità fuori scope.
