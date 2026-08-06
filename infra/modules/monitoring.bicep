param location string
param logAnalyticsName string
param applicationInsightsName string
param tags object = {}
@minValue(30)
@maxValue(730)
param retentionDays int = 30
@minValue(1)
param dailyCapGb int = 1

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    retentionInDays: retentionDays
    features: { enableLogAccessUsingOnlyResourcePermissions: true }
    sku: { name: 'PerGB2018' }
    workspaceCapping: { dailyQuotaGb: dailyCapGb }
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
    DisableLocalAuth: true
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

output applicationInsightsId string = insights.id
output applicationInsightsName string = insights.name
output applicationInsightsConnectionString string = insights.properties.ConnectionString
output logAnalyticsId string = workspace.id
