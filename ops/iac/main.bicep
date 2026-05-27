targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Azure region for the Static Web App. Defaults to the main deployment location.')
param staticWebAppLocation string = location

@description('Static Web App name.')
param staticWebAppName string

@description('Web App name.')
param webAppName string

@description('App Service plan name for the Web App.')
param appServicePlanName string = '${webAppName}-plan'

@description('Application Insights name.')
param applicationInsightsName string = '${webAppName}-appi'

@description('Key Vault name.')
param keyVaultName string

@description('PostgreSQL Flexible Server name.')
param postgresServerName string

@description('PostgreSQL Database name. Must be 1-63 lowercase alphanumerics and hyphens only. Cannot start or end with hyphens.')
@minLength(1)
@maxLength(63)
param postgresDatabaseName string

@description('PostgreSQL administrator login.')
param postgresAdministratorLogin string

@description('PostgreSQL administrator password.')
@secure()
param postgresAdministratorPassword string

@description('Azure OpenAI account name.')
param openAiAccountName string

@description('Azure OpenAI SKU name.')
param openAiSkuName string = 'S0'

@description('JWT secret key.')
@secure()
param jwtSecret string

@description('JWT issuer.')
param jwtIssuer string = 'kinhub'

@description('JWT access token expiry in minutes.')
param jwtAccessTokenExpiryMinutes string = '15'

var postgresConnectionString = 'Server=${postgresServer.properties.fullyQualifiedDomainName};Database=${postgresDatabaseName};Port=5432;User Id=${postgresAdministratorLogin};Password=${postgresAdministratorPassword};Ssl Mode=Require;'
var sqlConnectionStringSecretName = 'database-connection-string'
var jwtSecretSecretName = 'jwt-secret'
var openAiEndpointSecretName = 'openai-endpoint'
var openAiKeySecretName = 'openai-key'
var keyVaultSecretsUserRoleDefinitionId = subscriptionResourceId(
  'Microsoft.Authorization/roleDefinitions',
  '4633458b-17de-408a-b874-0445c86b69e6'
)
var webAppSettings = [
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: applicationInsights.properties.ConnectionString
  }
  {
    name: 'ConnectionStrings__KinHub'
    value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${sqlConnectionStringSecretName})'
  }
  {
    name: 'Jwt__Secret'
    value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${jwtSecretSecretName})'
  }
  {
    name: 'Jwt__Issuer'
    value: jwtIssuer
  }
  {
    name: 'Jwt__AccessTokenExpiryMinutes'
    value: jwtAccessTokenExpiryMinutes
  }
  {
    name: 'OpenAi__Endpoint'
    value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${openAiEndpointSecretName})'
  }
  {
    name: 'OpenAi__ApiKey'
    value: '@Microsoft.KeyVault(VaultName=${keyVault.name};SecretName=${openAiKeySecretName})'
  }
  {
    name: 'OpenAi__EmbeddingDeploymentName'
    value: embeddingDeployment.name
  }
  {
    name: 'OpenAi__ModelDeploymentName'
    value: gpt4oDeployment.name
  }
  {
    name: 'Mcp__AuthorizationServerUrl'
    value: 'https://${webAppName}.azurewebsites.net'
  }
  {
    name: 'Cors__AllowAnyOrigin'
    value: 'false'
  }
  {
    name: 'Cors__AllowedOrigins__0'
    value: 'https://${staticWebApp.properties.defaultHostname}'
  }
  {
    name: 'Mcp__AllowAnyOrigin'
    value: 'false'
  }
  {
    name: 'Mcp__AllowedOrigins__0'
    value: 'https://${staticWebApp.properties.defaultHostname}'
  }
]

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: null
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    enablePurgeProtection: true
    enableRbacAuthorization: true
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    publicNetworkAccess: 'Enabled'
    sku: {
      family: 'A'
      name: 'standard'
    }
    softDeleteRetentionInDays: 90
    tenantId: tenant().tenantId
  }
}

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2023-06-01-preview' = {
  name: postgresServerName
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    administratorLogin: postgresAdministratorLogin
    administratorLoginPassword: postgresAdministratorPassword
    version: '17'
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
  }
}

resource postgresFirewallRule 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-06-01-preview' = {
  parent: postgresServer
  name: 'AllowAllAzureServicesAndResourcesWithinAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource postgresDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-06-01-preview' = {
  parent: postgresServer
  name: postgresDatabaseName
}

resource sqlConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: sqlConnectionStringSecretName
  properties: {
    value: postgresConnectionString
  }
}

resource openAiAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAiAccountName
  location: location
  kind: 'OpenAI'
  sku: {
    name: openAiSkuName
  }
  properties: {
    customSubDomainName: openAiAccountName
    publicNetworkAccess: 'Enabled'
  }
}

resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openAiAccount
  name: 'gpt-4o-mini'
  sku: {
    name: 'GlobalStandard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o-mini'
    }
  }
}

resource embeddingDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openAiAccount
  name: 'text-embedding-3-small'
  dependsOn: [gpt4oDeployment]
  sku: {
    name: 'GlobalStandard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-3-small'
    }
  }
}

resource openAiEndpointSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: openAiEndpointSecretName
  properties: {
    value: openAiAccount.properties.endpoint
  }
}

resource openAiKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: openAiKeySecretName
  properties: {
    value: openAiAccount.listKeys().key1
  }
}

resource jwtSecretKvSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: jwtSecretSecretName
  properties: {
    value: jwtSecret
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource webApp 'Microsoft.Web/sites@2024-04-01' = {
  name: webAppName
  location: location
  kind: 'app,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: webAppSettings
      ftpsState: 'Disabled'
      healthCheckPath: '/health'
      http20Enabled: true
      linuxFxVersion: 'DOTNETCORE|10.0'
      minTlsVersion: '1.2'
    }
  }
}

resource keyVaultSecretsUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, webApp.id, keyVaultSecretsUserRoleDefinitionId)
  properties: {
    principalId: webApp.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
  }
}

resource staticWebApp 'Microsoft.Web/staticSites@2024-04-01' = {
  name: staticWebAppName
  location: staticWebAppLocation
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    allowConfigFileUpdates: true
    publicNetworkAccess: 'Enabled'
    stagingEnvironmentPolicy: 'Disabled'
  }
}

output webAppDefaultHostname string = webApp.properties.defaultHostName
output staticWebAppDefaultHostname string = staticWebApp.properties.defaultHostname
output openAiEndpoint string = openAiAccount.properties.endpoint
