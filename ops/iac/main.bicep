targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Azure region for all Static Web Apps. Defaults to the main deployment location.')
param staticWebAppLocation string = location

@description('Core hub Static Web App name.')
param coreStaticWebAppName string

@description('Identity Static Web App name.')
param identityStaticWebAppName string

@description('KinRecipe Static Web App name.')
param kinRecipeStaticWebAppName string

@description('Container Apps environment name.')
param containerAppsEnvironmentName string

@description('Identity backend container app name.')
param identityContainerAppName string

@description('KinRecipe backend container app name.')
param kinRecipeContainerAppName string

@description('Application Insights name.')
param applicationInsightsName string

@description('Log Analytics workspace name.')
param logAnalyticsWorkspaceName string

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

@description('JWT secret key shared across KinHub services.')
@secure()
param jwtSecret string

@description('JWT issuer value.')
param jwtIssuer string = 'kinhub'

@description('JWT access token expiry in minutes.')
param jwtAccessTokenExpiryMinutes string = '15'

@description('JWT refresh token expiry in days.')
param jwtRefreshTokenExpiryDays string = '7'

@description('Container registry server used by Container Apps.')
param ghcrServer string = 'ghcr.io'

@description('Container registry username used by Container Apps to pull private images.')
param ghcrUsername string

@description('Container registry password or PAT used by Container Apps to pull private images.')
@secure()
param ghcrPassword string

@description('Full image reference for the Identity backend.')
param identityImage string

@description('Full image reference for the KinRecipe backend.')
param kinRecipeImage string

var postgresConnectionString = 'Server=${postgresServer.properties.fullyQualifiedDomainName};Database=${postgresDatabaseName};Port=5432;User Id=${postgresAdministratorLogin};Password=${postgresAdministratorPassword};Ssl Mode=Require;'
var sqlConnectionStringSecretName = 'database-connection-string'
var jwtSecretSecretName = 'jwt-secret'
var openAiEndpointSecretName = 'openai-endpoint'
var openAiKeySecretName = 'openai-key'
var ghcrUsernameSecretName = 'ghcr-username'
var ghcrPasswordSecretName = 'ghcr-password'

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
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
  dependsOn: [
    gpt4oDeployment
  ]
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

resource sqlConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: sqlConnectionStringSecretName
  properties: {
    value: postgresConnectionString
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

resource ghcrUsernameSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: ghcrUsernameSecretName
  properties: {
    value: ghcrUsername
  }
}

resource ghcrPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: ghcrPasswordSecretName
  properties: {
    value: ghcrPassword
  }
}

resource coreStaticWebApp 'Microsoft.Web/staticSites@2024-04-01' = {
  name: coreStaticWebAppName
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

resource identityStaticWebApp 'Microsoft.Web/staticSites@2024-04-01' = {
  name: identityStaticWebAppName
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

resource kinRecipeStaticWebApp 'Microsoft.Web/staticSites@2024-04-01' = {
  name: kinRecipeStaticWebAppName
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

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppsEnvironmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
  }
}

resource identityContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: identityContainerAppName
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        allowInsecure: false
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      registries: [
        {
          server: ghcrServer
          username: ghcrUsername
          passwordSecretRef: 'ghcr-password'
        }
      ]
      secrets: [
        {
          name: 'db-connection-string'
          value: postgresConnectionString
        }
        {
          name: 'jwt-secret'
          value: jwtSecret
        }
        {
          name: 'ghcr-password'
          value: ghcrPassword
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'identity'
          image: identityImage
          env: [
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsights.properties.ConnectionString
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'ConnectionStrings__KinHub'
              secretRef: 'db-connection-string'
            }
            {
              name: 'Jwt__Secret'
              secretRef: 'jwt-secret'
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
              name: 'Jwt__RefreshTokenExpiryDays'
              value: jwtRefreshTokenExpiryDays
            }
            {
              name: 'Cors__AllowAnyOrigin'
              value: 'false'
            }
            {
              name: 'Cors__AllowedOrigins__0'
              value: 'https://${coreStaticWebApp.properties.defaultHostname}'
            }
            {
              name: 'Cors__AllowedOrigins__1'
              value: 'https://${identityStaticWebApp.properties.defaultHostname}'
            }
            {
              name: 'Cors__AllowedOrigins__2'
              value: 'https://${kinRecipeStaticWebApp.properties.defaultHostname}'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

resource kinRecipeContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: kinRecipeContainerAppName
  location: location
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        allowInsecure: false
        external: true
        targetPort: 8080
        transport: 'auto'
      }
      registries: [
        {
          server: ghcrServer
          username: ghcrUsername
          passwordSecretRef: 'ghcr-password'
        }
      ]
      secrets: [
        {
          name: 'db-connection-string'
          value: postgresConnectionString
        }
        {
          name: 'jwt-secret'
          value: jwtSecret
        }
        {
          name: 'openai-endpoint'
          value: openAiAccount.properties.endpoint
        }
        {
          name: 'openai-key'
          value: openAiAccount.listKeys().key1
        }
        {
          name: 'ghcr-password'
          value: ghcrPassword
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'kinrecipe'
          image: kinRecipeImage
          env: [
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsights.properties.ConnectionString
            }
            {
              name: 'ASPNETCORE_URLS'
              value: 'http://+:8080'
            }
            {
              name: 'ConnectionStrings__KinHub'
              secretRef: 'db-connection-string'
            }
            {
              name: 'Jwt__Secret'
              secretRef: 'jwt-secret'
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
              name: 'Jwt__RefreshTokenExpiryDays'
              value: jwtRefreshTokenExpiryDays
            }
            {
              name: 'OpenAi__Endpoint'
              secretRef: 'openai-endpoint'
            }
            {
              name: 'OpenAi__ApiKey'
              secretRef: 'openai-key'
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
              name: 'Cors__AllowAnyOrigin'
              value: 'false'
            }
            {
              name: 'Cors__AllowedOrigins__0'
              value: 'https://${coreStaticWebApp.properties.defaultHostname}'
            }
            {
              name: 'Cors__AllowedOrigins__1'
              value: 'https://${identityStaticWebApp.properties.defaultHostname}'
            }
            {
              name: 'Cors__AllowedOrigins__2'
              value: 'https://${kinRecipeStaticWebApp.properties.defaultHostname}'
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}

output coreStaticWebAppDefaultHostname string = coreStaticWebApp.properties.defaultHostname
output identityStaticWebAppDefaultHostname string = identityStaticWebApp.properties.defaultHostname
output kinRecipeStaticWebAppDefaultHostname string = kinRecipeStaticWebApp.properties.defaultHostname
output identityApiUrl string = 'https://${identityContainerApp.properties.configuration.ingress.fqdn}'
output kinRecipeApiUrl string = 'https://${kinRecipeContainerApp.properties.configuration.ingress.fqdn}'
output openAiEndpoint string = openAiAccount.properties.endpoint
