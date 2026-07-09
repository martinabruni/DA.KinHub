using Microsoft.AspNetCore.Http;

namespace Kin.KinHub.App.Functions.KinListFeature;

public sealed class KinListAudioFormRequest
{
    public IFormFile? Audio { get; set; }
}
