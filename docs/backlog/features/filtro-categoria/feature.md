---
status: Open
---

# FEAT-008 - Filtrare la lista per categoria

- **Codice**: `filtro-categoria`
- **Tipo**: `product`
- **Readiness**: `ready`
- **Wave**: 5
- **Risultato**: un membro restringe la lista a una categoria alla volta, prima della paginazione, e può tornare facilmente alla vista completa.

## Contesto autonomo

Il filtro modifica solo la vista e deve usare lo stesso predicato famiglia/visibility della lista. Una categoria alla volta è evidenziata con più del colore; il cambio filtro riparte dalla prima pagina e definisce il perimetro futuro di `Seleziona tutti`.

## Scope

### Incluso

- Carosello categorie visibili della famiglia, paginato/limitato se necessario.
- Filtro singolo server-side prima della paginazione item e rimozione semplice.
- Prima pagina a ogni cambio, ordine invariato, stato filtro senza risultati distinto.
- Cursore legato al filtro e recupero da cursore stale/incompatibile.
- Accessibilità, responsive, i18n, telemetria e contratti per FEAT-011.

### Escluso

- Filtro multiplo, ricerca, categorie globali/predefinite e selezione bulk effettiva.

## Tracciabilità

| Tipo | Riferimenti | Contributo della feature |
|---|---|---|
| Flussi | FLOW-005 | Filtro e navigazione pagine |
| Requisiti | FR-017, FR-018 | Filtro singolo e stato vuoto dedicato |
| Regole/decisioni | BR-008, BR-016, BR-037, BR-038; DEC-004, DEC-028 | Catalogo famiglia e filtro server |
| Architettura | ADR-014, ADR-017; sezione 6.3 | Visibility e keyset legato al filtro |

## Dipendenze

### Feature prerequisite

| Feature | Tipo | Motivo | Output richiesto | Effetto sul parallelismo |
|---|---|---|---|---|
| FEAT-003 - Consultare la lista condivisa paginata | hard | Estende query, pagina e stati della lista | Contratto pagina/cursori, visibility e ordine | Inizio dopo FEAT-003 |
| FEAT-014 - Usare un design system condiviso in tutta KinHub | hard | Il carosello categorie deve riusare chip, pagination e stati condivisi | Primitive chip/carousel/state e regole di riuso frontend | Inizio dopo integrazione FEAT-014 |

### Gate e assunzioni

Nessuno. L'ordine totale delle categorie è una verifica TECH-003 già avviata da FEAT-003 e va chiusa qui per il catalogo.

### Parallelismo consentito

Con FEAT-005/007/009 dopo CP-002, con ownership coordinata della query lista e dei componenti di pagina.

## Contratto di consegna

### Comportamento

- Nessun filtro mostra tutti gli item attivi visibili; una categoria mostra solo corrispondenze visibili.
- Cambio/rimozione filtro riparte dalla prima pagina senza modificare dati o ordine.
- Zero risultati mostra un messaggio specifico e l'azione di rimozione, non il microfono dello stato vuoto reale.
- Il client non interpreta il cursore e non filtra un dataset completo.

### Touchpoint previsti

- **Dominio/business**: query catalogo e filtro categoria scoped.
- **Persistenza/migration**: indici associazioni/ordine e keyset.
- **API/integrazioni**: parametri filtro/cursore con `Family`.
- **Frontend/UX**: pagina lista, carosello, stato filtro vuoto e paginazione costruiti sul design system condiviso.
- **Infrastruttura/configurazione**: Nessuna.
- **Documentazione/operazioni**: help/guida lista e change fragment.

### Errori, sicurezza e osservabilità

- Categorie e conteggi usano visibility prima della proiezione; nessun leak Personal.
- Cursore errato non restituisce dati e offre ripartenza dalla prima pagina filtrata.
- Metriche usano presenza filtro/page size, non nomi categoria o contenuto cursore.

## Criteri di accettazione

### AC-046 - Filtro server prima della pagina

- **Dato** item in più categorie e più pagine
- **Quando** il membro sceglie una categoria
- **Allora** la prima pagina contiene solo item attivi visibili associati, nell'ordine originale
- **Fonte**: FR-017, BR-008

### AC-047 - Selezione singola accessibile

- **Dato** categorie disponibili
- **Quando** una viene selezionata
- **Allora** solo quella è attiva e lo stato è percepibile senza affidarsi al solo colore
- **Fonte**: FR-017, NFR-009

### AC-048 - Vuoto filtrato distinto

- **Dato** una categoria senza item visibili
- **Quando** viene applicata
- **Allora** appare lo stato filtro vuoto con rimozione, distinto dalla lista realmente vuota
- **Fonte**: FR-018, FLOW-005

### AC-049 - Cambio filtro e cursori

- **Dato** una pagina successiva o un cursore legato a un altro filtro
- **Quando** il filtro cambia o il cursore viene riusato
- **Allora** si riparte dalla prima pagina oppure si riceve errore recuperabile senza dati estranei
- **Fonte**: FR-051, BR-038

## Strategia di verifica

| Livello | Verifica | Evidenza attesa |
|---|---|---|
| Unitario | Perimetro filtro e reset pagina | Test business |
| Integrazione | Filtro prima del keyset e no leak | Test PostgreSQL/API |
| Frontend/component | Carosello, vuoto, tastiera e annunci | Test componenti/accessibilità |
| End-to-end/manuale | Pagine, cambio filtro e Personal altrui | Vista coerente senza leak |
| Validator repository | Qualità frontend/backend/docs/release | Esiti registrati |

## Definition of Done

- AC-046-AC-049 verificati, FEAT-003 integrata e CP-002 rispettato.
- Il filtro usa solo chip/carousel/state component FEAT-014 e non introduce una seconda UI di selezione categorie.
- TECH-003 chiuso per catalogo/filtro; indici e query verificati.
- i18n, accessibilità, temi, help/guida, telemetria e fragment aggiornati.
- Comandi applicabili di `AGENTS.md` eseguiti.
- Nessun filtro multiplo/client-side o categoria globale introdotta.
