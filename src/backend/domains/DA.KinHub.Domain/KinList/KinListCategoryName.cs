using System.Globalization;
using System.Text;
using DA.KinHub.Domain.Common;

namespace DA.KinHub.Domain.KinList;

public sealed class KinListCategoryName : IEquatable<KinListCategoryName>
{
    private KinListCategoryName(string value, string normalizedValue)
    {
        Value = value;
        NormalizedValue = normalizedValue;
    }

    public string Value { get; }

    public string NormalizedValue { get; }

    public static KinListCategoryName Create(string? value)
    {
        var visual = NormalizeVisual(value);
        var normalized = visual.Normalize(NormalizationForm.FormKC).ToUpperInvariant();
        return new KinListCategoryName(visual, normalized);
    }

    public override string ToString() => Value;

    public bool Equals(KinListCategoryName? other) => other is not null && StringComparer.Ordinal.Equals(NormalizedValue, other.NormalizedValue);

    public override bool Equals(object? obj) => obj is KinListCategoryName other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(NormalizedValue);

    private static string NormalizeVisual(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new DomainException("Category name is required.");
        }

        var builder = new StringBuilder(trimmed.Length);
        var previousWasWhitespace = false;

        foreach (var rune in trimmed.EnumerateRunes())
        {
            if (Rune.IsWhiteSpace(rune))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            if (Rune.IsControl(rune))
            {
                throw new DomainException("Category name cannot contain control characters.");
            }

            builder.Append(rune.ToString());
            previousWasWhitespace = false;
        }

        var normalized = builder.ToString().Normalize(NormalizationForm.FormKC);
        if (normalized.Length == 0)
        {
            throw new DomainException("Category name is required.");
        }

        return normalized;
    }
}
