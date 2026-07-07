namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class RecipeIngredientAccessResult
{
    private RecipeIngredientAccessResult(bool isSuccess, RecipeIngredient? recipeIngredient, ResultStatus status, string? message)
    {
        IsSuccess = isSuccess;
        RecipeIngredient = recipeIngredient;
        Status = status;
        Message = message;
    }

    public bool IsSuccess { get; }

    public RecipeIngredient? RecipeIngredient { get; }

    public ResultStatus Status { get; }

    public string? Message { get; }

    public static RecipeIngredientAccessResult Success(RecipeIngredient recipeIngredient) =>
        new(true, recipeIngredient, ResultStatus.Success, null);

    public static RecipeIngredientAccessResult NotFound(string message) =>
        new(false, null, ResultStatus.NotFound, message);

    public static RecipeIngredientAccessResult Unauthorized(string message) =>
        new(false, null, ResultStatus.Unauthorized, message);

    public Result<T> ToResult<T>() =>
        Status switch
        {
            ResultStatus.NotFound => Result<T>.NotFound(Message!),
            ResultStatus.Unauthorized => Result<T>.Unauthorized(Message!),
            _ => Result<T>.UnexpectedError(Message ?? "Unexpected recipe ingredient access state."),
        };
}
