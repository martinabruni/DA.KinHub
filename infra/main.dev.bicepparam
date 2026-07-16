using './app.bicep'

param environmentName = 'dev'
param location = 'italynorth'
param namingPrefix = 'kinhub'
param runtimeName = 'dotnet-isolated'
param runtimeVersion = '10.0'
param instanceMemoryMB = 2048
param maximumInstanceCount = 20
param alwaysReadyInstanceCount = 0
param deploymentBlobContainerName = 'function-packages'
param entraTenantId = '<ENTRA_TENANT_ID>'
param entraBackendAudience = '<ENTRA_BACKEND_CLIENT_ID_OR_AUDIENCE>'
param entraApiScope = '<ENTRA_API_SCOPE>'
param postgresAdminUsername = '<POSTGRES_ADMIN_USERNAME>'
param postgresAdminPassword = '<POSTGRES_ADMIN_PASSWORD>'
param allowedOrigins = [
  'http://localhost:5173'
]
param enableVnetIntegration = false
param virtualNetworkSubnetResourceId = ''
param enablePurgeProtection = false
