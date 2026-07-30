# Prompt di bootstrap — KinHub

Agisci come **senior full-stack/cloud engineer, software architect e DevOps engineer**. Parti da un repository completamente vuoto e implementa realmente l’intero bootstrap dell’applicazione descritta di seguito.

Non limitarti a proporre un’architettura o a descrivere file ipotetici: **crea codice, progetti, configurazioni, documentazione, test, pipeline, infrastruttura e file operativi funzionanti**, usando placeholder espliciti solo per valori esterni e segreti.

---

## 1. Variabili del progetto

Usa queste variabili come fonte autorevole in tutto il repository:

```text
APP_NAME=KinHub
APP_DOMAIN=kinhub
APP_DOMAIN_DESCRIPTION=piattaforma semplice e intuitiva per la famiglia, che raggruppa piu servizi (es. KinRecipe, KinList, ecc...). Meno inquinamento visivo c'e', meglio e'. Colori tema customizzabili, preesistenti dark, light.
DEFAULT_LOCALE=it
SUPPORTED_LOCALES=it,en
```

Usale coerentemente in:

- naming;
- namespace;
- assembly;
- titoli UI;
- configurazioni;
- documentazione;
- database;
- pipeline;
- pacchetti di deployment backend e relativi metadati;
- risorse Azure;
- telemetria;
- versioning;
- manifest PWA;
- file di traduzione;
- patch note;
- skill e relativo harness.

Non inventare tenant, client ID, subscription, password, token o secret reali.

---

## 2. Obiettivo

Predisponi **KinHub** come piattaforma web full-stack pronta per:

- sviluppo locale;
- build;
- test;
- esecuzione locale e packaging della Function App;
- osservabilità;
- deploy Azure;
- CI/CD GitHub;
- internazionalizzazione italiano/inglese;
- funzionamento light/dark;
- installazione PWA;
- tutorial iniziale;
- documentazione integrata nel prodotto;
- gestione patch note;
- evoluzione controllata di componenti e servizi riutilizzabili tramite skill di progetto.

Stack obbligatorio:

- backend: **.NET 10**;
- frontend: **React + TypeScript**;
- build frontend: Vite o equivalente moderno e motivato;
- UI: **shadcn/ui**;
- database: **PostgreSQL**;
- ORM: **Entity Framework Core**;
- autenticazione: **Microsoft Entra External ID**;
- deploy Azure: **Bicep**;
- CI/CD: **GitHub Actions**;
- backend: **Azure Functions 4.x, .NET 10 Isolated Worker, Linux, piano Flex Consumption**;
- frontend: Azure Static Web Apps;
- frontend installabile come PWA desktop/mobile;
- test backend: xUnit;
- nessun test frontend in questa prima fase, salvo controlli statici essenziali richiesti per documentazione, routing o traduzioni.

---

## 3. Regole operative non negoziabili

1. Crea file reali, non pseudocodice.
2. Il repository deve compilare, salvo dipendenze da credenziali o risorse esterne chiaramente documentate.
3. Tutti i secret devono arrivare da variabili ambiente, GitHub Secrets, Key Vault o configurazioni locali non versionate.
4. Tutti i testi visibili all’utente devono passare dal sistema i18n.
5. Ogni pagina deve avere documentazione contestuale localizzata.
6. Ogni nuova funzionalità utente deve produrre o aggiornare:
   - traduzioni italiane e inglesi;
   - documentazione;
   - patch note;
   - test backend pertinenti;
   - skill di progetto, quando introduce un elemento riutilizzabile.
7. `AGENTS.md` nella root è la fonte di istruzioni per agenti e contributor e deve contenere le regole di questo prompt.
8. Le modifiche di codice in produzione non devono essere eseguite come codice arbitrario caricato dinamicamente. Lo skill harness serve a rendere aggiornabili e rileggibili a runtime di sviluppo le **conoscenze operative e i cataloghi di componenti/servizi riutilizzabili**, mentre il codice di produzione continua a passare da build, test e deploy versionati.
9. Usa convenzioni pulite e pragmatiche, senza sovraingegnerizzare.
10. Non inventare stringhe di versione, runtime, SKU, model version, flag CLI o parametri provider: prima verifica i valori supportati e il formato richiesto dalla piattaforma o dalla CLI correnti.
11. Ogni cambio di versioni, runtime, env var, app setting, parametro Bicep, secret, namespace o artifact name deve aggiornare nello stesso change tutti i consumer repository-wide: codice, script, workflow, README, prompt, documentazione e file generati.
12. Ogni modifica ai workflow deve essere verificata contro i contratti reali del repository: path, artifact, vars/secrets, permessi `GITHUB_TOKEN`, output, workflow riusabili e sintassi esatta dei comandi Azure.
13. Quando modifichi una fonte autorevole che genera output versionati, rigenera subito gli artefatti derivati e non correggerli a mano salvo esplicita scelta architetturale.

---

## 4. Struttura repository obbligatoria

Crea almeno la seguente struttura, aggiungendo i file necessari:

```text
/
├── AGENTS.md
├── CHANGELOG.md
├── README.md
├── VERSION
├── .editorconfig
├── .gitignore
├── docs/
│   ├── README.md
│   ├── architecture/
│   ├── development/
│   ├── operations/
│   ├── user-guide/
│   │   ├── it/
│   │   └── en/
│   ├── patch-notes/
│   │   ├── it/
│   │   └── en/
│   ├── FP/
│   │   ├── README.md
│   │   ├── README.it.md
│   │   └── README.en.md
│   └── CR/
│       ├── README.md
│       ├── README.it.md
│       └── README.en.md
├── changes/
│   └── README.md
├── skills/
│   ├── README.md
│   ├── registry.json
│   ├── frontend/
│   │   └── SKILL.md
│   ├── backend/
│   │   └── SKILL.md
│   ├── architecture/
│   │   └── SKILL.md
│   ├── documentation/
│   │   └── SKILL.md
│   └── release/
│       └── SKILL.md
├── tools/
│   ├── skill-harness/
│   ├── docs-sync/
│   └── release-notes/
├── tests/
│   ├── DA.KinHub.Domain.Tests/
│   ├── DA.KinHub.Business.Tests/
│   └── DA.KinHub.IntegrationTests/
├── src/
│   ├── backend/
│   │   ├── domains/
│   │   ├── infrastructure/
│   │   ├── business/
│   │   └── applications/
│   └── frontend/
├── infra/
│   ├── modules/
│   │   ├── function-app-flex.bicep
│   │   ├── storage.bicep
│   │   ├── observability.bicep
│   │   ├── postgres.bicep
│   │   ├── key-vault.bicep
│   │   └── static-web-app.bicep
│   ├── app.bicep
│   ├── main.dev.bicepparam
│   └── README.md
├── scripts/
│   ├── package-backend.sh
│   └── package-backend.ps1
└── .github/
    ├── pull_request_template.md
    └── workflows/
```

La struttura può essere estesa, ma non ridotta nei punti richiesti.

---

## 5. `AGENTS.md`

Crea nella root un file `AGENTS.md` completo e autorevole.

Deve spiegare almeno:

- identità e obiettivi di KinHub;
- stack e architettura;
- struttura del repository;
- regole DDD;
- regole frontend;
- regole backend;
- i18n italiano/inglese;
- documentazione in-app;
- accordion documentale obbligatorio in ogni pagina;
- tutorial iniziale;
- temi light/dark;
- PWA;
- versioning;
- patch note;
- skill harness;
- come promuovere un componente UI riutilizzabile nella skill frontend;
- come promuovere un servizio business riutilizzabile nella skill backend;
- Definition of Done;
- sicurezza;
- test;
- CI/CD;
- comandi principali, inclusi avvio locale, publish e packaging della Function App;
- regole specifiche di Azure Functions Isolated Worker e Flex Consumption;
- obbligo di aggiornare `AGENTS.md` quando cambiano regole strutturali.

Il file deve essere scritto in modo che un coding agent possa leggerlo prima di ogni modifica e operare senza perdere le convenzioni del progetto.

---

## 6. Skill harness di progetto

Predisponi un sistema di skill locali nella cartella `skills/`.

### 6.1 Scopo

Le skill sono conoscenza operativa versionata del repository. Devono descrivere pattern, componenti, servizi, contratti, esempi, limiti e procedure riutilizzabili.

Esempi:

- un nuovo componente UI generico utile a più pagine deve essere registrato nella skill frontend;
- un servizio backend di business riusabile da più use case deve essere registrato nella skill backend;
- una nuova convenzione architetturale deve aggiornare la skill architecture;
- nuove regole di documentazione o release devono aggiornare le skill dedicate.

### 6.2 Struttura minima di ogni skill

Ogni skill deve avere almeno:

```text
skills/<area>/
├── SKILL.md
├── catalog.json          # se utile
├── examples/             # se utile
└── templates/            # se utile
```

`SKILL.md` deve contenere:

- scopo;
- quando usare la skill;
- quando non usarla;
- componenti o servizi disponibili;
- API o interfacce;
- esempi;
- dipendenze;
- vincoli;
- test richiesti;
- checklist di aggiornamento;
- changelog locale della skill o riferimento alle patch note.

### 6.3 Harness

Crea in `tools/skill-harness/` un piccolo tool funzionante, documentato e testabile che:

- esegua la scansione di `skills/**/SKILL.md`;
- validi struttura e metadati;
- generi o aggiorni `skills/registry.json`;
- esponga un comando per elencare le skill;
- esponga un comando per leggere la skill corretta per area;
- esponga un comando `watch` per rileggere e rigenerare il registry quando i file cambiano in sviluppo;
- segnali riferimenti non validi o duplicati;
- non esegua codice arbitrario contenuto nelle skill;
- produca output deterministici e stabili tra Windows/Linux, inclusi checksum coerenti a parita di contenuto;
- possa essere richiamato da script npm, dotnet tool locale o comando equivalente;
- venga eseguito in CI in modalità `validate`.

Prevedi comandi simili a:

```bash
npm run skills:list
npm run skills:validate
npm run skills:build
npm run skills:watch
```

oppure equivalenti, purché documentati e realmente implementati.

### 6.4 Regola di promozione

Quando viene creato qualcosa di riutilizzabile:

1. implementalo nel layer corretto;
2. aggiungi test ed esempio;
3. documentalo;
4. registralo nel catalogo della skill appropriata;
5. aggiorna `SKILL.md`;
6. aggiorna `skills/registry.json` tramite harness;
7. aggiungi una change fragment;
8. aggiorna la documentazione localizzata, se visibile agli utenti;
9. verifica che `AGENTS.md` resti coerente.

---

## 7. Documentazione e i18n

### 7.1 Locali

Supporta obbligatoriamente:

- italiano: `it`;
- inglese: `en`.

Italiano come lingua predefinita; inglese come fallback esplicito o fallback tecnico documentato.

### 7.2 Regole frontend

Usa una libreria i18n matura, ad esempio `i18next` con `react-i18next`.

Predisponi:

- rilevamento lingua;
- selettore lingua;
- persistenza della scelta;
- namespace di traduzione;
- interpolation sicura;
- fallback;
- formattazione localizzata di date, numeri e percentuali;
- gestione delle chiavi mancanti in sviluppo;
- file di traduzione organizzati per feature;
- controllo CI che verifichi parità delle chiavi fra `it` ed `en`.

Non inserire stringhe visibili hardcoded nei componenti.

### 7.3 Documentazione tecnica e utente

La documentazione deve esistere in entrambe le lingue quando è destinata all’utente.

Crea:

```text
docs/user-guide/it/
docs/user-guide/en/
```

La documentazione utente deve essere visualizzabile anche nel sito. Mantieni una sola fonte Markdown e crea un tool `tools/docs-sync/` che:

- valida la presenza delle due lingue;
- genera o copia contenuti consumabili dal frontend;
- preserva slug e metadati;
- segnala pagine mancanti;
- venga eseguito durante la build frontend e in CI.

La documentazione tecnica interna può essere in italiano, ma le sezioni destinate all’utente, le patch note e l’help contestuale devono essere bilingui.

---

## 8. Accordion documentale obbligatorio in ogni pagina

Ogni pagina o route applicativa deve mostrare, nella parte alta della pagina e subito dopo il titolo principale, un componente condiviso simile a:

```text
<PageHelpAccordion />
```

Il componente deve:

- essere accessibile da tastiera;
- usare shadcn/ui Accordion;
- essere chiuso di default, salvo scelta motivata;
- mostrare contenuti localizzati;
- spiegare:
  - a cosa serve la pagina;
  - cosa può fare l’utente;
  - prerequisiti;
  - significato dei principali campi o azioni;
  - eventuali limitazioni;
  - link alla guida completa;
- adattarsi a mobile;
- rispettare light/dark;
- non duplicare testo direttamente nei componenti pagina.

Crea un registry delle route con una chiave documentale obbligatoria. Aggiungi un controllo automatico che fallisca se una route non possiede:

- titolo localizzato;
- help localizzato italiano;
- help localizzato inglese;
- collegamento alla guida utente pertinente.

Anche la pagina 404 e le pagine di errore devono avere una spiegazione minima appropriata.

---

## 9. Tutorial iniziale

Implementa un tutorial/onboarding iniziale localizzato.

Requisiti:

- avvio al primo accesso autenticato o al primo avvio, secondo la struttura scelta;
- disponibile in italiano e inglese;
- accessibile e responsive;
- possibilità di saltare;
- possibilità di tornare indietro;
- possibilità di riavviarlo da Help/Impostazioni;
- persistenza dello stato completato;
- gestione della versione del tutorial, così da poter mostrare nuovi passaggi dopo cambiamenti rilevanti;
- nessun blocco permanente dell’utente;
- step collegati a elementi stabili dell’interfaccia;
- fallback se un elemento target non è presente;
- documentazione in `docs/user-guide/{locale}/getting-started.md`.

Includi almeno passaggi per:

- navigazione;
- selezione lingua;
- cambio tema;
- documentazione contestuale;
- area versione/patch note;
- concetto generale del ciclo di vita progetto.

---

## 10. Tema light e dark

Usa il sistema theming di shadcn/ui e CSS variables.

Implementa:

- tema `light`;
- tema `dark`;
- opzione `system`;
- selettore tema;
- persistenza;
- rispetto di `prefers-color-scheme`;
- assenza di flash del tema errato al caricamento;
- contrasto accessibile;
- compatibilità di grafici, toast, dialog, accordion e tutorial;
- traduzioni delle etichette;
- documentazione utente.

---

## 11. Patch note, changelog e release note

Predisponi un sistema completo.

### 11.1 File obbligatori

Crea:

```text
CHANGELOG.md
changes/README.md
docs/patch-notes/it/
docs/patch-notes/en/
```

Usa Semantic Versioning.

`CHANGELOG.md` deve seguire una struttura coerente con Keep a Changelog:

- Added;
- Changed;
- Deprecated;
- Removed;
- Fixed;
- Security.

### 11.2 Change fragments

Ogni modifica significativa deve aggiungere un file in `changes/`, ad esempio:

```text
changes/1234-added-project-dashboard.md
```

con metadati:

- tipo;
- area;
- descrizione italiana;
- descrizione inglese;
- breaking change sì/no;
- eventuale issue/PR.

Crea `tools/release-notes/` per:

- validare i fragment;
- aggregarli;
- aggiornare `CHANGELOG.md`;
- generare patch note italiane e inglesi;
- generare un JSON consumabile dal frontend;
- includere versione, commit, data build e ambiente;
- supportare CI/CD.

### 11.3 Interfaccia

Crea una pagina `Release notes`/`Note di rilascio` nel frontend, localizzata, con:

- versione corrente;
- cronologia;
- categorie;
- data;
- breaking changes evidenziate;
- link alla documentazione pertinente.

Il componente About/Version deve collegarsi alle patch note.

---

## 12. Backend

Dentro `src/backend` crea una solution .NET 10 con architettura DDD a singolo dominio, separata in:

```text
src/backend/domains
src/backend/infrastructure
src/backend/business
src/backend/applications
```

Usa namespace coerenti, ad esempio `DA.KinHub.*`.

### 12.1 Domains

Crea una class library contenente:

- entity principali;
- classi e interfacce di dominio;
- value object utili;
- contratti base;
- domain exceptions;
- astrazioni repository;
- domain events solo se realmente utili;
- nessuna dipendenza da EF Core o framework infrastrutturali.

Predisponi un modello iniziale minimo coerente con il dominio, ad esempio progetto, fase progettuale, documento richiesto, alert o improvement, senza implementare funzionalità eccessive.

### 12.2 Infrastructure

Crea una class library contenente:

- EF Core;
- provider PostgreSQL;
- `DbContext`;
- entity configuration;
- repository concreti;
- migration;
- storage abstraction implementation;
- telemetria tecnica;
- implementazioni delle interfacce di dominio;
- health check database.

Le migration devono essere gestite in modo compatibile con un runtime serverless e multiistanza:

- non assumere che la Function App sia singleton;
- evitare migration lunghe o non deterministiche durante il cold start;
- abilitare l’applicazione automatica all’avvio solo in locale/dev tramite feature flag;
- in produzione, preferire un passaggio CI/CD esplicito prima del deploy del codice, usando migration EF Core o migration bundle;
- se è previsto anche un fallback all’avvio, proteggerlo con PostgreSQL advisory lock o strategia equivalente;
- logging, timeout e fallimento esplicito;
- documentare applicazione, rollback e verifica manuale.

### 12.3 Business

Crea una class library contenente:

- use case;
- servizi applicativi;
- validazioni;
- orchestrazione;
- DTO interni;
- contratti;
- gestione errori;
- pattern pragmatici, senza introdurre CQRS o mediator se non realmente giustificati.

I servizi riutilizzabili devono essere registrati nella skill backend.

### 12.4 Applications

Crea una **Azure Function App .NET 10 Isolated Worker**, basata su Azure Functions runtime 4.x e destinata a Linux Flex Consumption.

La Function App deve contenere e configurare:

- progetto `Microsoft.NET.Sdk` con Azure Functions Worker SDK compatibile con .NET 10;
- `Program.cs` come entry point;
- `host.json` versionato;
- `local.settings.json.example` o documentazione equivalente, senza versionare `local.settings.json` reale;
- `dotnet-isolated` come runtime autorevole del progetto; usa l'app setting legacy `FUNCTIONS_WORKER_RUNTIME` solo dove il percorso di esecuzione o tooling lo richiede davvero, evitando duplicazioni non supportate su Flex Consumption quando `functionAppConfig.runtime` e gia sufficiente;
- integrazione ASP.NET Core per HTTP trigger, quando utile e supportata;
- endpoint REST;
- dependency injection;
- Microsoft Entra ID JWT bearer;
- authorization policy;
- Swagger/OpenAPI;
- health checks;
- readiness/liveness;
- version endpoint;
- build metadata endpoint;
- endpoint stato applicativo;
- Problem Details;
- CORS configurabile;
- logging strutturato;
- Application Insights tramite Azure Monitor/OpenTelemetry, senza mantenere in parallelo una seconda pipeline classica permanente;
- correlation ID;
- avvio migration;
- configurazione ambienti;
- rate limiting base se appropriato;
- API versioning se utile, senza complessità superflua.

Configura il route prefix in `host.json` in modo coerente con gli endpoint richiesti. Endpoint minimi:

```text
GET /health/live
GET /health/ready
GET /api/version
GET /api/status
```

`/api/version` deve restituire almeno:

- app name;
- semantic version;
- commit SHA;
- build date;
- environment;
- API version.

L’avvio della Function App deve restare leggero: non eseguire scansioni, migrazioni o inizializzazioni che possano rendere il cold start fragile o superare i limiti del runtime.

Per configurazioni cloud opzionali come exporter o integrazioni osservabili, l'avvio locale/dev deve degradare in modo esplicito quando manca uno setting richiesto, senza introdurre crash di bootstrap evitabili.

---

## 13. Test backend

Dentro `tests` crea progetti xUnit .NET 10 per:

- dominio;
- business logic;
- integrazione API/infrastruttura, se utile.

I test iniziali devono coprire almeno:

- una regola di dominio;
- una validazione business;
- endpoint versione/stato;
- registrazione DI;
- serializzazione Problem Details o comportamento equivalente;
- validazione di una configurazione critica.

Non creare test frontend completi in questa fase. Sono ammessi script statici per verificare:

- parità delle traduzioni;
- copertura documentale delle route;
- validità del registry skill;
- validità delle change fragments.

---

## 14. Frontend

Dentro `src/frontend` crea un’app React + TypeScript.

Requisiti:

- shadcn/ui;
- routing client-side;
- refresh browser/F5 sulla route corrente funzionante in Azure Static Web Apps;
- API client tipizzato;
- autenticazione Entra ID con MSAL;
- responsive mobile-first;
- PWA;
- i18n italiano/inglese;
- light/dark/system;
- tutorial iniziale;
- accordion documentale in ogni pagina;
- pagina About/Version;
- pagina Release notes;
- notifiche di nuova versione;
- error boundary;
- gestione loading/empty/error states;
- accessibilità;
- struttura per feature;
- nessun secret nel bundle.

Predisponi almeno pagine iniziali:

- Home/Dashboard;
- Projects/Progetti;
- About/Version;
- Release notes;
- Settings/Impostazioni;
- Not Found.

Le funzionalità possono essere scaffold iniziali, ma navigazione, layout, i18n, tema, tutorial, help accordion e versioning devono essere funzionanti.

### 14.1 Version notification

Implementa un meccanismo che:

- esponga la versione frontend build-time;
- interroghi periodicamente o al focus un file/version endpoint;
- confronti versione corrente e versione disponibile;
- mostri una notifica localizzata;
- consenta refresh controllato;
- gestisca service worker e cache;
- eviti loop di refresh.

---

## 15. PWA

Predisponi:

- manifest;
- icone placeholder documentate;
- service worker o plugin equivalente;
- installabilità desktop/mobile;
- gestione update;
- offline fallback minimo;
- caching prudente;
- nome e short name coerenti;
- theme color compatibile con tema;
- documentazione installazione;
- note su limiti iOS/Android/desktop.

---

## 16. Autenticazione Microsoft Entra ID

Predisponi:

### Backend

- validazione token;
- authority;
- tenant;
- audience;
- scope/role placeholders;
- policy authorization;
- Swagger OAuth configuration se appropriata.

### Frontend

- login;
- logout;
- account selection;
- redirect o popup scelto e documentato;
- protected routes;
- token acquisition;
- API scope;
- error handling.

Usa placeholder chiari, ad esempio:

```text
<ENTRA_TENANT_ID>
<ENTRA_TENANT_SUBDOMAIN>
<ENTRA_FRONTEND_CLIENT_ID>
<ENTRA_BACKEND_CLIENT_ID>
<ENTRA_API_SCOPE>
<ENTRA_REDIRECT_URI>
```

Documenta configurazione app registration, redirect URI, expose an API, scope e permessi.

---

## 17. Versioning

Predisponi versioning condiviso backend/frontend.

Requisiti:

- Semantic Versioning;
- file `VERSION` nella root;
- versione disponibile nel backend;
- versione inclusa nel frontend;
- commit SHA;
- build date;
- environment;
- pacchetto backend `.zip` denominato con versione e SHA, ad esempio `kinhub-backend-1.2.3-<sha>.zip`;
- metadati di versione incorporati nell’assembly e inclusi nell’endpoint `/api/version`;
- integrazione GitHub Actions;
- About/Version;
- release notes;
- notifica nuova versione PWA;
- nessun incremento manuale duplicato in più file.

---

## 18. Infrastruttura Azure con Bicep

### Parte 2 — Infrastruttura applicativa

Dentro `infra/` crea file modulari e parametrizzabili per una Azure Function App Linux su piano Flex Consumption.

`infra/app.bicep` deve creare almeno:

- Azure Functions Flex Consumption plan dedicato alla singola Function App, con SKU `FC1` e tier `FlexConsumption`;
- Azure Function App Linux con runtime `dotnet-isolated` e versione runtime coerente con .NET 10 nel formato esatto richiesto dall'API Azure scelta;
- `functionAppConfig` per runtime, deployment, scala e concorrenza;
- Azure Storage Account richiesto da Functions;
- blob container privato dedicato al pacchetto di deployment One Deploy;
- autenticazione al deployment storage tramite managed identity, evitando shared key quando possibile;
- configurazione `AzureWebJobsStorage` tramite identity-based connection quando supportata, con una sola fonte di verita: `accountName` oppure gli URI espliciti richiesti, non entrambe;
- Azure Database for PostgreSQL Flexible Server;
- database applicativo;
- Application Insights;
- Log Analytics Workspace;
- Key Vault;
- Azure Static Web Apps, con location fissata a `westeurope` direttamente nel Bicep e non esposta come parametro o record di configurazione separato;
- managed identity user-assigned o system-assigned, motivando la scelta;
- RBAC least privilege verso Storage, Key Vault, Application Insights e altre risorse;
- ruoli dati minimi realmente necessari per lo storage host Functions, inclusi blob/queue/table quando richiesti dal runtime o dalla configurazione identity-based adottata;
- app settings e Key Vault references;
- CORS/allowed origins;
- HTTPS only e TLS minimo;
- configurazione di rete parametrica, lasciando VNet integration opzionale e disabilitata di default per contenere costi e complessità;
- output utili per pipeline e configurazione frontend.

### 18.2 Parametri Flex Consumption

Usa parametri espliciti per:

- `environmentName`;
- location, verificando che supporti Flex Consumption;
- naming prefix;
- runtime `dotnet-isolated`;
- runtime version `.NET 10` nel formato richiesto dall’API Azure usata;
- `instanceMemoryMB`, con valori consentiti `512`, `2048` o `4096` e default `2048`;
- `maximumInstanceCount`, con limiti validati e default `20`, adeguato a un progetto personale e facile da aumentare in seguito;
- concorrenza HTTP solo se realmente necessaria, lasciando il comportamento predefinito della piattaforma come scelta iniziale;
- always-ready instances, default `0` per minimizzare i costi;
- nome del deployment blob container;
- Entra ID;
- database;
- allowed origins;
- configurazioni non segrete.

La location della Static Web App non deve essere parametrizzata: impostala in modo esplicito a `westeurope` nel template Bicep.

Per questo repository considera best practice pragmatiche da progetto personale, non enterprise: semplicità operativa, costi bassi, system-assigned managed identity come scelta predefinita salvo necessità concrete, niente VNet di default, niente always-ready, niente tuning aggressivo della concorrenza finché non emerge un bisogno misurato.

Tieni conto che ogni Flex Consumption plan ospita una sola Function App. Non usare proprietà o app setting deprecati per Flex Consumption quando la stessa configurazione è disponibile in `functionAppConfig`.

Quando cambi runtime o parametri piattaforma, aggiorna nello stesso change i consumer accoppiati: project file, package compatibili, Bicep/bicepparam, workflow, documentazione operativa e artefatti generati.

### 18.3 Deployment storage

Il Bicep deve:

- creare il container Blob privato che ospita i pacchetti applicativi;
- configurarlo in `functionAppConfig.deployment.storage`;
- usare managed identity per autenticare la Function App al container;
- assegnare i ruoli Storage minimi necessari;
- produrre output per nome Function App, resource ID, hostname, storage account e container di deployment;
- non includere direttamente il codice applicativo nel template Bicep.

Il codice viene pubblicato separatamente dalla pipeline mediante pacchetto `.zip` e One Deploy.

Non inserire password in file versionati. Usa parametri `@secure()`, Key Vault, OIDC e secret pipeline.

Aggiungi almeno `main.dev.bicepparam` con placeholder non sensibili e valori Flex Consumption economici per l’ambiente dev.

---

## 19. Packaging e deployment backend

Crea:

```text
scripts/package-backend.sh
scripts/package-backend.ps1
```

Gli script devono:

- eseguire `dotnet restore`, `dotnet build` e `dotnet publish` sul progetto Function App;
- usare configurazione `Release`;
- iniettare semantic version, commit SHA, build date ed environment come proprietà MSBuild;
- creare una cartella di publish pulita;
- verificare che `host.json` e gli assembly siano nella root del contenuto pubblicato;
- creare un archivio `.zip` pronto per Azure Functions;
- generare checksum SHA-256 e manifest dei metadati;
- validare che il formato dell'artefatto, i percorsi e i metadati coincidano con quanto atteso dai workflow e da One Deploy;
- produrre l’artefatto in una cartella ignorata da Git;
- non includere `local.settings.json`, file `.env`, test output o secret;
- fallire esplicitamente se il pacchetto non è valido.

Il deployment Azure deve usare **One Deploy**, direttamente o tramite `Azure/functions-action`/Azure CLI quando rilevano il piano Flex Consumption.

---

## 20. GitHub Actions

Crea workflow YAML reali in `.github/workflows/`.

### 20.1 Qualità pull request

Aggiungi un workflow PR che:

- ripristina dipendenze;
- builda backend;
- esegue test backend;
- esegue `dotnet publish` della Function App;
- valida la struttura del pacchetto Azure Functions;
- builda frontend;
- controlla TypeScript/lint;
- valida i18n;
- valida route help docs;
- valida skill registry;
- valida change fragments;
- valida Bicep;
- non esegue deploy.

### 20.2 Orchestrazione path-based su `main`

Usa un solo workflow pubblico di orchestrazione con trigger su push a `main` e filtri `paths` limitati alle tre cartelle distribuibili. Mantieni anche un `workflow_dispatch` con scelta esplicita dello scope.

Trigger indicativo:

```yaml
on:
  push:
    branches: [main]
    paths:
      - "infra/**"
      - "src/backend/**"
      - "src/frontend/**"
```

Classifica tutti i file del push prima di distribuire:

- se cambia `infra/**`, esegui soltanto il provisioning Bicep;
- se cambia `src/backend/**`, applica le migration e distribuisci soltanto il backend;
- se cambia `src/frontend/**`, distribuisci soltanto il frontend;
- per commit misti esegui prima il provisioning, quindi avvia gli scope applicativi modificati.

Non usare tag come trigger ordinario. Per ripetere o inizializzare un rilascio usa il dispatch manuale da `main` con scope `infrastructure`, `backend`, `frontend` o `all`. I cambiamenti fuori dalle tre cartelle distribuibili non devono avviare un deploy.

### 20.3 Workflow riusabili e responsabilita

Il workflow infrastrutturale esegue:

1. checkout;
2. login Azure tramite OIDC;
3. validazione e deploy dell’infrastruttura Bicep usando i file `bicepparam` versionati;
4. acquisizione degli output Bicep senza stampare secret;
5. output e summary GitHub.

Il workflow backend esegue:

1. checkout;
2. setup .NET 10;
3. calcolo versione;
4. build e test backend;
5. creazione del migration bundle e del pacchetto `.zip` versionato;
6. login Azure tramite OIDC;
7. creazione/verifica dei principal PostgreSQL e applicazione controllata delle migration;
8. grant runtime e chiusura della firewall rule temporanea;
9. One Deploy della Function App;
10. smoke check di health/version e upload artifact.

Il workflow frontend esegue checkout, setup Node, test/lint/typecheck/validatori frontend, login OIDC, build con configurazione pubblica, deploy Static Web Apps e smoke test. Non compila o distribuisce il backend.

Il workflow infrastrutturale non applica migration e non distribuisce codice. Il workflow backend non esegue Bicep; il workflow frontend non modifica backend o infrastruttura. Verifica i flag `az` usati contro la CLI reale, compila il `.bicepparam` in JSON prima di aggiungere override sensibili, mantieni simmetrici i passi create/delete temporanei e allinea nello stesso change input, vars/secrets, output, path e artifact name.

Se una modifica tocca workflow, runtime, observability, packaging o migration, considera il lavoro concluso solo dopo aver verificato anche lo stato live risultante: runtime effettivo della Function App, `health/live`, `api/version` e ingestione telemetrica attesa quando applicabile.

Ogni merge su `main` distribuisce automaticamente solo gli scope le cui cartelle sono cambiate. Le migration appartengono al workflow backend e vengono sempre applicate prima del codice dipendente; nei commit che modificano anche `infra/**`, il provisioning termina prima del backend.

I parametri della Function App relativi a memoria, scala e concorrenza devono restare definiti in Bicep e nei relativi file `.bicepparam`, non in GitHub Variables del repository. Nelle pipeline GitHub mantieni come variabile della Function soltanto il nome della Function App necessario al deploy del codice.

Usa OIDC/federated credentials per Azure. Un publish profile può essere documentato solo come fallback esplicito, non come percorso principale.

---

## 21. GitHub Secrets e Variables

Alla fine crea una sezione completa nel README con l’elenco esatto.

Distingui:

- GitHub Secrets;
- GitHub Variables;
- valori per ambiente;
- valori generati dall’infrastruttura;
- valori configurati manualmente.

Prevedi almeno, adattando i nomi alle pipeline implementate:

```text
AZURE_CLIENT_ID
AZURE_TENANT_ID
AZURE_SUBSCRIPTION_ID
AZURE_RESOURCE_GROUP
AZURE_LOCATION
AZURE_FUNCTIONAPP_NAME
AZURE_STATIC_WEB_APPS_API_TOKEN
ENTRA_FRONTEND_CLIENT_ID
ENTRA_TENANT_ID
ENTRA_BACKEND_AUDIENCE
ENTRA_API_SCOPE
ENTRA_INSTANCE
POSTGRES_ADMIN_USERNAME
POSTGRES_ADMIN_PASSWORD
```

`AZURE_FUNCTIONAPP_PUBLISH_PROFILE` può essere previsto soltanto come secret opzionale di fallback, se il workflow implementato non può usare OIDC per il deployment.

Preferisci GitHub Variables per valori non sensibili come resource group, location e Function App name; usa GitHub Secrets per credenziali e password. I parametri di memoria/scala/concorrenza della Function devono vivere nei file Bicep versionati.

Fornisci comandi `gh secret set` e `gh variable set`, ad esempio:

```bash
gh secret set AZURE_CLIENT_ID --body "<VALUE>"
gh secret set AZURE_TENANT_ID --body "<VALUE>"
gh secret set AZURE_SUBSCRIPTION_ID --body "<VALUE>"
gh secret set ENTRA_TENANT_ID --body "<VALUE>"
gh secret set POSTGRES_ADMIN_PASSWORD --body "<VALUE>"

gh variable set AZURE_RESOURCE_GROUP --body "<VALUE>"
gh variable set AZURE_LOCATION --body "<VALUE>"
gh variable set AZURE_FUNCTIONAPP_NAME --body "<VALUE>"
gh variable set ENTRA_INSTANCE --body "https://<TENANT_SUBDOMAIN>.ciamlogin.com/"
```

Non inserire valori reali.

---

## 22. Configurazioni

Predisponi:

- `appsettings.json`;
- `appsettings.Development.json`;
- `host.json`;
- `local.settings.json.example` o istruzioni equivalenti;
- `.funcignore` coerente con il packaging;
- environment variables backend;
- `.env.example` frontend;
- configuration validation all’avvio;
- connection string PostgreSQL;
- Entra ID;
- Application Insights;
- AzureWebJobsStorage identity-based connection;
- deployment storage configurato dall’infrastruttura, non dal codice;
- database;
- CORS;
- versioning;
- feature flags minime per tutorial e migration;
- configurazione localizzazione;
- configurazione skill harness;
- file per Azure Static Web Apps routing/fallback;
- configurazione locale per Azure Functions Core Tools;
- nessun `.env` o `local.settings.json` reale versionato.

Evita app setting legacy o deprecati per Flex Consumption quando la configurazione è gestita da `functionAppConfig` in Bicep.

Per stringhe di connessione, parametri provider e nomi setting usa esclusivamente i nomi canonici documentati dal provider in uso; non introdurre sinonimi o placeholder che cambiano semantica tra codice, IaC e workflow.

---

## 23. `.gitignore`

Crea un `.gitignore` completo per:

- .NET;
- Node;
- React;
- Visual Studio;
- VS Code;
- Rider;
- build;
- test output;
- coverage;
- file temporanei;
- file ambiente;
- `local.settings.json`;
- output `dotnet publish`;
- pacchetti `.zip` della Function App;
- checksum e manifest di build locali;
- Azure Functions Core Tools e cache locali;
- Azure;
- Bicep output;
- log;
- cache;
- PWA generated files quando opportuno;
- artifact locali;
- secret;
- registry skill generato solo se la strategia scelta non prevede di versionarlo.

---

## 24. README principale

Crea un `README.md` completo con:

- descrizione;
- dominio;
- funzionalità iniziali;
- stack;
- architettura;
- struttura repository;
- prerequisiti, inclusi .NET 10 SDK, Node, Azure Functions Core Tools, Azure CLI e Bicep;
- avvio backend locale con Functions Core Tools;
- avvio frontend;
- database locale;
- migration create/apply;
- test;
- publish e packaging `.zip` della Function App;
- deployment One Deploy su Flex Consumption;
- configurazione di instance memory, maximum instance count, HTTP concurrency e always-ready;
- skill harness;
- i18n;
- sincronizzazione documentazione;
- accordion help;
- tutorial;
- tema;
- PWA;
- versioning;
- patch note;
- deploy Bicep;
- workflow CI/CD;
- GitHub Secrets/Variables;
- comandi `gh`;
- configurazione Entra ID;
- configurazione Azure;
- gestione cold start e costi Flex Consumption;
- troubleshooting di startup, deployment package, storage identity e quote regionali;
- punti manuali.

---

## 25. Qualità, sicurezza e accessibilità

Applica:

- nullable reference types;
- analyzers;
- warnings ragionevoli;
- TypeScript strict;
- lint/format;
- validation delle configurazioni;
- OWASP base;
- input validation;
- output encoding;
- secret management;
- least privilege;
- dipendenze aggiornabili;
- logging senza dati sensibili;
- accessibility WCAG di base;
- keyboard navigation;
- focus management;
- reduced motion nel tutorial;
- contrasto light/dark;
- health checks;
- error handling consistente;
- documentazione delle decisioni principali.

---

## 26. Definition of Done

Una modifica è completa soltanto quando, dove applicabile:

- compila;
- passa i test;
- passa lint e validazioni;
- non introduce secret;
- aggiorna italiano e inglese;
- aggiorna help della pagina;
- aggiorna guida utente;
- aggiunge change fragment;
- aggiorna patch note in release;
- aggiorna la skill pertinente se introduce riuso;
- aggiorna `AGENTS.md` se cambia una regola strutturale;
- mantiene light/dark;
- mantiene mobile e accessibilità;
- aggiorna version/build metadata tramite pipeline;
- documenta eventuali passaggi manuali.

---

## 27. Verifiche finali da eseguire

Prima di concludere:

1. stampa la struttura repository;
2. esegui restore/build backend;
3. esegui test backend;
4. esegui `dotnet publish` della Function App;
5. crea e valida il pacchetto `.zip`, verificando `host.json` e assembly nella posizione corretta;
6. esegui install/build frontend;
7. esegui controllo TypeScript/lint;
8. valida traduzioni;
9. valida documentazione route;
10. valida skill;
11. valida change fragments;
12. valida Bicep;
13. se tocchi workflow, deploy, runtime, observability o migration, verifica anche gli artefatti generati e le configurazioni effettive risultanti;
14. esegui un avvio locale con Azure Functions Core Tools e smoke test degli endpoint, se l’ambiente lo consente;
15. segnala con precisione ciò che non è stato possibile eseguire.

Non dichiarare riuscite verifiche non realmente eseguite.

---

## 28. Output finale richiesto

Al termine fornisci un riepilogo con:

- struttura creata;
- progetti creati;
- funzionalità implementate;
- file di documentazione;
- implementazione i18n;
- tema;
- tutorial;
- help accordion;
- skill harness;
- patch note;
- comandi principali;
- risultato build/test/publish/package/validation;
- GitHub Secrets;
- GitHub Variables;
- comandi `gh secret set`;
- comandi `gh variable set`;
- configurazioni manuali Entra ID;
- configurazioni manuali Azure;
- parametri Flex Consumption adottati;
- nome e percorso dell’artefatto `.zip` backend;
- modalità One Deploy implementata;
- esito smoke test health/version;
- limiti o attività rimaste.

L’obiettivo non è produrre soltanto uno scaffold dimostrativo, ma una **base coerente, compilabile, documentata ed evolvibile** per KinHub.
