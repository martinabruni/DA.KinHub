using './main.bicep'

// Copy to a real .bicepparam file and replace every placeholder with environment-specific values.
// Keep the app/container names aligned with managed-identities.sample.bicepparam.

param location = 'westeurope'
param staticWebAppLocation = 'westeurope'

param coreStaticWebAppName = 'kinhub-core-web-dev'
param identityStaticWebAppName = 'kinhub-identity-web-dev'
param kinRecipeStaticWebAppName = 'kinhub-kinrecipe-web-dev'
param kinListStaticWebAppName = 'kinhub-kinlist-web-dev'

param containerAppsEnvironmentName = 'kinhub-apps-dev'
param identityContainerAppName = 'kinhub-identity-api-dev'
param functionAppName = 'kinhub-func-dev'

param coreFrontendOrigin = 'https://core-dev.example.com'
param identityFrontendOrigin = 'https://identity-dev.example.com'
param kinRecipeFrontendOrigin = 'https://kinrecipe-dev.example.com'
param kinListFrontendOrigin = 'https://kinlist-dev.example.com'

param applicationInsightsName = 'kinhub-ai-dev'
param logAnalyticsWorkspaceName = 'kinhub-logs-dev'
param keyVaultName = 'kinhub-kv-dev'

param postgresServerName = 'kinhub-pg-dev'
param postgresDatabaseName = 'kinhub'
param postgresAdministratorLogin = 'kinhubadmin'
param postgresAdministratorPassword = '<set-secure-postgres-password>'

param openAiAccountName = 'kinhub-openai-dev'
param openAiSkuName = 'S0'
param speechAccountName = 'kinhub-speech-dev'
param speechSkuName = 'S0'
param kinListSpeechCandidateLocales = [
  'it-IT'
  'en-US'
  'en-GB'
  'fr-FR'
  'de-DE'
  'es-ES'
]

param jwtSecret = '<set-secure-jwt-secret>'
param jwtIssuer = 'kinhub'
param jwtAccessTokenExpiryMinutes = '15'
param jwtRefreshTokenExpiryDays = '7'

param ghcrServer = 'ghcr.io'
param ghcrUsername = '<set-ghcr-username>'
param ghcrPassword = '<set-ghcr-pat>'

param identityImage = 'ghcr.io/example/kinhub-identity:latest'

param familyContextApiTimeoutSeconds = '10'
param kinListMaxTitleLength = '100'
param kinListMaxItemLength = '200'
param kinListMaxItemsPerList = '100'
param kinListMaxItemsPerBulkConfirm = '50'
param kinListIdempotencyRetentionHours = '24'
param kinListIdempotencyCleanupIntervalMinutes = '60'
param kinListMaxAudioDurationSeconds = '60'
param kinListMaxAudioBytes = '10485760'
param kinListAudioProcessingTimeoutSeconds = '30'
param kinListAudioUploadSasTtlMinutes = '10'
param kinListAudioOperationRetentionHours = '24'
param kinListAudioPollingRetryAfterSeconds = '2'
param kinListAudioProcessingMaxDequeues = '5'
param kinListTransientRetryMaxAttempts = '3'
param kinListTransientRetryBaseDelayMilliseconds = '250'
param kinListTransientRetryMaxDelayMilliseconds = '5000'
param kinListAllowedAudioMimeTypes = [
  'audio/webm'
  'video/webm'
  'audio/mp4'
  'audio/x-m4a'
  'audio/m4a'
  'audio/ogg'
  'application/ogg'
]

param kinListAudioStorageAccountName = 'kinhubaudiodev'
