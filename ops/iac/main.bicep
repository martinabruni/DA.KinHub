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

@description('KinList Static Web App name.')
param kinListStaticWebAppName string

@description('Container Apps environment name.')
param containerAppsEnvironmentName string

@description('Identity backend container app name.')
param identityContainerAppName string

@description('KinRecipe backend container app name.')
param kinRecipeContainerAppName string

@description('KinList backend container app name.')
param kinListContainerAppName string

@description('KinList expand/contract migration Container Apps Job name.')
param kinListMigrationJobName string

@description('Core frontend origin allowed by backend CORS.')
param coreFrontendOrigin string

@description('Identity frontend origin allowed by backend CORS.')
param identityFrontendOrigin string

@description('KinRecipe frontend origin allowed by backend CORS.')
param kinRecipeFrontendOrigin string

@description('KinList frontend origin allowed by backend CORS.')
param kinListFrontendOrigin string

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

@description('Azure AI Speech account name used by the KinList audio pipeline.')
param speechAccountName string

@description('Azure AI Speech SKU name.')
param speechSkuName string = 'S0'

@description('OpenAI chat model deployment name used by the KinList audio pipeline.')
param kinListOpenAiModelDeploymentName string = 'gpt-4o-mini'

@description('Ordered candidate speech-to-text locales for the KinList audio pipeline.')
param kinListSpeechCandidateLocales array = [
  'it-IT'
  'en-US'
  'en-GB'
  'fr-FR'
  'de-DE'
  'es-ES'
]

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
@minLength(1)
param ghcrUsername string

@description('Container registry password or PAT used by Container Apps to pull private images.')
@secure()
@minLength(1)
param ghcrPassword string

@description('Full image reference for the Identity backend.')
@minLength(1)
param identityImage string

@description('Full image reference for the KinRecipe backend.')
@minLength(1)
param kinRecipeImage string

@description('Full image reference for the KinList backend.')
@minLength(1)
param kinListImage string

@description('Full image reference for the KinList expand/contract migration job (owned by t06-mig).')
@minLength(1)
param kinListMigrationImage string

@description('Family context API HTTP timeout in seconds.')
param familyContextApiTimeoutSeconds string = '10'

@description('KinList maximum list title length.')
param kinListMaxTitleLength string = '100'

@description('KinList maximum item length.')
param kinListMaxItemLength string = '200'

@description('KinList maximum items per list.')
param kinListMaxItemsPerList string = '100'

@description('KinList maximum items accepted per bulk-confirm / recording.')
param kinListMaxItemsPerBulkConfirm string = '50'

@description('KinList idempotency record retention in hours.')
param kinListIdempotencyRetentionHours string = '24'

@description('KinList idempotency cleanup interval in minutes.')
param kinListIdempotencyCleanupIntervalMinutes string = '60'

@description('KinList maximum audio duration in seconds.')
param kinListMaxAudioDurationSeconds string = '60'

@description('KinList maximum audio payload size in bytes.')
param kinListMaxAudioBytes string = '10485760'

@description('KinList audio processing timeout in seconds.')
param kinListAudioProcessingTimeoutSeconds string = '30'

@description('KinList transient retry maximum attempts.')
param kinListTransientRetryMaxAttempts string = '3'

@description('KinList transient retry base backoff delay in milliseconds.')
param kinListTransientRetryBaseDelayMilliseconds string = '250'

@description('KinList transient retry maximum backoff delay in milliseconds.')
param kinListTransientRetryMaxDelayMilliseconds string = '5000'

@description('KinList allowed audio MIME types accepted by the API.')
param kinListAllowedAudioMimeTypes array = [
  'audio/webm'
  'video/webm'
  'audio/mp4'
  'audio/x-m4a'
  'audio/m4a'
  'audio/ogg'
  'application/ogg'
]

var postgresConnectionString = 'Server=${postgresServer.properties.fullyQualifiedDomainName};Database=${postgresDatabaseName};Port=5432;User Id=${postgresAdministratorLogin};Password=${postgresAdministratorPassword};Ssl Mode=Require;'
var sqlConnectionStringSecretName = 'database-connection-string'
var jwtSecretSecretName = 'jwt-secret'
var openAiEndpointSecretName = 'openai-endpoint'
var openAiKeySecretName = 'openai-key'
var speechEndpointSecretName = 'speech-endpoint'
var speechKeySecretName = 'speech-key'
var ghcrUsernameSecretName = 'ghcr-username'
var ghcrPasswordSecretName = 'ghcr-password'

// Built-in Azure RBAC role definition IDs.
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

var kinListCorsAllowedOrigins = [
  coreFrontendOrigin
  identityFrontendOrigin
  kinRecipeFrontendOrigin
  kinListFrontendOrigin
  'https://${coreStaticWebApp.properties.defaultHostname}'
  'https://${identityStaticWebApp.properties.defaultHostname}'
  'https://${kinRecipeStaticWebApp.properties.defaultHostname}'
  'https://${kinListStaticWebApp.properties.defaultHostname}'
]

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

resource speechAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: speechAccountName
  location: location
  kind: 'SpeechServices'
  sku: {
    name: speechSkuName
  }
  properties: {
    customSubDomainName: speechAccountName
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

resource speechEndpointSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: speechEndpointSecretName
  properties: {
    value: speechAccount.properties.endpoint
  }
}

resource speechKeySecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: speechKeySecretName
  properties: {
    value: speechAccount.listKeys().key1
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

resource kinListStaticWebApp 'Microsoft.Web/staticSites@2024-04-01' = {
  name: kinListStaticWebAppName
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
              name: 'Jwt__Audience'
              value: 'kinhub.api'
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
              value: coreFrontendOrigin
            }
            {
              name: 'Cors__AllowedOrigins__1'
              value: identityFrontendOrigin
            }
            {
              name: 'Cors__AllowedOrigins__2'
              value: kinRecipeFrontendOrigin
            }
            {
              name: 'Cors__AllowedOrigins__3'
              value: kinListFrontendOrigin
            }
            {
              name: 'Cors__AllowedOrigins__4'
              value: 'https://${coreStaticWebApp.properties.defaultHostname}'
            }
            {
              name: 'Cors__AllowedOrigins__5'
              value: 'https://${identityStaticWebApp.properties.defaultHostname}'
            }
            {
              name: 'Cors__AllowedOrigins__6'
              value: 'https://${kinRecipeStaticWebApp.properties.defaultHostname}'
            }
            {
              name: 'Cors__AllowedOrigins__7'
              value: 'https://${kinListStaticWebApp.properties.defaultHostname}'
            }
            {
              name: 'OAuth__AuthorizationServerUrl'
              value: 'https://${identityContainerAppName}.${containerAppsEnvironment.properties.defaultDomain}'
            }
            {
              name: 'OAuth__RegistrationUiUrl'
              value: '${identityFrontendOrigin}/register'
            }
            {
              name: 'OAuth__Clients__0__ClientId'
              value: 'kinhub-core-spa'
            }
            {
              name: 'OAuth__Clients__0__ClientName'
              value: 'KinHub Core'
            }
            {
              name: 'OAuth__Clients__0__RedirectUris__0'
              value: '${coreFrontendOrigin}/oauth/callback'
            }
            {
              name: 'OAuth__Clients__0__Scope'
              value: 'kinhub.api'
            }
            {
              name: 'OAuth__Clients__1__ClientId'
              value: 'kinhub-identity-spa'
            }
            {
              name: 'OAuth__Clients__1__ClientName'
              value: 'KinHub Identity'
            }
            {
              name: 'OAuth__Clients__1__RedirectUris__0'
              value: '${identityFrontendOrigin}/oauth/callback'
            }
            {
              name: 'OAuth__Clients__1__Scope'
              value: 'kinhub.api'
            }
            {
              name: 'OAuth__Clients__2__ClientId'
              value: 'kinhub-kinrecipe-spa'
            }
            {
              name: 'OAuth__Clients__2__ClientName'
              value: 'KinHub KinRecipe'
            }
            {
              name: 'OAuth__Clients__2__RedirectUris__0'
              value: '${kinRecipeFrontendOrigin}/oauth/callback'
            }
            {
              name: 'OAuth__Clients__2__Scope'
              value: 'kinhub.api'
            }
            {
              name: 'OAuth__Clients__3__ClientId'
              value: 'kinhub-kinlist-spa'
            }
            {
              name: 'OAuth__Clients__3__ClientName'
              value: 'KinHub KinList'
            }
            {
              name: 'OAuth__Clients__3__RedirectUris__0'
              value: '${kinListFrontendOrigin}/oauth/callback'
            }
            {
              name: 'OAuth__Clients__3__Scope'
              value: 'kinhub.api'
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
              name: 'Jwt__Audience'
              value: 'kinhub.api'
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
              value: coreFrontendOrigin
            }
            {
              name: 'Cors__AllowedOrigins__1'
              value: identityFrontendOrigin
            }
            {
              name: 'Cors__AllowedOrigins__2'
              value: kinRecipeFrontendOrigin
            }
            {
              name: 'Cors__AllowedOrigins__3'
              value: 'https://${coreStaticWebApp.properties.defaultHostname}'
            }
            {
              name: 'Cors__AllowedOrigins__4'
              value: 'https://${identityStaticWebApp.properties.defaultHostname}'
            }
            {
              name: 'Cors__AllowedOrigins__5'
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

// Provisioned by managed-identities.bicep in a separate deployment so the
// Managed Identity resource provider has completed replication before Key Vault
// role assignments and Container Apps resolve these identities.
resource kinListIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: '${kinListContainerAppName}-identity'
}

resource kinListMigrationIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: '${kinListMigrationJobName}-identity'
}

resource kinListKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, kinListIdentity.id, keyVaultSecretsUserRoleId)
  properties: {
    principalId: kinListIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
  }
}

resource kinListMigrationKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, kinListMigrationIdentity.id, keyVaultSecretsUserRoleId)
  properties: {
    principalId: kinListMigrationIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
  }
}

resource kinListContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: kinListContainerAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${kinListIdentity.id}': {}
    }
  }
  dependsOn: [
    kinListKeyVaultSecretsUser
  ]
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
          keyVaultUrl: sqlConnectionStringSecret.properties.secretUri
          identity: kinListIdentity.id
        }
        {
          name: 'jwt-secret'
          keyVaultUrl: jwtSecretKvSecret.properties.secretUri
          identity: kinListIdentity.id
        }
        {
          name: 'openai-endpoint'
          keyVaultUrl: openAiEndpointSecret.properties.secretUri
          identity: kinListIdentity.id
        }
        {
          name: 'openai-key'
          keyVaultUrl: openAiKeySecret.properties.secretUri
          identity: kinListIdentity.id
        }
        {
          name: 'speech-endpoint'
          keyVaultUrl: speechEndpointSecret.properties.secretUri
          identity: kinListIdentity.id
        }
        {
          name: 'speech-key'
          keyVaultUrl: speechKeySecret.properties.secretUri
          identity: kinListIdentity.id
        }
        {
          name: 'ghcr-password'
          keyVaultUrl: ghcrPasswordSecret.properties.secretUri
          identity: kinListIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'kinlist'
          image: kinListImage
          env: concat([
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
              name: 'Jwt__Audience'
              value: 'kinhub.api'
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
              name: 'FamilyContextApi__BaseUrl'
              value: 'https://${identityContainerAppName}.${containerAppsEnvironment.properties.defaultDomain}'
            }
            {
              name: 'FamilyContextApi__TimeoutSeconds'
              value: familyContextApiTimeoutSeconds
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
              name: 'OpenAi__ModelDeploymentName'
              value: kinListOpenAiModelDeploymentName
            }
            {
              name: 'Speech__Endpoint'
              secretRef: 'speech-endpoint'
            }
            {
              name: 'Speech__ApiKey'
              secretRef: 'speech-key'
            }
            {
              name: 'KinList__MaxTitleLength'
              value: kinListMaxTitleLength
            }
            {
              name: 'KinList__MaxItemLength'
              value: kinListMaxItemLength
            }
            {
              name: 'KinList__MaxItemsPerList'
              value: kinListMaxItemsPerList
            }
            {
              name: 'KinList__MaxItemsPerBulkConfirm'
              value: kinListMaxItemsPerBulkConfirm
            }
            {
              name: 'KinList__IdempotencyRetentionHours'
              value: kinListIdempotencyRetentionHours
            }
            {
              name: 'KinList__IdempotencyCleanupIntervalMinutes'
              value: kinListIdempotencyCleanupIntervalMinutes
            }
            {
              name: 'KinList__MaxAudioDurationSeconds'
              value: kinListMaxAudioDurationSeconds
            }
            {
              name: 'KinList__MaxAudioBytes'
              value: kinListMaxAudioBytes
            }
            {
              name: 'KinList__AudioProcessingTimeoutSeconds'
              value: kinListAudioProcessingTimeoutSeconds
            }
            {
              name: 'KinList__TransientRetryMaxAttempts'
              value: kinListTransientRetryMaxAttempts
            }
            {
              name: 'KinList__TransientRetryBaseDelayMilliseconds'
              value: kinListTransientRetryBaseDelayMilliseconds
            }
            {
              name: 'KinList__TransientRetryMaxDelayMilliseconds'
              value: kinListTransientRetryMaxDelayMilliseconds
            }
            {
              name: 'Cors__AllowAnyOrigin'
              value: 'false'
            }
          ],
          map(range(0, length(kinListCorsAllowedOrigins)), i => {
            name: 'Cors__AllowedOrigins__${i}'
            value: kinListCorsAllowedOrigins[i]
          }),
          map(range(0, length(kinListSpeechCandidateLocales)), i => {
            name: 'Speech__CandidateLocales__${i}'
            value: kinListSpeechCandidateLocales[i]
          }),
          map(range(0, length(kinListAllowedAudioMimeTypes)), i => {
            name: 'KinList__AllowedAudioMimeTypes__${i}'
            value: kinListAllowedAudioMimeTypes[i]
          }))
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

// Expand/contract migration job. The image and command are owned by t06-mig; this
// template keeps the job definition infra-only and parameterizes the image. It is a
// manual-trigger job so CI/CD can invoke it (az containerapp job start) before rollout.
resource kinListMigrationJob 'Microsoft.App/jobs@2024-03-01' = {
  name: kinListMigrationJobName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${kinListMigrationIdentity.id}': {}
    }
  }
  dependsOn: [
    kinListMigrationKeyVaultSecretsUser
  ]
  properties: {
    environmentId: containerAppsEnvironment.id
    configuration: {
      triggerType: 'Manual'
      replicaTimeout: 1800
      replicaRetryLimit: 1
      manualTriggerConfig: {
        parallelism: 1
        replicaCompletionCount: 1
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
          keyVaultUrl: sqlConnectionStringSecret.properties.secretUri
          identity: kinListMigrationIdentity.id
        }
        {
          name: 'ghcr-password'
          keyVaultUrl: ghcrPasswordSecret.properties.secretUri
          identity: kinListMigrationIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'kinlist-migration'
          image: kinListMigrationImage
          env: [
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsights.properties.ConnectionString
            }
            {
              name: 'ConnectionStrings__KinHub'
              secretRef: 'db-connection-string'
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
        }
      ]
    }
  }
}

output coreStaticWebAppDefaultHostname string = coreStaticWebApp.properties.defaultHostname
output identityStaticWebAppDefaultHostname string = identityStaticWebApp.properties.defaultHostname
output kinRecipeStaticWebAppDefaultHostname string = kinRecipeStaticWebApp.properties.defaultHostname
output kinListStaticWebAppDefaultHostname string = kinListStaticWebApp.properties.defaultHostname
output identityApiUrl string = 'https://${identityContainerApp.properties.configuration.ingress.fqdn}'
output kinRecipeApiUrl string = 'https://${kinRecipeContainerApp.properties.configuration.ingress.fqdn}'
output kinListApiUrl string = 'https://${kinListContainerApp.properties.configuration.ingress.fqdn}'
output kinListMigrationJobName string = kinListMigrationJob.name
output openAiEndpoint string = openAiAccount.properties.endpoint
output speechEndpoint string = speechAccount.properties.endpoint
