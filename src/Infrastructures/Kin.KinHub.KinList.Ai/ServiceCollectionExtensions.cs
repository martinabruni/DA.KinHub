using Kin.KinHub.KinList.Ai.Common;
using Kin.KinHub.KinList.Ai.KinListFeature;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKinHubKinListAiInfrastructure(
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
        services.AddScoped<IKinListAudioDraftGenerator, AzureSpeechOpenAiKinListAudioDraftGenerator>();

        return services;
    }
}
