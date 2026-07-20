# Deployment Azure

L'infrastruttura completa viene aggiornata soltanto con tag `infra-*`. Il push su `main` pubblica codice, migration applicative previste, metadata e frontend senza cambiare memoria o scala della Function App.

Il backend viene pubblicato in ZIP con `host.json` e assembly nella root, checksum SHA-256 e manifest. `Azure/functions-action` rileva Flex Consumption e usa One Deploy sul container configurato da `functionAppConfig.deployment.storage`.

OIDC è obbligatorio come percorso primario. La federated credential GitHub deve autorizzare repository, branch/environment e workflow necessari. L'identità della pipeline richiede Contributor sul resource group e User Access Administrator (o custom equivalente) per creare role assignment durante il workflow infrastrutturale.

## Diagnostica

- 403 sul deployment storage: verificare ruolo Storage Blob Data Owner e propagazione RBAC.
- package non avviabile: verificare `host.json` e `AdvancedFrontier.Functions.dll` nella root ZIP.
- readiness 503: controllare PostgreSQL, bootstrap KinList, settings database e migration.
- provisioning Flex negato: eseguire `az functionapp list-flexconsumption-locations` e verificare quota regionale.
