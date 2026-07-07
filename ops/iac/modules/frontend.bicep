targetScope = 'resourceGroup'

@description('Azure region for all Static Web Apps.')
param location string

@description('Ordered Static Web App names: [core, identity, kinRecipe, kinList].')
param staticWebAppNames array

@description('Source repository URL connected to the Static Web Apps.')
param staticSitesRepositoryUrl string

@description('Source control provider connected to the Static Web Apps.')
param staticSitesProvider string

@description('Source branch connected to the Static Web Apps.')
param staticSitesBranch string

@description('Deployment auth policy for the Static Web Apps.')
param staticSitesDeploymentAuthPolicy string

resource staticWebApps 'Microsoft.Web/staticSites@2024-04-01' = [
  for name in staticWebAppNames: {
    name: name
    location: location
    sku: {
      name: 'Standard'
      tier: 'Standard'
    }
    properties: any({
      allowConfigFileUpdates: true
      branch: staticSitesBranch
      deploymentAuthPolicy: staticSitesDeploymentAuthPolicy
      provider: staticSitesProvider
      publicNetworkAccess: 'Enabled'
      repositoryUrl: staticSitesRepositoryUrl
      stagingEnvironmentPolicy: 'Disabled'
    })
  }
]

output defaultHostnames array = [
  for (name, i) in staticWebAppNames: staticWebApps[i].properties.defaultHostname
]
