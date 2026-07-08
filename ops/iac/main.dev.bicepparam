using './main.bicep'

// -----------------------------------------------------------------------------
// DEV environment parameters (non-secret only).
// Secret parameters (postgresAdministratorPassword, jwtSecret, ghcrUsername,
// ghcrPassword) are intentionally NOT set here. They are supplied by the
// deployment workflow via --parameters inline values or Key Vault references.
// -----------------------------------------------------------------------------

// Static Web Apps
param coreStaticWebAppName = 'kinhub-core-swa-dev'
param identityStaticWebAppName = 'kinhub-identity-swa-dev'
param kinRecipeStaticWebAppName = 'kinhub-kinrecipe-swa-dev'
param kinListStaticWebAppName = 'kinhub-kinlist-swa-dev'

// Container Apps
param containerAppsEnvironmentName = 'kinhub-cae-dev'
param identityContainerAppName = 'kinhub-identity-ca-dev'

// Function App (hosts all non-identity routes and the audio-processing queue trigger)
param functionAppName = 'kinhub-func-dev'

// CORS origins
param coreFrontendOrigin = 'https://dev.core.kinhub.example'
param identityFrontendOrigin = 'https://dev.identity.kinhub.example'
param kinRecipeFrontendOrigin = 'https://dev.recipe.kinhub.example'
param kinListFrontendOrigin = 'https://dev.list.kinhub.example'

// Observability
param applicationInsightsName = 'kinhub-appi-dev'
param logAnalyticsWorkspaceName = 'kinhub-law-dev'

// Key Vault
param keyVaultName = 'kinhub-kv-dev'

// PostgreSQL
param postgresServerName = 'kinhub-pg-dev'
param postgresDatabaseName = 'kinhub'
param postgresAdministratorLogin = 'kinhubadmin'

// Azure OpenAI / Speech
param openAiAccountName = 'kinhub-openai-dev'
param openAiSkuName = 'S0'
param speechAccountName = 'kinhub-speech-dev'
param speechSkuName = 'S0'
param kinListAudioStorageAccountName = 'kinhubaudiodev'

// Container images (override per deployment/tag as needed)
param identityImage = 'ghcr.io/kin/kinhub-identity:dev'

// JWT (non-secret settings; jwtSecret is passed by the workflow)
param jwtIssuer = 'kinhub-dev'
