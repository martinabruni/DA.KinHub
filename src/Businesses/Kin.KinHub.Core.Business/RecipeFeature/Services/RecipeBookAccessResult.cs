namespace Kin.KinHub.Core.Business.RecipeFeature;

public sealed class RecipeBookAccessResult
{
    private RecipeBookAccessResult(bool isSuccess, Family? family, RecipeBook? recipeBook, ResultStatus status, string? message)
    {
        IsSuccess = isSuccess;
        Family = family;
        RecipeBook = recipeBook;
        Status = status;
        Message = message;
    }

    public bool IsSuccess { get; }

    public Family? Family { get; }

    public RecipeBook? RecipeBook { get; }

    public ResultStatus Status { get; }

    public string? Message { get; }

    public static RecipeBookAccessResult Success(Family family, RecipeBook recipeBook) =>
        new(true, family, recipeBook, ResultStatus.Success, null);

    public static RecipeBookAccessResult NotFound(string message) =>
        new(false, null, null, ResultStatus.NotFound, message);

    public static RecipeBookAccessResult Unauthorized(string message) =>
        new(false, null, null, ResultStatus.Unauthorized, message);

    public Result<T> ToResult<T>() =>
        Status switch
        {
            ResultStatus.NotFound => Result<T>.NotFound(Message!),
            ResultStatus.Unauthorized => Result<T>.Unauthorized(Message!),
            _ => Result<T>.UnexpectedError(Message ?? "Unexpected recipe-book access state."),
        };
}
