# Prompt di bootstrap — KinHub

Agisci come **senior full-stack/cloud engineer, software architect e DevOps engineer**. Parti da un repository completamente vuoto e implementa realmente l’intero bootstrap dell’applicazione descritta di seguito.

Non limitarti a proporre un’architettura o a descrivere file ipotetici: **crea codice, progetti, configurazioni, documentazione, test, pipeline, infrastruttura e file operativi funzionanti**, usando placeholder espliciti solo per valori esterni e segreti.

---

## 1. Variabili del progetto

Usa queste variabili come fonte autorevole in tutto il repository:

```text
APP_NAME=KinHub
APP_DOMAIN=KinHub
APP_DOMAIN_DESCRIPTION=Piattaforma semplice e intuitiva pensata per l'utilizzo in famiglia, che raccoglie in un unico punto di accesso diversi servizi, come KinRecipe, KinList, KinDrive e altri. La schermata principale è composta esclusivamente da una home essenziale con i pulsanti dei servizi disponibili: toccando un pulsante, l'utente viene reindirizzato direttamente alla pagina del servizio corrispondente.
L'interfaccia deve privilegiare la semplicità, riducendo al minimo l'inquinamento visivo e mostrando solo gli elementi realmente necessari. L'esperienza deve risultare immediata, pulita e facilmente utilizzabile anche da smartphone. La piattaforma supporta temi grafici personalizzabili, includendo almeno i temi predefiniti Light e Dark.
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
- immagini Docker;
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
- containerizzazione;
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
- autenticazione: **Microsoft Entra ID**;
- deploy Azure: **Bicep**;
- CI/CD: **GitHub Actions**;
- backend: Docker container su Azure Web App for Containers;
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
├── .dockerignore
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
│   ├── AdvancedFrontier.Domain.Tests/
│   ├── AdvancedFrontier.Business.Tests/
│   └── AdvancedFrontier.IntegrationTests/
├── src/
│   ├── backend/
│   │   ├── domains/
│   │   ├── infrastructure/
│   │   ├── business/
│   │   └── applications/
│   └── frontend/
├── infra/
│   ├── modules/
│   ├── acr.bicep
│   ├── app.bicep
│   ├── main.dev.bicepparam
│   └── README.md
├── docker/
│   └── backend.Dockerfile
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
- comandi principali;
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

Usa namespace coerenti, ad esempio `AdvancedFrontier.*`.

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

Le migration devono poter essere applicate automaticamente all’avvio, con:

- configurazione abilitabile/disabilitabile;
- lock o strategia sicura per evitare concorrenza in ambienti multiistanza;
- logging;
- fallimento esplicito;
- documentazione;
- possibilità di applicazione manuale.

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

Crea una Web API .NET 10 contenente:

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
- Application Insights;
- correlation ID;
- avvio migration;
- configurazione ambienti;
- rate limiting base se appropriato;
- API versioning se utile, senza complessità superflua.

Endpoint minimi consigliati:

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
<ENTRA_FRONTEND_CLIENT_ID>
<ENTRA_BACKEND_CLIENT_ID_OR_AUDIENCE>
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
- immagine Docker taggata con versione e SHA;
- integrazione GitHub Actions;
- About/Version;
- release notes;
- notifica nuova versione PWA;
- nessun incremento manuale duplicato in più file.

---

## 18. Infrastruttura Azure con Bicep

Dentro `infra/` crea file modulari e parametrizzabili.

### Parte 1 — Container Registry

`infra/acr.bicep` deve creare:

- Azure Container Registry;
- output utili;
- naming parametrico;
- ambiente;
- tag;
- configurazione coerente con pipeline.

Questa parte deve poter essere eseguita prima del build/push dell’immagine.

### Parte 2 — Infrastruttura applicativa

`infra/app.bicep` deve creare almeno:

- Azure App Service Plan Linux B1;
- Azure Web App for Containers;
- collegamento ad ACR;
- Azure Database for PostgreSQL Flexible Server;
- database applicativo;
- Storage Account;
- Application Insights;
- Log Analytics Workspace, se necessario;
- Key Vault;
- Azure Static Web Apps;
- managed identity;
- RBAC o access policy appropriate;
- app settings;
- Key Vault references;
- configurazione health check;
- configurazione container;
- output utili.

Usa parametri per:

- `environmentName`;
- location;
- naming prefix;
- SKU;
- immagine/tag;
- Entra ID;
- database;
- allowed origins;
- configurazioni non segrete.

Non inserire password in file versionati. Usa parametri sicuri, Key Vault o secret pipeline.

Aggiungi almeno `main.dev.bicepparam` con placeholder non sensibili.

---

## 19. Docker

Crea:

```text
docker/backend.Dockerfile
.dockerignore
```

Il Dockerfile deve:

- usare multi-stage build;
- fare restore/build/publish;
- eseguire come utente non root quando possibile;
- esporre la porta corretta;
- includere health check se appropriato;
- essere compatibile con Azure Web App for Containers;
- ricevere version/build metadata come build args;
- produrre immagine minimale;
- non contenere secret.

---

## 20. GitHub Actions

Crea workflow YAML reali in `.github/workflows/`.

### 20.1 Qualità pull request

Aggiungi un workflow PR che:

- ripristina dipendenze;
- builda backend;
- esegue test backend;
- builda frontend;
- controlla TypeScript/lint;
- valida i18n;
- valida route help docs;
- valida skill registry;
- valida change fragments;
- valida Bicep;
- non esegue deploy.

### 20.2 Deploy infrastruttura completo tramite tag

Trigger esempio:

```yaml
on:
  push:
    tags:
      - "infra-*"
```

Passaggi:

1. checkout;
2. calcolo versione/build metadata;
3. login Azure;
4. deploy ACR;
5. login ACR;
6. build backend container;
7. push con tag versione e SHA;
8. deploy infrastruttura applicativa;
9. build frontend;
10. generazione docs/patch note/version metadata;
11. deploy Azure Static Web Apps;
12. output e summary GitHub;
13. nessun secret stampato.

### 20.3 Deploy solo codice su `main`

Trigger push su `main`.

Passaggi:

1. checkout;
2. calcolo versione;
3. build backend;
4. test backend;
5. validazioni skill/i18n/docs/change fragments;
6. build Docker;
7. push ACR;
8. aggiornamento Web App con nuova immagine;
9. build frontend;
10. generazione metadata e release notes;
11. deploy Static Web Apps;
12. smoke check di health/version;
13. summary.

L’infrastruttura completa si modifica solo tramite il workflow tag; `main` pubblica solo codice e configurazioni applicative previste.

Usa OIDC/federated credentials per Azure quando possibile, evitando client secret statici. Documenta entrambi solo se necessario, privilegiando OIDC.

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
AZURE_ACR_NAME
AZURE_WEBAPP_NAME
AZURE_STATIC_WEB_APPS_API_TOKEN
ENTRA_FRONTEND_CLIENT_ID
ENTRA_BACKEND_AUDIENCE
ENTRA_API_SCOPE
POSTGRES_ADMIN_USERNAME
POSTGRES_ADMIN_PASSWORD
```

Preferisci GitHub Variables per valori non sensibili.

Fornisci comandi `gh secret set` e `gh variable set`, ad esempio:

```bash
gh secret set AZURE_CLIENT_ID --body "<VALUE>"
gh secret set AZURE_TENANT_ID --body "<VALUE>"
gh secret set AZURE_SUBSCRIPTION_ID --body "<VALUE>"
```

Non inserire valori reali.

---

## 22. Configurazioni

Predisponi:

- `appsettings.json`;
- `appsettings.Development.json`;
- environment variables backend;
- `.env.example` frontend;
- configuration validation all’avvio;
- connection string;
- Entra ID;
- Application Insights;
- storage;
- database;
- CORS;
- versioning;
- feature flags minime per tutorial e migration;
- configurazione localizzazione;
- configurazione skill harness;
- file per Azure Static Web Apps routing/fallback;
- nessun `.env` reale versionato.

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
- Docker;
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
- prerequisiti;
- avvio backend;
- avvio frontend;
- database locale;
- migration create/apply;
- test;
- Docker;
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
- troubleshooting;
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
4. esegui install/build frontend;
5. esegui controllo TypeScript/lint;
6. valida traduzioni;
7. valida documentazione route;
8. valida skill;
9. valida change fragments;
10. valida Bicep;
11. builda Docker, se l’ambiente lo consente;
12. segnala con precisione ciò che non è stato possibile eseguire.

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
- risultato build/test/validation;
- GitHub Secrets;
- GitHub Variables;
- comandi `gh secret set`;
- comandi `gh variable set`;
- configurazioni manuali Entra ID;
- configurazioni manuali Azure;
- limiti o attività rimaste.

L’obiettivo non è produrre soltanto uno scaffold dimostrativo, ma una **base coerente, compilabile, documentata ed evolvibile** per KinHub.
