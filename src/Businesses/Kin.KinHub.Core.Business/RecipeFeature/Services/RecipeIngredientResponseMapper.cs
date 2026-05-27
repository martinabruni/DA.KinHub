namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IRecipeIngredientResponseMapper
{
    RecipeIngredientResponse Map(RecipeIngredient recipeIngredient);
}

public sealed class RecipeIngredientResponseMapper : IRecipeIngredientResponseMapper
{
    public RecipeIngredientResponse Map(RecipeIngredient recipeIngredient) =>
        new()
        {
            Id = recipeIngredient.Id,
            Name = recipeIngredient.Name,
            MeasureUnit = recipeIngredient.MeasureUnit,
            Quantity = recipeIngredient.Quantity,
            RecipeId = recipeIngredient.RecipeId,
        };
}
