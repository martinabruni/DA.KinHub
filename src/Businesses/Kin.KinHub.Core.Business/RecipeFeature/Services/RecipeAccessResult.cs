namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class RecipeAccessResult
{
    private RecipeAccessResult(bool isSuccess, Family? family, Recipe? recipe, RecipeBook? recipeBook, ResultStatus status, string? message)
    {
        IsSuccess = isSuccess;
        Family = family;
        Recipe = recipe;
        RecipeBook = recipeBook;
        Status = status;
        Message = message;
    }

    public bool IsSuccess { get; }

    public Family? Family { get; }

    public Recipe? Recipe { get; }

    public RecipeBook? RecipeBook { get; }

    public ResultStatus Status { get; }

    public string? Message { get; }

    public static RecipeAccessResult Success(Family family, Recipe recipe, RecipeBook recipeBook) =>
        new(true, family, recipe, recipeBook, ResultStatus.Success, null);

    public static RecipeAccessResult NotFound(string message) =>
        new(false, null, null, null, ResultStatus.NotFound, message);

    public static RecipeAccessResult Unauthorized(string message) =>
        new(false, null, null, null, ResultStatus.Unauthorized, message);

    public Result<T> ToResult<T>() =>
        Status switch
        {
            ResultStatus.NotFound => Result<T>.NotFound(Message!),
            ResultStatus.Unauthorized => Result<T>.Unauthorized(Message!),
            _ => Result<T>.UnexpectedError(Message ?? "Unexpected recipe access state."),
        };
}
