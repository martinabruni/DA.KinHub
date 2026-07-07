using System.Globalization;
using Kin.KinHub.KinRecipe.Domain.RecipeAssistantFeature;
using Kin.KinHub.KinRecipe.Domain.RecipeFeature;
using Mapster;

namespace Kin.KinHub.KinRecipe.AzureOpenAi.RecipeAssistantFeature;

internal sealed class OpenAiMappingProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<IngredientJson, RecipeIngredient>()
            .Map(dest => dest.Id, src => ParseGuidOrEmpty(src.Id))
            .Map(dest => dest.MeasureUnit, src => src.Unit)
            .Map(dest => dest.RecipeId, _ => Guid.Empty)
            .Map(dest => dest.CreatedAt, _ => default(DateTime))
            .Map(dest => dest.UpdatedAt, _ => default(DateTime));

        config.NewConfig<StepJson, RecipeStep>()
            .Map(dest => dest.Id, _ => Guid.Empty)
            .Map(dest => dest.RecipeId, _ => Guid.Empty)
            .Map(dest => dest.CreatedAt, _ => default(DateTime))
            .Map(dest => dest.UpdatedAt, _ => default(DateTime));

        config.NewConfig<RecipeJson, Recipe>()
            .Map(dest => dest.Id, _ => Guid.Empty)
            .Map(dest => dest.RecipeBookId, _ => Guid.Empty)
            .Map(dest => dest.FinalTime, src => ParseTimeSpanOrZero(src.FinalTime))
            .Map(dest => dest.CreatedAt, _ => default(DateTime))
            .Map(dest => dest.UpdatedAt, _ => default(DateTime));

        config.NewConfig<SuggestionItem, RecipeSuggestion>();

        config.NewConfig<ChangeJson, RecipeChange>()
            .Map(dest => dest.OriginalIngredientId, src => ParseGuidNullable(src.OriginalIngredientId));
    }

    private static Guid ParseGuidOrEmpty(string? s) =>
        s is not null && Guid.TryParse(s, out var id) ? id : Guid.Empty;

    private static Guid? ParseGuidNullable(string? s) =>
        s is not null && Guid.TryParse(s, out var id) ? id : null;

    private static TimeSpan ParseTimeSpanOrZero(string? s) =>
        TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out var ts) ? ts : TimeSpan.Zero;
}
