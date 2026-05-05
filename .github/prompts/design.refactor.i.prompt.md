Refactoring completo della UI/UX dell'applicazione Kin Hub. Analizza l'intera codebase prima di apportare qualsiasi modifica.

## Contesto

Kin Hub è un **hub di servizi**. Ogni servizio (es. Kin Recipe, altri futuri) è un modulo indipendente accessibile tramite una card nella home. L'applicazione non deve essere strutturata attorno a un singolo servizio.

Il **login** è associato all'account famiglia. Una volta autenticati, si sceglie con quale **profilo membro** operare. Ogni membro può avere un ruolo (es. utente standard, amministratore).

---

## 1 — Side menu: ristrutturazione per servizio

Il side menu attuale contiene voci appartenenti a servizi specifici (es. Kin Recipe) mescolate con la navigazione globale dell'hub. Questo è sbagliato.

**Obiettivo:** il side menu globale deve contenere **solo** le voci di navigazione dell'hub. Le voci di un servizio devono essere visibili **esclusivamente** quando l'utente ha aperto quel servizio (navigando dalla sua card).

- Identifica tutte le voci del side menu attualmente presenti.
- Raggruppa quelle che appartengono a un servizio specifico (es. Kin Recipe).
- Sposta tali voci in una navigazione contestuale interna alla pagina/sezione del servizio corrispondente.
- Applica questo pattern in modo consistente per ogni servizio presente e futuro.

---

## 2 — Autenticazione amministratore: verifica con codice admin

Il flusso di accesso è il seguente:

1. Login con le credenziali dell'account famiglia.
2. Selezione del profilo membro con cui operare.
3. Se il membro selezionato ha ruolo **amministratore**, viene richiesto un **codice admin** prima che i privilegi elevati vengano abilitati.

Attualmente questo terzo step non esiste: selezionando un profilo amministratore, i privilegi vengono concessi immediatamente senza alcuna verifica aggiuntiva.

**Obiettivo:** aggiungere lo step di verifica del codice admin dopo la selezione del profilo amministratore.

- Individua il punto del flusso in cui il profilo/ruolo viene risolto dopo la selezione del membro.
- Inserisci uno step di richiesta del codice admin prima di abilitare i privilegi amministrativi.
- Se il codice non viene inserito o è errato, l'utente deve poter comunque accedere ma senza privilegi admin (o tornare alla selezione profilo).

---

## 3 — Rimozione endpoint inesistenti

Sono presenti chiamate API verso endpoint che non esistono nel progetto API di backend.

**Obiettivo:** rimuovere tutte le chiamate a endpoint non definiti nel progetto API.

- Analizza il progetto API per ricavare la lista degli endpoint effettivamente esistenti.
- Confronta con tutte le chiamate HTTP presenti nel frontend.
- Elimina ogni chiamata verso endpoint non esistenti.
- Non inventare o aggiungere endpoint nuovi: usa solo ciò che il backend espone realmente.
