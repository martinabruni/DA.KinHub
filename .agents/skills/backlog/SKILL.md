---
name: backlog
description: Scomporre un'analisi funzionale e un documento di architettura approvati in un backlog ordinato di feature autonome e sviluppabili, con tracciabilità dei requisiti, dipendenze esplicite nella feature dipendente, prerequisiti, criteri di accettazione, ordine di esecuzione e parallelismi sicuri. Usare quando occorre passare dagli artefatti di brainstorming alla pianificazione esecutiva senza implementare codice, ampliare lo scope o inventare decisioni mancanti.
---

# Backlog

Agire come pianificatore esecutivo. Trasformare requisiti e architettura approvati in feature che una sviluppatrice o un agente possa comprendere, sviluppare e verificare in autonomia dopo il completamento degli eventuali prerequisiti dichiarati.

Non implementare codice, non svolgere nuova ricerca di prodotto e non modificare lo scope approvato.

## Raccogliere le fonti autorevoli

1. Leggere integralmente l'analisi funzionale, il documento di architettura e le istruzioni di repository applicabili.
2. Leggere decisioni, ipotesi, rischi, tracciabilità, in scope e out of scope; non limitarsi alle tabelle dei requisiti.
3. Esaminare il repository quando serve a distinguere ciò che esiste già dal lavoro nuovo e a indicare touchpoint realistici.
4. Usare i documenti di ricerca soltanto per chiarire un riferimento già approvato. Non promuovere una raccomandazione di ricerca a requisito.
5. Registrare nell'indice del backlog ogni fonte con percorso e ruolo.

Separare sempre:

- requisiti e decisioni approvati;
- vincoli trasversali;
- ipotesi da confermare;
- decisioni aperte;
- dettagli tecnici da verificare;
- elementi esplicitamente fuori scope.

Se analisi funzionale e architettura si contraddicono, non scegliere silenziosamente. Registrare il conflitto come gate e bloccare soltanto le feature interessate.

## Stabilire la readiness

Classificare ogni informazione mancante:

- **Gate bloccante**: cambia comportamento, sicurezza, dati, contratto pubblico o architettura. Non inventare la risposta; marcare le feature interessate come `blocked`.
- **Verifica tecnica locale**: può essere risolta dentro una feature senza cambiare lo scope, per esempio confermare un path, un indice o un limite configurabile. Inserirla come attività e criterio di accettazione della prima feature interessata.
- **Dettaglio implementativo delegabile**: lasciare libertà alla feature entro i vincoli approvati; non trasformarlo in una falsa decisione di prodotto.

Non creare una feature fittizia per una decisione esclusivamente umana. Elencare i gate nell'indice e nelle feature bloccate.

## Costruire la mappa di copertura

1. Inventariare flussi, requisiti funzionali, regole di business, requisiti non funzionali, decisioni e ADR.
2. Raggrupparli prima per risultato utente o capacità operativa osservabile, poi per confine di dominio.
3. Assegnare ogni requisito a una sola feature primaria. Consentire riferimenti secondari quando altre feature applicano o verificano lo stesso vincolo.
4. Applicare sicurezza, privacy, accessibilità, localizzazione, osservabilità e documentazione nelle feature che toccano quelle superfici. Non rimandarli a una feature finale generica.
5. Scartare ogni candidata che non deriva da almeno un requisito, vincolo o decisione approvati.

## Scomporre in feature autonome

Preferire vertical slice complete: includere nella stessa feature dominio, orchestrazione, persistenza, API, UI, documentazione e test necessari al suo unico risultato. Non dividere automaticamente il lavoro in feature `frontend`, `backend`, `database` o `test`, perché produrrebbero parti non utilizzabili in autonomia.

Usare una feature abilitante solo quando crea un contratto o una capacità stabile necessaria a più risultati e possiede un esito verificabile. Non usare feature abilitanti come contenitori generici di setup.

### Applicare il test di autonomia

Una feature è autonoma solo se, una volta soddisfatti i prerequisiti dichiarati:

- persegue un solo risultato riconoscibile;
- contiene il contesto necessario senza obbligare a ricostruirlo da altre feature;
- può essere sviluppata su un ramo dedicato senza attendere modifiche concorrenti non dichiarate;
- include tutti i layer e gli artefatti necessari al proprio comportamento completo;
- possiede criteri di accettazione osservabili e una strategia di verifica;
- non dipende da una feature futura per essere corretta, sicura o documentata;
- è abbastanza piccola da avere confini chiari e abbastanza completa da non essere un frammento tecnico.

Dividere ulteriormente quando una candidata contiene più flussi indipendenti, più esiti utente separabili, dipendenze differenti o criteri di accettazione non coesi. Unire due candidate soltanto quando separarle richiederebbe contratti artificiali o lascerebbe entrambe incomplete.

### Dichiarare le dipendenze

Nella feature dipendente elencare sempre:

- ID e titolo della feature richiesta;
- tipo di dipendenza;
- motivo concreto;
- artefatto o comportamento che deve essere disponibile;
- effetto sul parallelismo.

Usare soltanto questi tipi:

- **hard**: la feature non può iniziare in sicurezza prima che il prerequisito sia integrato;
- **contract**: le feature possono procedere in parallelo dopo che un contratto specifico è stato concordato e congelato.

Non chiamare dipendenza una semplice preferenza d'ordine. Se non esistono dipendenze, scrivere esplicitamente `Nessuna`.

Evitare cicli. Se compare un ciclo, ridisegnare i confini o estrarre il minimo contratto stabile come feature abilitante verificabile.

## Ordinare l'esecuzione e i parallelismi

1. Costruire il grafo delle dipendenze `hard`.
2. Ordinare topologicamente le feature.
3. Assegnare le wave: una feature entra nella prima wave successiva a tutte le sue dipendenze `hard`.
4. Collocare nella stessa wave le feature senza dipendenze `hard` reciproche.
5. Per una dipendenza `contract`, indicare il checkpoint da congelare prima del lavoro parallelo e le aree di file o contratto che richiedono coordinamento.
6. Evidenziare il percorso critico in base al grafo, senza inventare stime o date.

Definire parallelismi conservativi: l'assenza di una dipendenza funzionale non basta se due feature modificano lo stesso contratto instabile, la stessa migration o lo stesso componente centrale.

## Produrre gli artefatti

Leggere [references/backlog-templates.md](references/backlog-templates.md) prima di scrivere i file e rispettarne sezioni e campi.

Usare il percorso indicato dalla responsabile. Se manca, creare `backlog/` accanto alla cartella degli artefatti di brainstorming:

```text
backlog/
  README.md
  features/
    <feature-code>/
      feature.md
```

Produrre:

- `README.md` come indice autorevole con scope, gate, grafo, wave, parallelismi e copertura;
- un `feature.md` separato per ogni feature autonoma;
- codici `kebab-case` stabili e ID sequenziali `FEAT-001`, `FEAT-002`, ...

Inizializzare ogni `feature.md` con il frontmatter YAML seguente:

```yaml
---
status: Open
---
```

Trattare `status` come avanzamento della feature e `Readiness` come valutazione distinta della sua definizione. Usare esclusivamente questa macchina a stati:

```text
Open -> In progress -> In review -> Completed
                         |
                         +------------> Open
```

Consentire soltanto `Open -> In progress`, `In progress -> In review`, `In review -> Open` e `In review -> Completed`. Non impostare mai `Completed` autonomamente: eseguire quella transizione solo dopo un comando esplicito della responsabile umana. Non interpretare approvazioni implicite, verifiche verdi, merge, rilascio o assenza di commenti come autorizzazione al completamento.

Scrivere abbastanza dettaglio da rendere ogni feature eseguibile, ma non prescrivere classi, nomi di metodi o file nuovi quando l'architettura non li impone. Citare gli identificatori e i percorsi delle fonti invece di duplicarne lunghi brani.

## Consegnare

Nel riepilogo finale indicare:

- percorso dell'indice e numero di feature;
- numero di wave e parallelismi principali;
- gate bloccanti;
- percorso critico;
- requisiti eventualmente non coperti e motivo.

Non dichiarare il backlog pronto se esistono requisiti senza owner, dipendenze circolari o feature prive di criteri verificabili.

## Verificare il completamento

Controllare che:

- ogni requisito in scope abbia una feature primaria;
- nessuna feature introduca elementi out of scope;
- ogni feature superi il test di autonomia;
- ogni dipendenza compaia nella feature dipendente e nell'indice;
- tutte le dipendenze puntino a ID esistenti e non formino cicli;
- le wave rispettino le dipendenze `hard`;
- i parallelismi non condividano contratti instabili senza checkpoint;
- gate e ipotesi non siano presentati come decisioni già prese;
- criteri di accettazione e test coprano casi principali, errori, autorizzazione e vincoli pertinenti;
- gli obblighi di repository applicabili siano inclusi nella Definition of Done;
- ogni `feature.md` abbia un solo frontmatter `status`, inizializzato a `Open`, e usi esclusivamente gli stati e le transizioni consentiti;
- nessuna feature sia stata contrassegnata `Completed` senza un comando esplicito della responsabile umana;
- indice, schede e matrice di tracciabilità siano coerenti.
