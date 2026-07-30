using Microsoft.Extensions.Options;

namespace DA.KinHub.Business.KinList;

public sealed class PaginationReadOptions
{
    public const string SectionName = "Pagination";
    public const int AbsoluteMaximum = 5000;

    public int ReadMax { get; init; } = 5000;
}

public sealed class PaginationReadOptionsValidator : IValidateOptions<PaginationReadOptions>
{
    public ValidateOptionsResult Validate(string? name, PaginationReadOptions options)
    {
        if (options.ReadMax <= 0 || options.ReadMax > PaginationReadOptions.AbsoluteMaximum)
        {
            return ValidateOptionsResult.Fail($"Pagination:ReadMax must be between 1 and {PaginationReadOptions.AbsoluteMaximum}.");
        }

        return ValidateOptionsResult.Success;
    }
}
