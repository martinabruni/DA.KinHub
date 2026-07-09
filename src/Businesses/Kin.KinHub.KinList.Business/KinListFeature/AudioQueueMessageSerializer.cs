using System.Text.Json;

namespace Kin.KinHub.KinList.Business.KinListFeature;

public static class AudioQueueMessageSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize(AudioQueueMessage message) =>
        JsonSerializer.Serialize(message, JsonOptions);

    public static AudioQueueMessage? TryDeserialize(string messageText)
    {
        try
        {
            return JsonSerializer.Deserialize<AudioQueueMessage>(messageText, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
