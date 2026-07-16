# Modelli degli artefatti

Leggere questo riferimento prima di creare o aggiornare gli output del brainstorming. Mantenere i due documenti separati. Omettere una sezione opzionale solo quando non è pertinente e dichiararne brevemente il motivo.

## Analisi funzionale

Usare questa struttura in docs/functional-analysis.md, salvo diverso percorso concordato.

# Analisi funzionale — <nome iniziativa>

## 1. Scopo del documento

Spiegare finalità, contesto e risultato atteso senza anticipare la soluzione tecnica.

## 2. Glossario

Definire solo termini di dominio o concetti necessari alla comprensione.

## 3. Attori

Per ogni attore indicare obiettivo, responsabilità e permessi rilevanti.

## 4. In scope

Elencare esclusivamente funzionalità e comportamenti approvati.

## 5. Out of scope

Elencare esclusioni esplicite e proposte non approvate che rischiano di essere reintrodotte per errore.

## 6. Flussi utente

Per ogni flusso usare un identificatore FLOW-### e descrivere:

- trigger;
- precondizioni;
- percorso principale;
- alternative;
- errori e recupero;
- risultato osservabile.

Aggiungere diagrammi Mermaid solo quando migliorano materialmente la comprensione.

## 7. Requisiti funzionali

Per ogni requisito usare un identificatore FR-### e indicare:

- descrizione;
- attore interessato;
- valore o risultato;
- origine: richiesta, decisione approvata o miglioria qualitativa approvata;
- flussi correlati.

Formulare requisiti verificabili senza descrivere l'implementazione.

## 8. Regole di business

Per ogni regola usare un identificatore BR-###. Specificare condizioni, comportamento atteso ed eventuali eccezioni.

## 9. Dati concettuali

Descrivere le informazioni necessarie, chi le fornisce, chi può vederle e le regole di validità. Non progettare tabelle o schemi fisici.

## 10. Stati ed esperienza utente

Coprire, quando pertinenti:

- stato iniziale;
- caricamento;
- stato vuoto;
- successo;
- errore recuperabile;
- errore non recuperabile;
- accesso negato;
- conferme per azioni rilevanti;
- accessibilità e feedback.

## 11. Casi limite

Elencare scenari ai confini dello scope e comportamento atteso, senza inventare nuove capacità.

## 12. Vincoli e requisiti non funzionali

Includere solo vincoli approvati o necessari per qualità, sicurezza, privacy, accessibilità e usabilità. Evitare obiettivi numerici inventati.

## 13. Ipotesi

Usare identificatori ASM-###. Spiegare conseguenza e modalità di conferma.

## 14. Decisioni aperte

Usare identificatori OPEN-###. Indicare impatto, opzioni pertinenti e responsabile della decisione.

## 15. Matrice di tracciabilità

Collegare requisiti, flussi, regole, decisioni approvate e fonte dell'input. Rendere visibili requisiti privi di origine approvata.

## 16. Criterio di approvazione

Confermare che comportamento e confini possano essere trasformati in backlog senza inventare requisiti.

## Documento di architettura

Usare questa struttura in docs/architecture.md, salvo diverso percorso concordato.

# Architettura — <nome iniziativa>

## 1. Scopo e contesto

Riassumere problema, utenti, scope e vincoli che guidano l'architettura.

## 2. Principi

Esplicitare semplicità corretta, modularità, testabilità, separazione delle responsabilità, indipendenza ragionevole dal cloud e controllo dei costi.

## 3. Architettura iniziale

Fornire una vista comprensibile della soluzione minima approvata. Aggiungere un diagramma Mermaid quando chiarisce componenti, confini o dipendenze.

## 4. Componenti e responsabilità

Per ogni componente indicare:

- responsabilità;
- input e output;
- dipendenze autorizzate;
- elementi che non gli competono;
- requisiti funzionali serviti.

Non creare componenti senza una responsabilità richiesta.

## 5. Organizzazione del codice

Descrivere moduli e confini. Motivare interfacce e astrazioni; evitare livelli introdotti soltanto per una possibile esigenza futura.

## 6. Flussi di dati e integrazioni

Descrivere sequenza, contratti logici, validazione, persistenza e integrazioni già approvate. Evidenziare errori, timeout o indisponibilità pertinenti.

## 7. Gestione degli errori

Distinguere errori di input, dominio, autorizzazione, dipendenze e problemi inattesi. Spiegare comportamento osservabile, logging e recupero.

## 8. Sicurezza e privacy

Descrivere autenticazione, autorizzazione, protezione dei dati, segreti e rischi pertinenti. Non introdurre controlli enterprise privi di un rischio concreto.

## 9. Osservabilità

Indicare log, metriche e tracce minime utili a comprendere funzionamento ed errori. Evitare piattaforme aggiuntive se gli strumenti esistenti sono sufficienti.

## 10. Strategia di test

Collegare test unitari, integrazione, contratto, end-to-end o manuali ai rischi e ai requisiti. Non richiedere ogni livello di test per principio.

## 11. Infrastruttura Azure iniziale

Per ogni risorsa realmente necessaria indicare:

- problema risolto;
- configurazione iniziale proporzionata;
- costo o complessità rilevante;
- responsabilità che resta fuori dalla logica applicativa;
- alternativa più semplice considerata.

Dichiarare esplicitamente quando non servono nuove risorse.

## 12. Decisioni architetturali

Per ogni decisione usare un identificatore ADR-### e includere:

- contesto;
- overview;
- scelta;
- motivazione;
- pro;
- contro;
- limiti;
- alternative scartate e motivo;
- problemi futuri prevenuti o semplificati;
- requisiti collegati.

## 13. Evoluzioni future non implementate

Descrivere soltanto evoluzioni rese plausibili dai requisiti o dalla crescita. Indicare il segnale concreto che ne giustificherebbe l'adozione. Non trattarle come scope corrente.

## 14. Rischi e mitigazioni

Elencare rischi effettivi, probabilità o impatto qualitativo e mitigazione proporzionata.

## 15. Ipotesi e decisioni aperte

Riutilizzare gli identificatori dell'analisi funzionale quando condivisi. Evidenziare ciò che blocca il backlog.

## 16. Tracciabilità

Collegare componenti, decisioni e test ai requisiti funzionali. Ogni componente deve servire almeno un requisito o un vincolo approvato.

## 17. Criterio di approvazione

Confermare che impostazione tecnica, confini e decisioni siano sufficienti per costruire il backlog senza aggiungere funzionalità o complessità.
