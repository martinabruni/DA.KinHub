using Kin.KinHub.KinList.AzureOpenAi.Common;
using Kin.KinHub.KinList.AzureOpenAi.KinListFeature;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKinHubKinListAzureOpenAiInfrastructure(
        this IServiceCollection services,
        Action<SpeechToTextOptions> configureSpeech,
        Action<OpenAiOptions> configureOpenAi,
        Action<KinListAudioPromptOptions>? configurePrompt = null)
    {
        var speechOptions = new SpeechToTextOptions();
        configureSpeech(speechOptions);
        speechOptions.Validate();

        var openAiOptions = new OpenAiOptions();
        configureOpenAi(openAiOptions);
        openAiOptions.Validate();

        var promptOptions = new KinListAudioPromptOptions();
        configurePrompt?.Invoke(promptOptions);
        promptOptions.Validate();

        services.AddSingleton(speechOptions);
        services.AddSingleton(openAiOptions);
        services.AddSingleton(promptOptions);
        services.AddScoped<IKinListSpeechTranscriber, AzureSpeechKinListTranscriber>();
        services.AddScoped<IKinListChatCompletionClient, AzureOpenAiKinListChatCompletionClient>();
        services.AddScoped<IKinListAudioPromptInterpreter, AzureOpenAiKinListAudioPromptInterpreter>();
        services.AddScoped<AzureSpeechOpenAiKinListAudioDraftGenerator>();
        services.AddScoped<IKinListAudioDraftGenerator>(sp => new TelemetryKinListAudioDraftGenerator(
            sp.GetRequiredService<AzureSpeechOpenAiKinListAudioDraftGenerator>(),
            sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<TelemetryKinListAudioDraftGenerator>>()));

        return services;
    }
}
