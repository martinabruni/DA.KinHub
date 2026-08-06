using System.Security.Cryptography;
using System.Text;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;
using Microsoft.Extensions.Options;

namespace DA.KinHub.Infrastructure.Persistence;

internal sealed class FamilyInvitationCodeProtector(IOptions<FamilyInvitationCodeOptions> options) : IFamilyInvitationCodeProtector
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public CreatedInvitationCode CreateNewCode()
    {
        var normalized = string.Create(12, 0, static (span, _) =>
        {
            Span<byte> random = stackalloc byte[12];
            RandomNumberGenerator.Fill(random);

            for (var index = 0; index < span.Length; index++)
            {
                span[index] = Alphabet[random[index] % Alphabet.Length];
            }
        });

        return CreateCurrentCode(normalized);
    }

    public string Normalize(string? code)
    {
        var raw = code?.Trim() ?? string.Empty;
        if (raw.Length == 0)
        {
            throw new DomainException("Invitation code is required.");
        }

        var builder = new StringBuilder(12);
        foreach (var character in raw)
        {
            if (character is '-' or ' ')
            {
                continue;
            }

            var normalizedCharacter = char.ToUpperInvariant(character) switch
            {
                'I' => '1',
                'L' => '1',
                'O' => '0',
                var current when Alphabet.Contains(current, StringComparison.Ordinal) => current,
                _ => throw new DomainException("Invitation code format is invalid.")
            };

            builder.Append(normalizedCharacter);
        }

        if (builder.Length != 12)
        {
            throw new DomainException("Invitation code format is invalid.");
        }

        return builder.ToString();
    }

    public IReadOnlyList<InvitationCodeHmacCandidate> CreateLookupCandidates(string normalizedCode)
        => options.Value.HmacKeys
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Value))
            .Select(entry => new InvitationCodeHmacCandidate(entry.Key, ComputeHmac(normalizedCode, entry.Value)))
            .ToArray();

    private CreatedInvitationCode CreateCurrentCode(string normalizedCode)
    {
        var currentKeyVersion = options.Value.CurrentKeyVersion;
        var key = options.Value.HmacKeys[currentKeyVersion];
        return new CreatedInvitationCode(FormatForDisplay(normalizedCode), normalizedCode, new InvitationCodeHmacCandidate(currentKeyVersion, ComputeHmac(normalizedCode, key)));
    }

    private static byte[] ComputeHmac(string normalizedCode, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(normalizedCode));
    }

    private static string FormatForDisplay(string normalizedCode)
        => string.Create(14, normalizedCode, static (span, code) =>
        {
            span[0] = code[0];
            span[1] = code[1];
            span[2] = code[2];
            span[3] = code[3];
            span[4] = '-';
            span[5] = code[4];
            span[6] = code[5];
            span[7] = code[6];
            span[8] = code[7];
            span[9] = '-';
            span[10] = code[8];
            span[11] = code[9];
            span[12] = code[10];
            span[13] = code[11];
        });
}
