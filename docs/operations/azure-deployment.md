# Deployment Azure

Il push su `main` avvia `deploy.yml` soltanto per modifiche a `infra/**`, `src/backend/**` o `src/frontend/**`. L'orchestratore rileva tutti gli scope presenti nel push e richiama workflow riusabili con responsabilita separate:

- `deploy-infrastructure.yml` valida e applica Bicep, senza migration o deploy applicativi;
- `deploy-backend.yml` compila e testa il backend, applica migration e grant PostgreSQL, pubblica la Function App e verifica health/version;
- `deploy-frontend.yml` valida e compila la SPA, pubblica Static Web Apps e verifica il sito.

Uno scope non modificato non viene distribuito. Nei commit misti il provisioning termina prima di backend e frontend, che possono poi procedere in parallelo. Il dispatch manuale da `main` consente di eseguire `infrastructure`, `backend`, `frontend` oppure `all`; quest'ultimo mantiene lo stesso ordine.

Il backend viene pubblicato in ZIP con `host.json` e assembly nella root, checksum SHA-256 e manifest. `Azure/functions-action` rileva Flex Consumption e usa One Deploy sul container configurato da `functionAppConfig.deployment.storage`.

OIDC e obbligatorio come percorso primario. La federated credential GitHub deve autorizzare repository, branch/environment e workflow necessari. L'identita della pipeline richiede Contributor sul resource group, User Access Administrator (o custom equivalente) per creare role assignment durante il provisioning e deve essere configurata come Microsoft Entra administrator del server PostgreSQL usato dal workflow backend.

## Migration backend

Le migration non usano una connection string segreta e vengono applicate a ogni deploy backend prima di One Deploy. Il workflow:

- recupera host e database dalle impostazioni della Function App quando non riceve output da un provisioning nello stesso run;
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
- backend senza risorse: eseguire prima lo scope `infrastructure` e valorizzare `AZURE_FUNCTIONAPP_NAME`.
- frontend senza provisioning nello stesso run: valorizzare `AZURE_FUNCTIONAPP_URL` e `AZURE_STATIC_WEB_APP_URL` con gli URL ottenuti dagli output Bicep, evitando che il deploy dipenda dalla discovery ARM degli hostname.
