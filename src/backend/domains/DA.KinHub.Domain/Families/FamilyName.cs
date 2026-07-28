using System.Text;
using DA.KinHub.Domain.Common;

namespace DA.KinHub.Domain.Families;

public sealed class FamilyName : IEquatable<FamilyName>
{
    private FamilyName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static FamilyName Create(string? value)
    {
        var normalized = Normalize(value);
        return new FamilyName(normalized);
    }

    public override string ToString() => Value;

    public bool Equals(FamilyName? other) => other is not null && StringComparer.Ordinal.Equals(Value, other.Value);

    public override bool Equals(object? obj) => obj is FamilyName other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    private static string Normalize(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new DomainException("Family name is required.");
        }

        var builder = new StringBuilder(trimmed.Length);
        var previousWasWhitespace = false;

        foreach (var character in trimmed)
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            if (char.IsControl(character))
            {
                throw new DomainException("Family name cannot contain control characters.");
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        var normalized = builder.ToString();
        if (normalized.Length == 0)
        {
            throw new DomainException("Family name is required.");
        }

        if (normalized.Length > 100)
        {
            throw new DomainException("Family name cannot exceed 100 characters.");
        }

        return normalized;
    }
}
