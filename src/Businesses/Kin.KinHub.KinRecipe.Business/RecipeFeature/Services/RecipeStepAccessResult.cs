namespace Kin.KinHub.KinRecipe.Business.RecipeFeature;

public sealed class RecipeStepAccessResult
{
    private RecipeStepAccessResult(bool isSuccess, RecipeStep? recipeStep, ResultStatus status, string? message)
    {
        IsSuccess = isSuccess;
        RecipeStep = recipeStep;
        Status = status;
        Message = message;
    }

    public bool IsSuccess { get; }

    public RecipeStep? RecipeStep { get; }

    public ResultStatus Status { get; }

    public string? Message { get; }

    public static RecipeStepAccessResult Success(RecipeStep recipeStep) =>
        new(true, recipeStep, ResultStatus.Success, null);

    public static RecipeStepAccessResult NotFound(string message) =>
        new(false, null, ResultStatus.NotFound, message);

    public static RecipeStepAccessResult Unauthorized(string message) =>
        new(false, null, ResultStatus.Unauthorized, message);

    public Result<T> ToResult<T>() =>
        Status switch
        {
            ResultStatus.NotFound => Result<T>.NotFound(Message!),
            ResultStatus.Unauthorized => Result<T>.Unauthorized(Message!),
            _ => Result<T>.UnexpectedError(Message ?? "Unexpected recipe step access state."),
        };
}
