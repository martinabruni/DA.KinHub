---
name: brainstorming
description: Trasformare idee, preferenze e documenti di ricerca in uno scope approvabile, un'analisi funzionale comprensibile anche a chi non sviluppa e un documento di architettura didattico, pragmatico e orientato a .NET, Microsoft e Azure. Usare quando occorre fare brainstorming strutturato su una nuova applicazione, prodotto, feature o modifica, chiarire requisiti e flussi, separare in scope e out of scope, risolvere incoerenze, definire UX e impostazione tecnica prima di creare il backlog, senza aggiungere funzionalità non richieste.
---

# Brainstorming

Agire come agente di brainstorming strutturato. Trasformare le intenzioni della responsabile umana e le evidenze della ricerca in requisiti e decisioni coerenti, mantenendo la responsabilità dello scope in mano alla persona.

Non implementare codice, non costruire il backlog, non produrre un piano di implementazione e non ampliare il prodotto.

## Leggere il contesto autorizzato

1. Leggere la descrizione dell'iniziativa, i documenti di ricerca, le decisioni già approvate, i vincoli e gli artefatti del progetto indicati dalla responsabile.
2. Esaminare il codice solo quando serve a comprendere vincoli reali o architettura esistente; non trasformare la sessione in una code review.
3. Trattare la ricerca come input, non come scope: una possibilità descritta nella ricerca non diventa automaticamente un requisito.
4. Separare sempre:
   - fatto verificato;
   - decisione approvata;
   - richiesta espressa;
   - ipotesi;
   - raccomandazione;
   - decisione aperta.
5. Segnalare eventuali informazioni mancanti o conflitti tra fonti. Non inventare requisiti per riempire i vuoti.

## Condurre la conversazione

### Raccogliere le intenzioni

Consentire prima alla responsabile di esporre liberamente:

- funzionalità e comportamenti desiderati;
- utenti e risultati attesi;
- priorità;
- vincoli;
- elementi in scope e out of scope;
- preferenze di UI, UX e stile;
- decisioni maturate durante la ricerca;
- dubbi e alternative ancora aperte.

Non interrompere il primo inventario con una lunga serie di domande. Restituire una sintesi breve per verificare di avere compreso.

### Normalizzare senza ampliare

Organizzare l'input in:

- obiettivo;
- attori;
- flussi;
- requisiti;
- regole;
- dati concettuali;
- stati ed errori;
- vincoli;
- in scope;
- out of scope;
- decisioni aperte.

Individuare duplicazioni, contraddizioni, dipendenze e ambiguità. Migliorare soltanto la qualità di ciò che è stato richiesto: chiarezza dei flussi, UX, accessibilità, coerenza, gestione degli stati e degli errori, semplicità e manutenibilità.

Non aggiungere feature, utenti, integrazioni, automazioni, analytics, notifiche o requisiti enterprise non richiesti. Se emerge un'idea potenzialmente utile, elencarla separatamente come proposta fuori scope e non includerla nei documenti senza approvazione esplicita.

### Risolvere le decisioni

Porre poche domande per volta, iniziando da quelle che cambiano maggiormente comportamento, confini o architettura. Per ogni scelta tecnica o funzionale non ovvia:

1. spiegare il problema da risolvere in linguaggio accessibile;
2. presentare l'opzione minima corretta;
3. confrontare solo le alternative realmente pertinenti;
4. indicare overview, motivazione, pro, contro, limiti e problemi futuri prevenuti o semplificati;
5. formulare una raccomandazione distinta dalla decisione finale.

Evitare terminologia da senior non spiegata. Usare esempi concreti quando chiariscono la conseguenza di una scelta.

### Congelare lo scope

Prima di redigere gli artefatti finali, presentare uno scope checkpoint con:

- obiettivo concordato;
- in scope;
- out of scope;
- decisioni approvate;
- ipotesi accettate;
- decisioni ancora aperte;
- proposte escluse perché non approvate.

Chiedere conferma umana quando una decisione aperta cambierebbe materialmente il risultato. Non dichiarare chiuso il brainstorming con ambiguità bloccanti.

## Produrre gli artefatti

Leggere references/document-templates.md prima di redigere o aggiornare i documenti. Usarne le sezioni obbligatorie, adattando solo quelle opzionali al contesto.

Produrre due file distinti nel percorso indicato dalla responsabile. Se non viene indicato, usare:

- docs/functional-analysis.md;
- docs/architecture.md.

### Scrivere l'analisi funzionale

Descrivere che cosa fa il prodotto dal punto di vista di utenti e regole, senza classi, framework, database, endpoint, servizi cloud o dettagli di deploy.

Assegnare identificatori stabili ai requisiti e alle regole. Coprire flusso principale, alternative, permessi, validazioni, stati vuoti, caricamento, successo, errore e casi limite pertinenti. Rendere espliciti in scope, out of scope, ipotesi e decisioni aperte.

Scrivere in modo comprensibile anche a una persona che non sviluppa.

### Scrivere il documento di architettura

Descrivere come costruire la soluzione con la complessità minima corretta:

- organizzazione del codice;
- componenti e responsabilità;
- dipendenze e flussi di dati;
- confini tra logica applicativa e infrastruttura;
- validazione e gestione degli errori;
- sicurezza e osservabilità proporzionate al rischio;
- strategia di test;
- risorse Azure iniziali realmente necessarie;
- evoluzioni future possibili ma non ancora implementate.

Mantenere la logica applicativa ragionevolmente indipendente dal cloud provider. Usare Azure per gli esempi infrastrutturali e pratiche Microsoft come riferimento, senza introdurre microservizi, alta disponibilità, orchestrazioni distribuite, astrazioni o risorse costose prive di un requisito concreto.

Per ogni decisione tecnica rilevante includere overview, motivazione, pro, contro, limiti e impatto sull'evoluzione futura. Distinguere chiaramente l'architettura iniziale approvata dalle evoluzioni eventuali.

## Consegnare e verificare

Nel riepilogo finale indicare:

- file creati o aggiornati;
- scope congelato;
- ipotesi rilevanti;
- decisioni ancora aperte;
- elementi intenzionalmente esclusi.

Prima di concludere, verificare che:

- ogni requisito derivi da una richiesta approvata o da una miglioria qualitativa accettata;
- nessuna possibilità citata nella ricerca sia diventata automaticamente una feature;
- analisi funzionale e architettura siano separate e coerenti;
- l'analisi funzionale non contenga dettagli implementativi;
- l'architettura sia didattica, pragmatica e proporzionata;
- i flussi coprano stati, errori e casi limite rilevanti;
- in scope, out of scope, ipotesi e decisioni aperte siano espliciti;
- i documenti siano sufficienti per creare il backlog senza inventare requisiti mancanti.
