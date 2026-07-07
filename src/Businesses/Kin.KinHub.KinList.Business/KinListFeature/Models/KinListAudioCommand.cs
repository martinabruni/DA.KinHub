namespace Kin.KinHub.KinList.Business.KinListFeature;

public sealed class KinListAudioCommand
{
    public required byte[] AudioBytes { get; set; }
    public required string ContentType { get; set; }
    public string FileName { get; set; } = "audio";
}
