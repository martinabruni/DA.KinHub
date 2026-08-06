# **Piano Aggiornato**

**Piano Aggiornato**
**Decisioni Acquisite**
| Elemento | Valore |
|---|---|
| Subscription dev | `a148a62f-0509-4dd5-a61f-0043b182d5f1` |
| Resource group esistente | `rg-kinhub-dev` |
| Daily cap Log Analytics | `1 GB/giorno` |
| Tag `workload` | `kinhub` |
| Tag `environment` | `dev` |
| Tag `owner` proposto | `martinabruni` |
| Tag `costClassification` | `personal-low-cost` |
Il tag `owner` è un semplice metadato Azure usato per indicare chi è responsabile della risorsa. Non assegna permessi e non comporta costi. Userò `martinabruni`, coerente con il proprietario del repository.
La subscription indicata non è accessibile dalla sessione Azure CLI corrente. Questo non blocca le modifiche repository, ma impedisce l’inventario live e il `what-if` locale finché non saranno disponibili credenziali per quella subscription. La pipeline OIDC dovrà avere accesso alla subscription indicata.
**Gestione Risorse Esistenti**
I nomi generati dal precedente IaC sono ora nomi esplicitamente confermati e quindi validi:
| Risorsa | Nome adottato |
|---|---|
| Key Vault | `kinhub-dev-lauj4ewc` |
| Application Insights | `kinhub-dev-lauj4ewc-appi` |
| Function App | `kinhub-dev-lauj4ewc-func` |
| Flex plan | `kinhub-dev-lauj4ewc-fc` |
| Log Analytics | `kinhub-dev-lauj4ewc-log` |
| PostgreSQL | `kinhub-dev-lauj4ewc-pg` |
| Static Web App | `kinhub-dev-lauj4ewc-web` |
| Storage Account | `kinhubdevlauj4ewc` |
Bicep adotterà e gestirà le risorse esistenti tramite questi nomi. Non saranno dichiarate `existing` quando devono continuare a essere amministrate da Bicep. Il primo `what-if` dovrà confermare aggiornamenti in-place e bloccare qualsiasi sostituzione o cancellazione.

1. **Ristrutturazione Bicep**
   - Sostituire `infra/app.bicep` con `infra/main.bicep`.
   - Sostituire `infra/main.dev.bicepparam` con `infra/environments/dev.bicepparam`.
   - Consolidare i moduli precedenti in `monitoring.bicep`, `data-security.bicep`, `functions.bicep` e `static-web-app.bicep`.
   - Eliminare definitivamente `uniqueString`, `namingPrefix` e qualsiasi generazione automatica dei nomi.
   - Inserire i nomi esistenti come configurazione esplicita di `dev`.
   - Mantenere deployment a scope resource group e modalità `incremental`.
   - Non creare o modificare il resource group da Bicep.
2. **Adozione Sicura Delle Risorse**
   - Eseguire un inventario live nella subscription target prima del primo deployment.
   - Confrontare tipo, regione, SKU, identity e proprietà reali con il nuovo Bicep.
   - Mantenere eventuali proprietà irreversibili già abilitate, come Key Vault purge protection.
   - Non riabilitare Shared Key Storage se risulta già disabilitata e gli accessi identity-based funzionano.
   - Non modificare SKU o proprietà che richiederebbero una sostituzione automatica.
   - Fare fallire la pipeline se il what-if rileva `Delete`, sostituzioni o cambiamenti distruttivi.
3. **Configurazione Azure**
   - Mantenere tutte le risorse regionali in `italynorth`.
   - Mantenere Static Web Apps in `westeurope`.
   - Aggiornare Static Web Apps a `Standard`, se ancora `Free`, solo dopo conferma what-if di aggiornamento in-place.
   - Creare il collegamento Bicep `staticSites/linkedBackends` verso la Function App.
   - Usare `/api` come percorso applicativo frontend-backend.
   - Configurare Flex `FC1`, Linux, .NET 10 isolated, 2.048 MB, massimo 20 istanze, zero always-ready e strategia `Recreate`.
   - Conservare system-assigned managed identity e `functionAppConfig`.
   - Mantenere Storage `Standard_LRS`, TLS 1.2, container privati e accesso anonimo disabilitato.
   - Mantenere PostgreSQL senza HA, senza geo-redundancy e senza backup applicativi aggiuntivi.
   - Configurare Log Analytics con retention 30 giorni e daily cap `1 GB`.
   - Applicare tag e RBAC least privilege alle risorse adottate.
   - Non esporre valori sensibili negli output.
4. **Sostituzione Workflow**
   - Eliminare i cinque workflow correnti:
     - `pr-quality.yml`
     - `deploy.yml`
     - `deploy-infrastructure.yml`
     - `deploy-backend.yml`
     - `deploy-frontend.yml`
   - Creare esclusivamente:
     - `ci.yml`
     - `infrastructure.yml`
     - `release.yml`
   - Fissare ogni action esterna a uno SHA completo.
   - Aggiungere `.github/CODEOWNERS` con `@martinabruni` per `.github/workflows/**` e `infra/**`.
   - Non usare workflow riusabili, matrix o orchestrazione per scope.
5. **`ci.yml`**
   - Eseguire su tutte le pull request senza secret o credenziali Azure.
   - Eseguire build, test, package backend e verifica migration bundle.
   - Eseguire test, lint, typecheck, build e validatori frontend.
   - Eseguire validazioni skill, documentazione, route, release e i18n.
   - Eseguire Bicep format, build, lint e compilazione dei parametri.
   - Validare i workflow con actionlint.
   - Conservare test report temporanei per massimo 7 giorni.
   - Non produrre artefatti autorizzati al deployment.
6. **`infrastructure.yml`**
   - Eseguire da `main` fidato per modifiche a `infra/**` o al workflow stesso.
   - Consentire il dispatch manuale per il primo riallineamento.
   - Usare environment `dev`, OIDC e subscription confermata.
   - Usare concurrency dedicata con `cancel-in-progress: false`.
   - Eseguire Azure validation e what-if immediatamente prima del deploy.
   - Conservare il what-if non sensibile come artifact temporaneo.
   - Bloccare delete, sostituzioni e modifiche distruttive a PostgreSQL o rete.
   - Applicare Bicep in modalità `incremental`.
   - Verificare SKU, regioni, Flex runtime, identity, Storage LRS e collegamento `/api`.
   - Usare un nome stabile per il deployment ARM, così `release.yml` può recuperarne gli output.
7. **`release.yml`**
   - Ricompilare il commit già unito a `main`.
   - Creare una sola volta ZIP Function, migration bundle, frontend `dist` e metadata.
   - Modificare gli script di packaging per consentire package/publish senza una seconda compilazione.
   - Conservare gli artefatti per 30 giorni.
   - Verificare checksum e SHA prima del deployment.
   - Recuperare nomi e hostname dagli output dell’ultimo deployment ARM, senza discovery euristica o Variables duplicate.
   - Applicare le migration prima di One Deploy.
   - Aprire una regola firewall PostgreSQL limitata all’IP del runner e chiuderla sempre.
   - Creare o verificare principal Entra e grant runtime.
   - Distribuire la Function con One Deploy e remote build disabilitata.
   - Distribuire il frontend già compilato con `skip_app_build`.
   - Verificare Function diretta, readiness, Static Web App e `/api/version` attraverso Static Web Apps.
   - Verificare la telemetria con retry limitato.
   - Usare concurrency `dev` senza cancellare deployment iniziati.
8. **Configurazione GitHub**
   - Mantenere nell’environment `dev`:
     - `AZURE_CLIENT_ID`
     - `AZURE_TENANT_ID`
     - `AZURE_SUBSCRIPTION_ID`
     - `AZURE_STATIC_WEB_APPS_API_TOKEN`
     - configurazione Entra External ID
     - eventuali credenziali PostgreSQL ancora necessarie al provisioning
   - Impostare `AZURE_SUBSCRIPTION_ID` alla subscription confermata.
   - Mantenere `AZURE_RESOURCE_GROUP=rg-kinhub-dev` come Variable esplicita.
   - Rimuovere Variables obsolete per URL, hostname e discovery delle risorse.
   - Documentare che il bootstrap OIDC, environment e token Static Web Apps è già una responsabilità manuale e non viene ripetuto a ogni deploy.
9. **Harness**
   - Aggiungere una skill `infrastructure` con `infra-guidelines.md` come reference autorevole.
   - Riscrivere la skill `implementation` eliminando l’orchestrazione path-based precedente.
   - Rimuovere dal changelog della skill i riferimenti alle pipeline sostituite.
   - Aggiornare `tools/skill-harness/README.md`.
   - Estendere la validazione con controlli su workflow ammessi, SHA delle action, `pull_request_target`, `uniqueString`, SKU Static Web Apps, what-if, modalità incremental e concurrency.
   - Rigenerare `.agents/skills/registry.json`.
10. **Prompt E Documentazione**

- Riscrivere le sezioni infrastruttura, pipeline, secret, bootstrap e verifiche di `docs/bootstrap.prompt.md`.
- Rimuovere ogni riferimento ai workflow precedenti, ai deploy separati per cartella e alla discovery degli hostname.
- Mantenere lo stile imperativo e didattico del prompt.
- Aggiornare `AGENTS.md`, `README.md`, `infra/README.md`, `.azure/deployment-plan.md` e i documenti operativi.
- Aggiornare repository-wide i riferimenti ai file Bicep e workflow rinominati.
- Conservare patch note e fragment storici come evidenza dei rilasci passati.
- Aggiungere un nuovo change fragment bilingue.

11. **Verifica Finale**

- Eseguire tutti i validatori repository.
- Eseguire build, test, migration bundle, publish e packaging backend.
- Eseguire test, lint, typecheck e build frontend.
- Eseguire Bicep format/build/build-params.
- Eseguire actionlint.
- Eseguire Azure validation e what-if tramite credenziali OIDC della subscription target.
- Applicare il deployment solo se il what-if dimostra che le risorse esistenti vengono aggiornate senza ricreazione.
- Verificare runtime ARM, health, `/api/version`, collegamento Static Web Apps e telemetria.
