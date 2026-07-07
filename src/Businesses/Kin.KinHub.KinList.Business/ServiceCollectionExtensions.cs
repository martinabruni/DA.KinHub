using Kin.KinHub.KinList.Business.KinListFeature;
using Kin.KinHub.KinList.Business.Common;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKinHubKinListBusiness(this IServiceCollection services, Action<KinListOptions>? configure = null)
    {
        var options = new KinListOptions();
        configure?.Invoke(options);
        options.Validate();

        services.AddSingleton(options);
        services.TryAddScoped<IKinListTransactionExecutor, NoOpKinListTransactionExecutor>();
        services.TryAddScoped<IKinListAudioDraftGenerator, UnavailableKinListAudioDraftGenerator>();
        services.TryAddScoped<IAudioProcessingBlobStorage, UnavailableAudioProcessingBlobStorage>();
        services.TryAddScoped<IAudioProcessingQueue, UnavailableAudioProcessingQueue>();
        services.AddSingleton<IEtagProvider, EtagProvider>();
        services.AddScoped<ICorrelationIdProvider, CorrelationIdProvider>();
        services.AddScoped<IKinListMapper, KinListMapper>();
        services.AddScoped<IKinListItemDeduplicator, KinListItemDeduplicator>();
        services.AddScoped<IKinListAudioService, KinListAudioService>();
        services.AddScoped<KinListService>();
        services.AddScoped<IKinListService>(sp => sp.GetRequiredService<KinListService>());
        services.AddScoped<IAudioOperationProcessor>(sp => (KinListAudioService)sp.GetRequiredService<IKinListAudioService>());
        return services;
    }
}
