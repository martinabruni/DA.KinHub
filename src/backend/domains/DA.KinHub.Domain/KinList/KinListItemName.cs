using System.Text;
using DA.KinHub.Domain.Common;

namespace DA.KinHub.Domain.KinList;

public sealed class KinListItemName : IEquatable<KinListItemName>
{
    private KinListItemName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static KinListItemName Create(string? value) => new(Normalize(value));

    public override string ToString() => Value;

    public bool Equals(KinListItemName? other) => other is not null && StringComparer.Ordinal.Equals(Value, other.Value);

    public override bool Equals(object? obj) => obj is KinListItemName other && Equals(other);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    private static string Normalize(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new DomainException("Item name is required.");
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
                throw new DomainException("Item name cannot contain control characters.");
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        var normalized = builder.ToString();
        if (normalized.Length == 0)
        {
            throw new DomainException("Item name is required.");
        }

        return normalized;
    }
}
