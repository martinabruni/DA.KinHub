# Deployment Azure

Il push su `main` avvia `deploy.yml` soltanto quando cambiano input distribuibili. L'orchestratore sceglie un solo percorso:

- modifiche applicative: `deploy-code.yml` pubblica backend, metadata e frontend senza Bicep o migration;
- modifiche in `infra/`, alle migration EF o al workflow infrastrutturale: `deploy-infrastructure.yml` applica Bicep, principal, migration e grant, poi richiama lo stesso deploy applicativo;
- commit misti: prevale il percorso infrastrutturale full-stack.

La concurrency dell'ambiente `dev` non interrompe il run attivo e puo coalescere piu pending senza garantirne l'ordine. Quando un run parte, fallisce se commit successivi su `main` contengono input distribuibili, mentre puo proseguire se HEAD differisce soltanto per path esclusi dal deploy. Finche non esiste un deploy riuscito usa sempre il percorso full-stack, poi confronta il proprio SHA con l'ultimo baseline riuscito e include anche modifiche infrastrutturali o migration appartenute a run falliti o sostituiti. Non rieseguire un vecchio run: avvia un nuovo dispatch manuale da `main`, lasciando lo scope `auto` salvo una scelta intenzionale. Anche una scelta `application` viene promossa a full-stack se il diff accumulato lo richiede. Il dispatch rimane disponibile per riallineamenti o drift che non producono modifiche repository; i tag non attivano il deploy.

Il backend viene pubblicato in ZIP con `host.json` e assembly nella root, checksum SHA-256 e manifest. `Azure/functions-action` rileva Flex Consumption e usa One Deploy sul container configurato da `functionAppConfig.deployment.storage`.

OIDC è obbligatorio come percorso primario. La federated credential GitHub deve autorizzare repository, branch/environment e workflow necessari. L'identità della pipeline richiede Contributor sul resource group, User Access Administrator (o custom equivalente) per creare role assignment durante il workflow infrastrutturale e deve essere configurata come Microsoft Entra administrator del server PostgreSQL usato dal workflow.

Il percorso full-stack usa l'environment `dev` sia per il job infrastruttura/database sia per il successivo job applicativo riusabile. Se l'environment ha reviewer o wait timer, entrambe le fasi devono superare le protection rule; la seconda approvazione avviene dopo la migration e non deve essere lasciata in sospeso.

Le migration non usano piu una connection string segreta e vengono eseguite soltanto dal percorso full-stack prima del codice. Il workflow infrastrutturale:

- risolvono il principal Entra della pipeline;
- aprono una firewall rule temporanea per il runner quando il server e pubblico;
- creano o verificano i principal database `kinhub_app` e `kinhub_migrator`;
- eseguono il bundle EF con un token `az account get-access-token --resource-type oss-rdbms`;
- applica grant runtime sugli schemi `shared` e `kinlist` e chiude la firewall rule temporanea.

## Diagnostica

- 403 sul deployment storage: verificare ruolo Storage Blob Data Owner e propagazione RBAC.
- package non avviabile: verificare `host.json` e `DA.KinHub.Functions.dll` nella root ZIP.
- readiness 503: controllare PostgreSQL, bootstrap KinHub, settings database, principal Entra e migration.
- provisioning Flex negato: eseguire `az functionapp list-flexconsumption-locations` e verificare quota regionale.
