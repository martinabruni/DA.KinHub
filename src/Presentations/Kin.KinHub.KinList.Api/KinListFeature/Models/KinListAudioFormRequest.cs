using Microsoft.AspNetCore.Http;

namespace Kin.KinHub.KinList.Api.KinListFeature;

public sealed class KinListAudioFormRequest
{
    public IFormFile? Audio { get; set; }
}
