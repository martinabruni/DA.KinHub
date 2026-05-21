using System.Text.Json.Serialization;

namespace Kin.KinHub.Core.Domain.ChatFeature;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChatToolCallStatus
{
    Pending,
    Confirmed,
    Rejected
}
