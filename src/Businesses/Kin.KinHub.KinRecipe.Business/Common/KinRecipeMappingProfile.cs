using Mapster;

namespace Kin.KinHub.KinRecipe.Business.Common;

public sealed class KinRecipeMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RecipeIngredient, AssistantIngredientResponse>()
            .Ignore(dest => dest.Id);
    }
}
