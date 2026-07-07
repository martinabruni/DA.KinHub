targetScope = 'resourceGroup'

@description('Azure region for all resources.')
param location string

@description('Container Apps environment name.')
param containerAppsEnvironmentName string

@description('Identity backend container app name.')
param identityContainerAppName string

@description('KinRecipe backend container app name.')
param kinRecipeContainerAppName string

@description('KinList backend container app name.')
param kinListContainerAppName string

@description('KinList audio worker container app name.')
param kinListAudioWorkerContainerAppName string

@description('KinList expand/contract migration Container Apps Job name.')
param kinListMigrationJobName string

@description('Log Analytics customer id for the Container Apps environment.')
param logAnalyticsCustomerId string

@description('Log Analytics primary shared key for the Container Apps environment.')
@secure()
param logAnalyticsPrimarySharedKey string

@description('Application Insights connection string.')
param applicationInsightsConnectionString string

// Identities (provisioned by managed-identities.bicep, referenced existing here).
@description('Identity backend user-assigned identity name.')
param identityIdentityName string
param kinRecipeIdentityName string
param kinListIdentityName string
param kinListAudioWorkerIdentityName string
param kinListMigrationIdentityName string

// Key Vault secret URIs.
param sqlConnectionStringSecretUri string
param jwtSecretUri string
param openAiEndpointSecretUri string
param openAiKeySecretUri string
param speechEndpointSecretUri string
param ghcrPasswordSecretUri string

// AI deployment/model names.
param embeddingDeploymentName string
param gpt4oDeploymentName string
param kinListOpenAiModelDeploymentName string

// Storage.
param storageAccountName string
param kinListAudioContainerName string
param kinListAudioProcessingQueueName string
param kinListAudioPoisonQueueName string

// Static Web App hostnames [core, identity, kinRecipe, kinList].
param staticWebAppHostnames array

// CORS origins.
param coreFrontendOrigin string
param identityFrontendOrigin string
param kinRecipeFrontendOrigin string
param kinListFrontendOrigin string

// Container registry.
param ghcrServer string
param ghcrUsername string

// Images.
@minLength(1)
param identityImage string
@minLength(1)
param kinRecipeImage string
@minLength(1)
param kinListImage string
@minLength(1)
param kinListAudioWorkerImage string
@minLength(1)
param kinListMigrationImage string

// JWT config.
param jwtIssuer string
param jwtAccessTokenExpiryMinutes string
param jwtRefreshTokenExpiryDays string

// KinList config.
param familyContextApiTimeoutSeconds string
param kinListSpeechCandidateLocales array
param kinListAllowedAudioMimeTypes array
param kinListMaxTitleLength string
param kinListMaxItemLength string
param kinListMaxItemsPerList string
param kinListMaxItemsPerBulkConfirm string
param kinListIdempotencyRetentionHours string
param kinListIdempotencyCleanupIntervalMinutes string
param kinListMaxAudioDurationSeconds string
param kinListMaxAudioBytes string
param kinListAudioProcessingTimeoutSeconds string
param kinListAudioUploadSasTtlMinutes string
param kinListAudioOperationRetentionHours string
param kinListAudioPollingRetryAfterSeconds string
param kinListAudioProcessingMaxDequeues string
param kinListTransientRetryMaxAttempts string
param kinListTransientRetryBaseDelayMilliseconds string
param kinListTransientRetryMaxDelayMilliseconds string

var kinListCorsAllowedOrigins = [
  coreFrontendOrigin
  identityFrontendOrigin
  kinRecipeFrontendOrigin
  kinListFrontendOrigin
  'https://${staticWebAppHostnames[0]}'
  'https://${staticWebAppHostnames[1]}'
  'https://${staticWebAppHostnames[2]}'
  'https://${staticWebAppHostnames[3]}'
]

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppsEnvironmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsCustomerId
        sharedKey: logAnalyticsPrimarySharedKey
      }
    }
  }
}

resource identityIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: identityIdentityName
}

resource kinRecipeIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: kinRecipeIdentityName
}

resource kinListIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: kinListIdentityName
}

resource kinListAudioWorkerIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: kinListAudioWorkerIdentityName
}

resource kinListMigrationIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: kinListMigrationIdentityName
}

resource identityContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: identityContainerAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityIdentity.id}': {}
    }
  }
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
          keyVaultUrl: sqlConnectionStringSecretUri
          identity: identityIdentity.id
        }
        {
          name: 'jwt-secret'
          keyVaultUrl: jwtSecretUri
          identity: identityIdentity.id
        }
        {
          name: 'ghcr-password'
          keyVaultUrl: ghcrPasswordSecretUri
          identity: identityIdentity.id
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
              value: applicationInsightsConnectionString
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
              value: 'https://${staticWebAppHostnames[0]}'
            }
            {
              name: 'Cors__AllowedOrigins__5'
              value: 'https://${staticWebAppHostnames[1]}'
            }
            {
              name: 'Cors__AllowedOrigins__6'
              value: 'https://${staticWebAppHostnames[2]}'
            }
            {
              name: 'Cors__AllowedOrigins__7'
              value: 'https://${staticWebAppHostnames[3]}'
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
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${kinRecipeIdentity.id}': {}
    }
  }
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
          keyVaultUrl: sqlConnectionStringSecretUri
          identity: kinRecipeIdentity.id
        }
        {
          name: 'jwt-secret'
          keyVaultUrl: jwtSecretUri
          identity: kinRecipeIdentity.id
        }
        {
          name: 'openai-endpoint'
          keyVaultUrl: openAiEndpointSecretUri
          identity: kinRecipeIdentity.id
        }
        {
          name: 'openai-key'
          keyVaultUrl: openAiKeySecretUri
          identity: kinRecipeIdentity.id
        }
        {
          name: 'ghcr-password'
          keyVaultUrl: ghcrPasswordSecretUri
          identity: kinRecipeIdentity.id
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
              value: applicationInsightsConnectionString
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
              name: 'OpenAi__UseManagedIdentity'
              value: 'true'
            }
            {
              name: 'OpenAi__EmbeddingDeploymentName'
              value: embeddingDeploymentName
            }
            {
              name: 'OpenAi__ModelDeploymentName'
              value: gpt4oDeploymentName
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
              value: 'https://${staticWebAppHostnames[0]}'
            }
            {
              name: 'Cors__AllowedOrigins__4'
              value: 'https://${staticWebAppHostnames[1]}'
            }
            {
              name: 'Cors__AllowedOrigins__5'
              value: 'https://${staticWebAppHostnames[2]}'
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

resource kinListContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: kinListContainerAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${kinListIdentity.id}': {}
    }
  }
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
          keyVaultUrl: sqlConnectionStringSecretUri
          identity: kinListIdentity.id
        }
        {
          name: 'jwt-secret'
          keyVaultUrl: jwtSecretUri
          identity: kinListIdentity.id
        }
        {
          name: 'openai-endpoint'
          keyVaultUrl: openAiEndpointSecretUri
          identity: kinListIdentity.id
        }
        {
          name: 'speech-endpoint'
          keyVaultUrl: speechEndpointSecretUri
          identity: kinListIdentity.id
        }
        {
          name: 'ghcr-password'
          keyVaultUrl: ghcrPasswordSecretUri
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
              value: applicationInsightsConnectionString
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
              name: 'OpenAi__UseManagedIdentity'
              value: 'true'
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
              name: 'Speech__UseManagedIdentity'
              value: 'true'
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
              name: 'KinList__AudioUploadSasTtlMinutes'
              value: kinListAudioUploadSasTtlMinutes
            }
            {
              name: 'KinList__AudioOperationRetentionHours'
              value: kinListAudioOperationRetentionHours
            }
            {
              name: 'KinList__AudioPollingRetryAfterSeconds'
              value: kinListAudioPollingRetryAfterSeconds
            }
            {
              name: 'KinList__AudioProcessingMaxDequeues'
              value: kinListAudioProcessingMaxDequeues
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
            {
              name: 'AudioStorage__BlobServiceUri'
              value: 'https://${storageAccountName}.blob.${environment().suffixes.storage}'
            }
            {
              name: 'AudioStorage__QueueServiceUri'
              value: 'https://${storageAccountName}.queue.${environment().suffixes.storage}'
            }
            {
              name: 'AudioStorage__ContainerName'
              value: kinListAudioContainerName
            }
            {
              name: 'AudioStorage__ProcessingQueueName'
              value: kinListAudioProcessingQueueName
            }
            {
              name: 'AudioStorage__PoisonQueueName'
              value: kinListAudioPoisonQueueName
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

resource kinListAudioWorkerContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: kinListAudioWorkerContainerAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${kinListAudioWorkerIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
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
          keyVaultUrl: sqlConnectionStringSecretUri
          identity: kinListAudioWorkerIdentity.id
        }
        {
          name: 'openai-endpoint'
          keyVaultUrl: openAiEndpointSecretUri
          identity: kinListAudioWorkerIdentity.id
        }
        {
          name: 'speech-endpoint'
          keyVaultUrl: speechEndpointSecretUri
          identity: kinListAudioWorkerIdentity.id
        }
        {
          name: 'ghcr-password'
          keyVaultUrl: ghcrPasswordSecretUri
          identity: kinListAudioWorkerIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'kinlist-audio-worker'
          image: kinListAudioWorkerImage
          env: concat([
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsightsConnectionString
            }
            {
              name: 'ConnectionStrings__KinHub'
              secretRef: 'db-connection-string'
            }
            {
              name: 'OpenAi__Endpoint'
              secretRef: 'openai-endpoint'
            }
            {
              name: 'OpenAi__UseManagedIdentity'
              value: 'true'
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
              name: 'Speech__UseManagedIdentity'
              value: 'true'
            }
            {
              name: 'AudioStorage__BlobServiceUri'
              value: 'https://${storageAccountName}.blob.${environment().suffixes.storage}'
            }
            {
              name: 'AudioStorage__QueueServiceUri'
              value: 'https://${storageAccountName}.queue.${environment().suffixes.storage}'
            }
            {
              name: 'AudioStorage__ContainerName'
              value: kinListAudioContainerName
            }
            {
              name: 'AudioStorage__ProcessingQueueName'
              value: kinListAudioProcessingQueueName
            }
            {
              name: 'AudioStorage__PoisonQueueName'
              value: kinListAudioPoisonQueueName
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
              name: 'KinList__AudioUploadSasTtlMinutes'
              value: kinListAudioUploadSasTtlMinutes
            }
            {
              name: 'KinList__AudioOperationRetentionHours'
              value: kinListAudioOperationRetentionHours
            }
            {
              name: 'KinList__AudioPollingRetryAfterSeconds'
              value: kinListAudioPollingRetryAfterSeconds
            }
            {
              name: 'KinList__AudioProcessingMaxDequeues'
              value: kinListAudioProcessingMaxDequeues
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
          ],
          map(range(0, length(kinListSpeechCandidateLocales)), i => {
            name: 'Speech__CandidateLocales__${i}'
            value: kinListSpeechCandidateLocales[i]
          }),
          map(range(0, length(kinListAllowedAudioMimeTypes)), i => {
            name: 'KinList__AllowedAudioMimeTypes__${i}'
            value: kinListAllowedAudioMimeTypes[i]
          }))
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
          keyVaultUrl: sqlConnectionStringSecretUri
          identity: kinListMigrationIdentity.id
        }
        {
          name: 'ghcr-password'
          keyVaultUrl: ghcrPasswordSecretUri
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
              value: applicationInsightsConnectionString
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

output identityApiUrl string = 'https://${identityContainerApp.properties.configuration.ingress.fqdn}'
output kinRecipeApiUrl string = 'https://${kinRecipeContainerApp.properties.configuration.ingress.fqdn}'
output kinListApiUrl string = 'https://${kinListContainerApp.properties.configuration.ingress.fqdn}'
output kinListMigrationJobName string = kinListMigrationJob.name
