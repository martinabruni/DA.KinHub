using './managed-identities.bicep'

// Copy to a real .bicepparam file and replace every placeholder with environment-specific values.

param location = 'westeurope'

param identityContainerAppName = 'kinhub-identity-api-dev'
param kinRecipeContainerAppName = 'kinhub-kinrecipe-api-dev'
param kinListContainerAppName = 'kinhub-kinlist-api-dev'
param kinListAudioWorkerContainerAppName = 'kinhub-kinlist-audio-worker-dev'
param kinListMigrationJobName = 'kinhub-kinlist-migrations-dev'
