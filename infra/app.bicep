targetScope = 'resourceGroup'

@allowed(['dev', 'test', 'prod'])
param environmentName string = 'dev'
param location string = resourceGroup().location
@minLength(2)
@maxLength(16)
param namingPrefix string = 'kinhub'
@allowed(['dotnet-isolated'])
param runtimeName string = 'dotnet-isolated'
param runtimeVersion string = '10.0'
@allowed([512, 2048, 4096])
param instanceMemoryMB int = 2048
@minValue(1)
@maxValue(1000)
param maximumInstanceCount int = 20
@minValue(0)
param alwaysReadyInstanceCount int = 0
param deploymentBlobContainerName string = 'function-packages'
param entraTenantId string
param entraBackendAudience string
param entraApiScope string
param postgresAdminUsername string
@secure()
param postgresAdminPassword string
param allowedOrigins array = ['http://localhost:5173']
param enableVnetIntegration bool = false
param virtualNetworkSubnetResourceId string = ''
param enablePurgeProtection bool = false
param tags object = {
  application: 'KinHub'
  environment: environmentName
  managedBy: 'Bicep'
}

var token = take(toLower(uniqueString(subscription().id, resourceGroup().id, environmentName)), 8)
var baseName = '${namingPrefix}-${environmentName}-${token}'
var storageName = take(replace('${namingPrefix}${environmentName}${token}', '-', ''), 24)
var keyVaultName = take('${namingPrefix}-${environmentName}-${token}', 24)
var postgresName = take('${baseName}-pg', 63)
var postgresSecretName = 'postgres-connection-string'

module storage './modules/storage.bicep' = {
  name: 'storage'
  params: {
    name: storageName
    location: location
    tags: tags
    deploymentContainerName: deploymentBlobContainerName
  }
}

module observability './modules/observability.bicep' = {
  name: 'observability'
  params: {
    location: location
    logAnalyticsName: take('${baseName}-log', 63)
    applicationInsightsName: take('${baseName}-appi', 260)
    tags: tags
  }
}

module postgres './modules/postgres.bicep' = {
  name: 'postgres'
  params: {
    name: postgresName
    databaseName: 'kinhub'
    location: location
    tags: tags
    administratorLogin: postgresAdminUsername
    administratorPassword: postgresAdminPassword
  }
}

module keyVault './modules/key-vault.bicep' = {
  name: 'keyVault'
  params: {
    name: keyVaultName
    location: location
    tags: tags
    enablePurgeProtection: enablePurgeProtection
    secretName: postgresSecretName
    secretValue: 'Host=${postgres.outputs.fqdn};Port=5432;Database=${postgres.outputs.databaseName};Username=${postgresAdminUsername};Password=${postgresAdminPassword};SSL Mode=Require;Trust Server Certificate=false'
  }
}

module functionApp './modules/function-app-flex.bicep' = {
  name: 'functionApp'
  params: {
    name: take('${baseName}-func', 60)
    planName: take('${baseName}-fc', 40)
    location: location
    tags: tags
    runtimeName: runtimeName
    runtimeVersion: runtimeVersion
    instanceMemoryMB: instanceMemoryMB
    maximumInstanceCount: maximumInstanceCount
    alwaysReadyInstanceCount: alwaysReadyInstanceCount
    storageAccountName: storage.outputs.name
    storageAccountId: storage.outputs.id
    storageBlobEndpoint: storage.outputs.blobEndpoint
    storageQueueEndpoint: storage.outputs.queueEndpoint
    storageTableEndpoint: storage.outputs.tableEndpoint
    deploymentContainerName: storage.outputs.deploymentContainerName
    applicationContainerName: storage.outputs.applicationContainerName
    applicationInsightsName: observability.outputs.applicationInsightsName
    applicationInsightsConnectionString: observability.outputs.applicationInsightsConnectionString
    keyVaultName: keyVault.outputs.name
    postgresSecretName: keyVault.outputs.createdSecretName
    entraTenantId: entraTenantId
    entraBackendAudience: entraBackendAudience
    entraApiScope: entraApiScope
    environmentName: environmentName
    allowedOrigins: union(allowedOrigins, ['https://${staticWebApp.outputs.defaultHostname}'])
    enableVnetIntegration: enableVnetIntegration
    virtualNetworkSubnetResourceId: virtualNetworkSubnetResourceId
  }
}

module staticWebApp './modules/static-web-app.bicep' = {
  name: 'staticWebApp'
  params: {
    name: take('${baseName}-web', 60)
    tags: tags
  }
}

output functionAppName string = functionApp.outputs.name
output functionAppId string = functionApp.outputs.id
output functionAppHostname string = functionApp.outputs.hostname
output functionPlanId string = functionApp.outputs.planId
output storageAccountName string = storage.outputs.name
output storageAccountId string = storage.outputs.id
output deploymentContainerName string = storage.outputs.deploymentContainerName
output deploymentContainerUri string = storage.outputs.deploymentContainerUri
output staticWebAppName string = staticWebApp.outputs.name
output staticWebAppHostname string = staticWebApp.outputs.defaultHostname
output postgresServerName string = postgres.outputs.name
output keyVaultName string = keyVault.outputs.name
