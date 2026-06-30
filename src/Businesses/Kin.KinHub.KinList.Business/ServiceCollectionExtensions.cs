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
        services.AddScoped<IKinListService, KinListService>();
        return services;
    }
}
