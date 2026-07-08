using './main.bicep'

// -----------------------------------------------------------------------------
// PROD environment parameters (non-secret only).
// Secret parameters (postgresAdministratorPassword, jwtSecret, ghcrUsername,
// ghcrPassword) are intentionally NOT set here. They are supplied by the
// deployment workflow via --parameters inline values or Key Vault references.
// -----------------------------------------------------------------------------

// Static Web Apps
param coreStaticWebAppName = 'kinhub-core-swa-prod'
param identityStaticWebAppName = 'kinhub-identity-swa-prod'
param kinRecipeStaticWebAppName = 'kinhub-kinrecipe-swa-prod'
param kinListStaticWebAppName = 'kinhub-kinlist-swa-prod'

// Container Apps
param containerAppsEnvironmentName = 'kinhub-cae-prod'
param identityContainerAppName = 'kinhub-identity-ca-prod'

// Function App (hosts all non-identity routes and the audio-processing queue trigger)
param functionAppName = 'kinhub-func-prod'

// CORS origins
param coreFrontendOrigin = 'https://core.kinhub.example'
param identityFrontendOrigin = 'https://identity.kinhub.example'
param kinRecipeFrontendOrigin = 'https://recipe.kinhub.example'
param kinListFrontendOrigin = 'https://list.kinhub.example'

// Observability
param applicationInsightsName = 'kinhub-appi-prod'
param logAnalyticsWorkspaceName = 'kinhub-law-prod'

// Key Vault
param keyVaultName = 'kinhub-kv-prod'

// PostgreSQL
param postgresServerName = 'kinhub-pg-prod'
param postgresDatabaseName = 'kinhub'
param postgresAdministratorLogin = 'kinhubadmin'

// Azure OpenAI / Speech
param openAiAccountName = 'kinhub-openai-prod'
param openAiSkuName = 'S0'
param speechAccountName = 'kinhub-speech-prod'
param speechSkuName = 'S0'
param kinListAudioStorageAccountName = 'kinhubaudioprod'

// Container images (override per deployment/tag as needed)
param identityImage = 'ghcr.io/kin/kinhub-identity:prod'

// JWT (non-secret settings; jwtSecret is passed by the workflow)
param jwtIssuer = 'kinhub'
