using Kin.KinHub.Core.Business.RecipeAssistantFeature;
using Kin.KinHub.Core.Domain.RecipeFeature;
using Mapster;

namespace Kin.KinHub.Core.Business.Common;

public sealed class CoreMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // AssistantIngredientResponse.Id is intentionally omitted — not exposed to AI suggestions
        config.NewConfig<RecipeIngredient, AssistantIngredientResponse>()
            .Ignore(dest => dest.Id);
    }
}
