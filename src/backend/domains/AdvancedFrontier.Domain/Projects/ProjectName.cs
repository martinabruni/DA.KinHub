using AdvancedFrontier.Domain.Common;

namespace AdvancedFrontier.Domain.Projects;

public readonly record struct ProjectName
{
    public const int MinimumLength = 3;
    public const int MaximumLength = 120;

    public ProjectName(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is < MinimumLength or > MaximumLength)
        {
            throw new DomainException($"Project name must contain between {MinimumLength} and {MaximumLength} characters.");
        }

        Value = normalized;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
