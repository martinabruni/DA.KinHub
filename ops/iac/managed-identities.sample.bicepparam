using './managed-identities.bicep'

// Copy to a real .bicepparam file and replace every placeholder with environment-specific values.

param location = 'westeurope'

param identityContainerAppName = 'kinhub-identity-api-dev'
param functionAppName = 'kinhub-func-dev'
