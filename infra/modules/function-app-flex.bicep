param name string
param planName string
param location string
param tags object = {}
param runtimeName string = 'dotnet-isolated'
param runtimeVersion string = '10'
@allowed([512, 2048, 4096])
param instanceMemoryMB int = 2048
@minValue(1)
@maxValue(1000)
param maximumInstanceCount int = 20
@minValue(0)
param alwaysReadyInstanceCount int = 0
param storageAccountName string
param storageAccountId string
param storageBlobEndpoint string
param storageQueueEndpoint string
param storageTableEndpoint string
param deploymentContainerName string
param applicationContainerName string
param applicationInsightsName string
param applicationInsightsConnectionString string
param entraTenantId string
param entraBackendAudience string
param entraApiScope string
param environmentName string
param postgresHost string
param postgresDatabaseName string
param postgresRuntimeUsername string = 'kinhub_app'
param allowedOrigins array = []
param enableVnetIntegration bool = false
param virtualNetworkSubnetResourceId string = ''

var storageBlobDataOwnerRoleId = 'b7e6dc6d-f1e8-4753-8033-0f276bb0955b'
var storageQueueDataContributorRoleId = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
var storageTableDataContributorRoleId = '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
var monitoringMetricsPublisherRoleId = '3913510d-42f4-4e42-8a64-420c390055eb'

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: planName
  location: location
  tags: tags
  kind: 'functionapp'
  sku: { name: 'FC1', tier: 'FlexConsumption' }
  properties: { reserved: true }
}

resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: name
  location: location
  tags: union(tags, { 'azd-service-name': 'api' })
  kind: 'functionapp,linux'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: false
    publicNetworkAccess: 'Enabled'
    virtualNetworkSubnetId: enableVnetIntegration ? virtualNetworkSubnetResourceId : null
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storageBlobEndpoint}${deploymentContainerName}'
          authentication: { type: 'SystemAssignedIdentity' }
        }
      }
      scaleAndConcurrency: {
        instanceMemoryMB: instanceMemoryMB
        maximumInstanceCount: maximumInstanceCount
        alwaysReady: alwaysReadyInstanceCount > 0 ? [
          { name: 'http', instanceCount: alwaysReadyInstanceCount }
        ] : []
      }
      runtime: { name: runtimeName, version: runtimeVersion }
    }
    siteConfig: {
      alwaysOn: false
      ftpsState: 'Disabled'
      http20Enabled: true
      minTlsVersion: '1.2'
      cors: {
        allowedOrigins: allowedOrigins
        supportCredentials: false
      }
      appSettings: [
        { name: 'FUNCTIONS_EXTENSION_VERSION', value: '~4' }
        { name: 'AzureWebJobsStorage__accountName', value: storageAccountName }
        { name: 'AzureWebJobsStorage__credential', value: 'managedidentity' }
        { name: 'AzureWebJobsStorage__blobServiceUri', value: storageBlobEndpoint }
        { name: 'AzureWebJobsStorage__queueServiceUri', value: storageQueueEndpoint }
        { name: 'AzureWebJobsStorage__tableServiceUri', value: storageTableEndpoint }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: applicationInsightsConnectionString }
        { name: 'APPLICATIONINSIGHTS_AUTHENTICATION_STRING', value: 'Authorization=AAD' }
        { name: 'KinHub__AppName', value: 'KinHub' }
        { name: 'KinHub__Environment', value: environmentName }
        { name: 'KinHub__ApiVersion', value: '1.0' }
        { name: 'Entra__Enabled', value: 'true' }
        { name: 'Entra__Instance', value: environment().authentication.loginEndpoint }
        { name: 'Entra__TenantId', value: entraTenantId }
        { name: 'Entra__Audience', value: entraBackendAudience }
        { name: 'Entra__Scope', value: entraApiScope }
        { name: 'Database__Mode', value: 'ManagedIdentity' }
        { name: 'Database__Host', value: postgresHost }
        { name: 'Database__Port', value: '5432' }
        { name: 'Database__DatabaseName', value: postgresDatabaseName }
        { name: 'Database__Username', value: postgresRuntimeUsername }
        { name: 'Database__RequireSsl', value: 'true' }
        { name: 'Database__ApplyMigrationsOnStartup', value: 'false' }
        { name: 'Storage__AccountUri', value: storageBlobEndpoint }
        { name: 'Storage__ContainerName', value: applicationContainerName }
      ]
    }
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' existing = { name: storageAccountName }
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = { name: applicationInsightsName }

resource storageRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountId, functionApp.id, storageBlobDataOwnerRoleId)
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataOwnerRoleId)
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource storageQueueRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountId, functionApp.id, storageQueueDataContributorRoleId)
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageQueueDataContributorRoleId)
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource storageTableRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountId, functionApp.id, storageTableDataContributorRoleId)
  scope: storage
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageTableDataContributorRoleId)
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

resource monitoringRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(applicationInsights.id, functionApp.id, monitoringMetricsPublisherRoleId)
  scope: applicationInsights
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', monitoringMetricsPublisherRoleId)
    principalId: functionApp.identity.principalId
    principalType: 'ServicePrincipal'
  }
}

output id string = functionApp.id
output name string = functionApp.name
output hostname string = functionApp.properties.defaultHostName
output principalId string = functionApp.identity.principalId
output planId string = plan.id
