namespace Kin.KinHub.Core.Business.RecipeFeature;

public interface IRecipeBookResponseMapper
{
    RecipeBookResponse Map(RecipeBook recipeBook);
}

public sealed class RecipeBookResponseMapper : IRecipeBookResponseMapper
{
    public RecipeBookResponse Map(RecipeBook recipeBook) =>
        new()
        {
            Id = recipeBook.Id,
            Name = recipeBook.Name,
            Description = recipeBook.Description,
            FamilyId = recipeBook.FamilyId,
        };
}
