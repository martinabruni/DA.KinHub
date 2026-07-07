using Kin.KinHub.KinList.Business.KinListFeature;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKinHubKinListAzureStorageInfrastructure(
        this IServiceCollection services,
        Action<Kin.KinHub.KinList.AzureStorage.AudioStorageOptions> configure)
    {
        var options = new Kin.KinHub.KinList.AzureStorage.AudioStorageOptions();
        configure(options);
        options.Validate();

        services.AddSingleton(options);
        services.AddSingleton<Kin.KinHub.KinList.AzureStorage.AzureStorageAudioClients>();
        services.AddSingleton<IAudioProcessingQueuePump, Kin.KinHub.KinList.AzureStorage.AzureAudioProcessingQueuePump>();
        services.AddScoped<IAudioProcessingBlobStorage, Kin.KinHub.KinList.AzureStorage.AzureBlobAudioProcessingBlobStorage>();
        services.AddScoped<IAudioProcessingQueue, Kin.KinHub.KinList.AzureStorage.AzureQueueAudioProcessingQueue>();
        return services;
    }
}
