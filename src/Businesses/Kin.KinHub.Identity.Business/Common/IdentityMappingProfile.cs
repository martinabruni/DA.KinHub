using Kin.KinHub.Identity.Business.AuthenticationFeature;
using Kin.KinHub.Identity.Domain.AuthenticationFeature;
using Mapster;

namespace Kin.KinHub.Identity.Business.Common;

public sealed class IdentityMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UserProvider, LinkedProviderResponse>()
            .Map(dest => dest.Provider, src => (IdentityProviderType)src.ProviderId)
            .Map(dest => dest.ProviderName, src => Enum.IsDefined(typeof(IdentityProviderType), src.ProviderId)
                ? ((IdentityProviderType)src.ProviderId).ToString()
                : src.ProviderId.ToString())
            .Map(dest => dest.LinkedAt, src => src.CreatedAt);
    }
}
