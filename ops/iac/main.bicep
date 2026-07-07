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

@description('KinList audio worker container app name.')
param kinListAudioWorkerContainerAppName string

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

@description('Source repository URL connected to the Static Web Apps.')
param staticSitesRepositoryUrl string = 'https://github.com/martinabruni/Kin.KinHub'

@description('Source control provider connected to the Static Web Apps.')
param staticSitesProvider string = 'GitHub'

@description('Source branch connected to the Static Web Apps.')
param staticSitesBranch string = 'main'

@description('Deployment auth policy for the Static Web Apps.')
param staticSitesDeploymentAuthPolicy string = 'DeploymentToken'

@description('Full image reference for the Identity backend.')
@minLength(1)
param identityImage string

@description('Full image reference for the KinRecipe backend.')
@minLength(1)
param kinRecipeImage string

@description('Full image reference for the KinList backend.')
@minLength(1)
param kinListImage string

@description('Full image reference for the KinList audio worker.')
@minLength(1)
param kinListAudioWorkerImage string

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

@description('KinList upload SAS TTL in minutes.')
param kinListAudioUploadSasTtlMinutes string = '10'

@description('KinList audio operation retention in hours.')
param kinListAudioOperationRetentionHours string = '24'

@description('KinList audio polling retry-after in seconds.')
param kinListAudioPollingRetryAfterSeconds string = '2'

@description('KinList audio processing max dequeue count.')
param kinListAudioProcessingMaxDequeues string = '5'

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

@description('Storage account name dedicated to KinList audio.')
param kinListAudioStorageAccountName string

// Managed identities are provisioned by managed-identities.bicep in a separate deployment so the
// Managed Identity resource provider has completed replication before Key Vault role assignments
// and Container Apps resolve these identities. They are referenced here to obtain principal ids
// consumed by the data/ai role assignments and the compute container apps.
resource identityIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: '${identityContainerAppName}-identity'
}

resource kinRecipeIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: '${kinRecipeContainerAppName}-identity'
}

resource kinListIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: '${kinListContainerAppName}-identity'
}

resource kinListAudioWorkerIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: '${kinListAudioWorkerContainerAppName}-identity'
}

resource kinListMigrationIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: '${kinListMigrationJobName}-identity'
}

module observability 'modules/observability.bicep' = {
  name: 'observability'
  params: {
    location: location
    applicationInsightsName: applicationInsightsName
    logAnalyticsWorkspaceName: logAnalyticsWorkspaceName
  }
}

module ai 'modules/ai.bicep' = {
  name: 'ai'
  params: {
    location: location
    openAiAccountName: openAiAccountName
    openAiSkuName: openAiSkuName
    speechAccountName: speechAccountName
    speechSkuName: speechSkuName
    kinListApiPrincipalId: kinListIdentity.properties.principalId
    kinListApiIdentityId: kinListIdentity.id
    kinListWorkerPrincipalId: kinListAudioWorkerIdentity.properties.principalId
    kinListWorkerIdentityId: kinListAudioWorkerIdentity.id
  }
}

module data 'modules/data.bicep' = {
  name: 'data'
  params: {
    location: location
    keyVaultName: keyVaultName
    postgresServerName: postgresServerName
    postgresDatabaseName: postgresDatabaseName
    postgresAdministratorLogin: postgresAdministratorLogin
    postgresAdministratorPassword: postgresAdministratorPassword
    kinListAudioStorageAccountName: kinListAudioStorageAccountName
    logAnalyticsWorkspaceId: observability.outputs.logAnalyticsWorkspaceId
    openAiEndpoint: ai.outputs.openAiEndpoint
    openAiKey: ai.outputs.openAiKey
    speechEndpoint: ai.outputs.speechEndpoint
    speechKey: ai.outputs.speechKey
    jwtSecret: jwtSecret
    ghcrUsername: ghcrUsername
    ghcrPassword: ghcrPassword
    identityPrincipalId: identityIdentity.properties.principalId
    kinRecipePrincipalId: kinRecipeIdentity.properties.principalId
    kinListApiPrincipalId: kinListIdentity.properties.principalId
    kinListWorkerPrincipalId: kinListAudioWorkerIdentity.properties.principalId
    kinListMigrationPrincipalId: kinListMigrationIdentity.properties.principalId
    identityIdentityId: identityIdentity.id
    kinRecipeIdentityId: kinRecipeIdentity.id
    kinListApiIdentityId: kinListIdentity.id
    kinListWorkerIdentityId: kinListAudioWorkerIdentity.id
    kinListMigrationIdentityId: kinListMigrationIdentity.id
  }
}

module frontend 'modules/frontend.bicep' = {
  name: 'frontend'
  params: {
    location: staticWebAppLocation
    staticWebAppNames: [
      coreStaticWebAppName
      identityStaticWebAppName
      kinRecipeStaticWebAppName
      kinListStaticWebAppName
    ]
    staticSitesRepositoryUrl: staticSitesRepositoryUrl
    staticSitesProvider: staticSitesProvider
    staticSitesBranch: staticSitesBranch
    staticSitesDeploymentAuthPolicy: staticSitesDeploymentAuthPolicy
  }
}

module compute 'modules/compute.bicep' = {
  name: 'compute'
  // Container Apps depend on the data and ai modules implicitly through the secret URIs,
  // deployment names and storage values consumed below. Because a module only completes once
  // ALL its resources — including the KV/Storage/Speech/OpenAI role assignments — are provisioned,
  // this implicit dependency guarantees the identities are authorized before the apps start.
  params: {
    location: location
    containerAppsEnvironmentName: containerAppsEnvironmentName
    identityContainerAppName: identityContainerAppName
    kinRecipeContainerAppName: kinRecipeContainerAppName
    kinListContainerAppName: kinListContainerAppName
    kinListAudioWorkerContainerAppName: kinListAudioWorkerContainerAppName
    kinListMigrationJobName: kinListMigrationJobName
    logAnalyticsCustomerId: observability.outputs.logAnalyticsCustomerId
    logAnalyticsPrimarySharedKey: observability.outputs.logAnalyticsPrimarySharedKey
    applicationInsightsConnectionString: observability.outputs.applicationInsightsConnectionString
    identityIdentityName: '${identityContainerAppName}-identity'
    kinRecipeIdentityName: '${kinRecipeContainerAppName}-identity'
    kinListIdentityName: '${kinListContainerAppName}-identity'
    kinListAudioWorkerIdentityName: '${kinListAudioWorkerContainerAppName}-identity'
    kinListMigrationIdentityName: '${kinListMigrationJobName}-identity'
    sqlConnectionStringSecretUri: data.outputs.sqlConnectionStringSecretUri
    jwtSecretUri: data.outputs.jwtSecretUri
    openAiEndpointSecretUri: data.outputs.openAiEndpointSecretUri
    openAiKeySecretUri: data.outputs.openAiKeySecretUri
    speechEndpointSecretUri: data.outputs.speechEndpointSecretUri
    ghcrPasswordSecretUri: data.outputs.ghcrPasswordSecretUri
    embeddingDeploymentName: ai.outputs.embeddingDeploymentName
    gpt4oDeploymentName: ai.outputs.gpt4oDeploymentName
    kinListOpenAiModelDeploymentName: kinListOpenAiModelDeploymentName
    storageAccountName: data.outputs.storageAccountName
    kinListAudioContainerName: data.outputs.kinListAudioContainerName
    kinListAudioProcessingQueueName: data.outputs.kinListAudioProcessingQueueName
    kinListAudioPoisonQueueName: data.outputs.kinListAudioPoisonQueueName
    staticWebAppHostnames: frontend.outputs.defaultHostnames
    coreFrontendOrigin: coreFrontendOrigin
    identityFrontendOrigin: identityFrontendOrigin
    kinRecipeFrontendOrigin: kinRecipeFrontendOrigin
    kinListFrontendOrigin: kinListFrontendOrigin
    ghcrServer: ghcrServer
    ghcrUsername: ghcrUsername
    identityImage: identityImage
    kinRecipeImage: kinRecipeImage
    kinListImage: kinListImage
    kinListAudioWorkerImage: kinListAudioWorkerImage
    kinListMigrationImage: kinListMigrationImage
    jwtIssuer: jwtIssuer
    jwtAccessTokenExpiryMinutes: jwtAccessTokenExpiryMinutes
    jwtRefreshTokenExpiryDays: jwtRefreshTokenExpiryDays
    familyContextApiTimeoutSeconds: familyContextApiTimeoutSeconds
    kinListSpeechCandidateLocales: kinListSpeechCandidateLocales
    kinListAllowedAudioMimeTypes: kinListAllowedAudioMimeTypes
    kinListMaxTitleLength: kinListMaxTitleLength
    kinListMaxItemLength: kinListMaxItemLength
    kinListMaxItemsPerList: kinListMaxItemsPerList
    kinListMaxItemsPerBulkConfirm: kinListMaxItemsPerBulkConfirm
    kinListIdempotencyRetentionHours: kinListIdempotencyRetentionHours
    kinListIdempotencyCleanupIntervalMinutes: kinListIdempotencyCleanupIntervalMinutes
    kinListMaxAudioDurationSeconds: kinListMaxAudioDurationSeconds
    kinListMaxAudioBytes: kinListMaxAudioBytes
    kinListAudioProcessingTimeoutSeconds: kinListAudioProcessingTimeoutSeconds
    kinListAudioUploadSasTtlMinutes: kinListAudioUploadSasTtlMinutes
    kinListAudioOperationRetentionHours: kinListAudioOperationRetentionHours
    kinListAudioPollingRetryAfterSeconds: kinListAudioPollingRetryAfterSeconds
    kinListAudioProcessingMaxDequeues: kinListAudioProcessingMaxDequeues
    kinListTransientRetryMaxAttempts: kinListTransientRetryMaxAttempts
    kinListTransientRetryBaseDelayMilliseconds: kinListTransientRetryBaseDelayMilliseconds
    kinListTransientRetryMaxDelayMilliseconds: kinListTransientRetryMaxDelayMilliseconds
  }
}

output coreStaticWebAppDefaultHostname string = frontend.outputs.defaultHostnames[0]
output identityStaticWebAppDefaultHostname string = frontend.outputs.defaultHostnames[1]
output kinRecipeStaticWebAppDefaultHostname string = frontend.outputs.defaultHostnames[2]
output kinListStaticWebAppDefaultHostname string = frontend.outputs.defaultHostnames[3]
output identityApiUrl string = compute.outputs.identityApiUrl
output kinRecipeApiUrl string = compute.outputs.kinRecipeApiUrl
output kinListApiUrl string = compute.outputs.kinListApiUrl
output kinListMigrationJobName string = compute.outputs.kinListMigrationJobName
output openAiEndpoint string = ai.outputs.openAiEndpoint
output speechEndpoint string = ai.outputs.speechEndpoint
