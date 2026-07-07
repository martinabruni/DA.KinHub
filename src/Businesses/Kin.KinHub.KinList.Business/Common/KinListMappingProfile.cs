using Kin.KinHub.KinList.Business.KinListFeature;
using Kin.KinHub.KinList.Domain.KinListFeature;
using Mapster;

namespace Kin.KinHub.KinList.Business.Common;

public sealed class KinListMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<KinListItem, KinListExistingDuplicateResponse>()
            .Map(dest => dest.ItemId, src => src.Id);

        config.NewConfig<AudioProcessingOperation, AudioProcessingOperationResponse>()
            .Map(dest => dest.Type, src => src.Type.ToString())
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Ignore(dest => dest.Items)
            .Ignore(dest => dest.ItemProposals)
            .Ignore(dest => dest.ExistingDuplicates)
            .Ignore(dest => dest.RetryAfterSeconds);
    }
}
