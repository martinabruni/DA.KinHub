# Task List

La ricerca parte da `docs/kinlist/idea.md`, dai requisiti aggiuntivi forniti per KinList, dai documenti consolidati in `docs/kinlist/brainstorming/` e dai vincoli autorevoli di `AGENTS.md`. Il repository dispone gia di una SPA React, un backend Azure Functions .NET, autenticazione Microsoft Entra External ID, PostgreSQL e infrastruttura Azure condivisa; le ricerche distinguono questo contesto esistente dalle funzionalita ancora da progettare.

## Task

- task_code: pwa-shell-connectivity
- task_title: Shell PWA e comportamento con la rete
- goal: definire come KinList si installa, si avvia e comunica chiaramente che le funzioni vocali richiedono connettività.
- why_separate: installabilità, aggiornamenti e cache della shell hanno un ciclo di vita distinto dalla registrazione e dall'elaborazione AI.
- output_file: tasks/pwa-shell-connectivity/research.md

## Task

- task_code: voice-recording
- task_title: Registrazione vocale nel browser
- goal: definire un flusso di acquisizione audio mobile chiaro, accessibile e compatibile con i browser supportati.
- why_separate: permesso microfono, formati audio, avvio/arresto e guasti del dispositivo avvengono prima di qualsiasi elaborazione AI.
- output_file: tasks/voice-recording/research.md

## Task

- task_code: voice-to-list-ai
- task_title: Trasformazione dell'audio in item e categorie
- goal: stabilire un confine sicuro e verificabile tra client, backend, trascrizione e interpretazione AI.
- why_separate: trascrivere parole e trasformarle in dati applicativi sono responsabilità diverse, con errori, costi e controlli propri.
- output_file: tasks/voice-to-list-ai/research.md

## Task

- task_code: active-list-filtering
- task_title: Lista attiva, ordinamento e filtro categorie
- goal: rendere deterministico il raggruppamento dei nuovi item e semplice il filtro della lista attiva.
- why_separate: questo flusso riguarda lettura e navigazione della lista, non modifica, completamento o conservazione dei dati.
- output_file: tasks/active-list-filtering/research.md

## Task

- task_code: data-access-limits-pagination
- task_title: Limiti di accesso ai dati e paginazione
- goal: definire un contratto comune per paginare ogni lettura di collezioni e limitare in modo sicuro letture e scritture bulk.
- why_separate: cursori, dimensioni di pagina e ceiling configurabili sono vincoli trasversali del repository e delle API, indipendenti dal filtro o comando funzionale che li usa.
- output_file: tasks/data-access-limits-pagination/research.md

## Task

- task_code: item-edit-history
- task_title: Dettaglio, modifica e cronologia dell'item
- goal: definire il drawer che modifica nome e categorie e mostra metadati e timeline senza perdere aggiornamenti concorrenti.
- why_separate: il drawer è un flusso contestuale con validazione, salvataggio e gestione dei conflitti propri.
- output_file: tasks/item-edit-history/research.md

## Task

- task_code: complete-item-undo
- task_title: Completamento immediato e annullamento
- goal: rendere coerente la scomparsa dell'item, il cambio di stato e l'azione Annulla di cinque secondi.
- why_separate: è una transizione di stato reversibile e temporizzata, distinta dalla modifica ordinaria e dalla cancellazione definitiva.
- output_file: tasks/complete-item-undo/research.md

## Task

- task_code: completed-item-retention
- task_title: Conservazione e cancellazione dopo 30 giorni
- goal: eliminare definitivamente gli item completati in base a `CompletedAt` con un processo osservabile e ripetibile.
- why_separate: è un flusso backend pianificato senza superficie UI e con requisiti specifici di affidabilità e conservazione.
- output_file: tasks/completed-item-retention/research.md

## Task

- task_code: inactive-data-cleanup
- task_title: Pulizia definitiva dei dati inattivi
- goal: eliminare fisicamente user, membership, family e dati KinList soft-deleted o inattivi da almeno 30 giorni in modo limitato, coerente e osservabile.
- why_separate: condivide il Timer Trigger con la retention degli item completati, ma usa `inactiveAt`, collegamenti attivi, ordine transazionale e metriche lifecycle propri.
- output_file: tasks/inactive-data-cleanup/research.md

## Task

- task_code: family-onboarding
- task_title: Accesso obbligatorio tramite famiglia
- goal: definire il passaggio dopo il login che porta direttamente al servizio un membro associato e richiede agli altri di creare una famiglia o unirsi con un codice.
- why_separate: il riconoscimento dell'appartenenza e il blocco dell'accesso precedono sia l'uso della lista sia il ciclo di vita tecnico del codice d'invito.
- output_file: tasks/family-onboarding/research.md

## Task

- task_code: family-invite-code
- task_title: Invito pragmatico tramite codice
- goal: definire generazione, condivisione manuale e consumo sicuro di un codice che associa un utente autenticato a una famiglia.
- why_separate: il codice e una credenziale temporanea con rischi di enumerazione, concorrenza, scadenza e revoca distinti dall'onboarding e dalla lettura dei membri.
- output_file: tasks/family-invite-code/research.md

## Task

- task_code: family-authorization-policy
- task_title: Policy Family e verifica dell'appartenenza
- goal: definire il controllo server-side che combina `familyId` in query string, user ID dai claim e associazione corrente nel database.
- why_separate: e un confine di sicurezza trasversale a tutte le API nel perimetro famiglia e deve restare indipendente dalle singole schermate e regole degli item.
- output_file: tasks/family-authorization-policy/research.md

## Task

- task_code: floating-settings-entry
- task_title: Accesso flottante alle impostazioni
- goal: rendere l'ingranaggio fisso in basso a destra accessibile, compatibile con la safe area e capace di aprire la pagina Impostazioni.
- why_separate: posizione, navigazione, focus e sovrapposizioni con microfono e snackbar sono problemi di shell frontend distinti dai dati della famiglia.
- output_file: tasks/floating-settings-entry/research.md

## Task

- task_code: family-settings-members
- task_title: Famiglia e membri nelle impostazioni
- goal: mostrare nome della famiglia, elenco essenziale dei membri e accesso all'invito tramite codice.
- why_separate: la consultazione dei membri ha dati, stati e autorizzazione propri e non deve essere confusa con generazione e consumo della credenziale d'invito.
- output_file: tasks/family-settings-members/research.md

## Task

- task_code: bulk-item-completion
- task_title: Selezione multipla e completamento bulk
- goal: completare con un'unica intenzione piu item selezionati mantenendo autorizzazione, concorrenza e feedback coerenti.
- why_separate: un comando bulk introduce atomicita, limiti e riconciliazione diversi dalla transizione e dall'undo del singolo item.
- output_file: tasks/bulk-item-completion/research.md

## Task

- task_code: item-visibility-scope
- task_title: Visibilita Personal e Shared
- goal: predisporre item personali visibili al solo autore e item condivisi visibili alla famiglia, mantenendo `Shared` come default corrente.
- why_separate: la visibilita e una regola trasversale a query, dettaglio, modifica, completamento e bulk, non una semplice opzione di presentazione.
- output_file: tasks/item-visibility-scope/research.md

La scomposizione copre il percorso «login → famiglia → servizio», le impostazioni familiari, l'isolamento delle API e le nuove operazioni sugli item, oltre al percorso gia studiato «Parla → Ottieni la lista → Spunta». Non introduce ricerca utenti, inviti via email, rimozione membri, nuovi ruoli, notifiche, piu famiglie selezionabili o una schermata degli item completati.
