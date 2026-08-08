# Deployment Azure

Il push su `main` avvia `infrastructure.yml` per `infra/**` e `release.yml` per il codice applicativo. Se lo stesso commit modifica anche `infra/**` o `infrastructure.yml`, il primo job di `release.yml` attende il run `Infrastructure` relativo allo stesso SHA prima di leggere output ARM, applicare migration o distribuire applicazioni. Il workflow `ci.yml` gira sulle pull request senza credenziali Azure:

- `infrastructure.yml` valida, esegue what-if e applica Bicep in modalità incremental;
- `release.yml` ricompila il commit unito, applica migration, pubblica Function e Static Web Apps e verifica health/version;
- `ci.yml` valida build, test, package, frontend, documentazione, skill e Bicep.

Il provisioning e la release sono serializzati con concurrency `cancel-in-progress: false`. Il gate usa soltanto il `GITHUB_TOKEN` con permesso `actions: read`: attende gli stati `queued` e `in_progress`, procede solo con `success` e blocca `failure`, `cancelled`, `timed_out` o assenza di completamento entro 30 minuti. Il dispatch manuale consente di riallineare l'infrastruttura o ripetere una release trusted senza attivare questo gate push.

Il backend viene pubblicato in ZIP con `host.json` e assembly nella root, checksum SHA-256 e manifest. `Azure/functions-action` riceve il path ZIP esatto prodotto dal packaging, rileva Flex Consumption e usa One Deploy sul container configurato da `functionAppConfig.deployment.storage`. Dopo il login OIDC, `release.yml` recupera il token Static Web Apps con `az staticwebapp secrets list` e lo maschera prima del deploy, senza richiedere un secret GitHub separato.

Lo smoke test post-release segue gli URL prodotti dal deployment ARM, segue i redirect e ritenta soltanto errori di rete, timeout e status transitori entro un limite definito. La health Function riceve un access token temporaneo ottenuto dal login OIDC perche l'autenticazione front-door della Function App puo proteggere anche le route applicativamente anonime; il token non viene scritto nei log. Root Static Web App e `/api/version` sono verificati separatamente e ogni status diverso da `200` blocca la release con una diagnostica limitata al nome logico, status e numero di tentativi.

OIDC e obbligatorio come percorso primario. La federated credential GitHub deve autorizzare repository, branch/environment e workflow necessari. L'identita della pipeline richiede Contributor sul resource group, User Access Administrator (o custom equivalente) per creare role assignment durante il provisioning e deve essere configurata come Microsoft Entra administrator del server PostgreSQL usato dal workflow backend.

## Migration backend

Le migration non usano una connection string segreta e vengono applicate a ogni deploy backend prima di One Deploy. Il workflow:

- recupera nomi e hostname dagli output del deployment ARM stabile `kinhub-dev-infrastructure`;
- risolve il principal Entra della pipeline e la managed identity della Function App;
- apre una firewall rule temporanea per il runner quando il server e pubblico;
- crea o verifica i principal database `kinhub_app` e `kinhub_migrator`;
- esegue il bundle EF con un token `az account get-access-token --resource-type oss-rdbms`;
- applica i grant runtime sugli schemi `shared` e `kinlist` e chiude sempre la firewall rule temporanea;
- distribuisce il backend soltanto dopo il completamento delle migration.

## Diagnostica

- 403 sul deployment storage: verificare ruolo Storage Blob Data Owner e propagazione RBAC.
- package non avviabile: verificare `host.json` e `DA.KinHub.Functions.dll` nella root ZIP.
- readiness 503: controllare PostgreSQL, bootstrap KinHub, settings database, principal Entra e migration.
- provisioning Flex negato: eseguire `az functionapp list-flexconsumption-locations` e verificare quota regionale.
- backend senza risorse: eseguire prima `infrastructure.yml` e verificare il deployment ARM stabile.
- frontend senza collegamento `/api`: verificare il resource `staticSites/linkedBackends/api` e gli output del deployment.
