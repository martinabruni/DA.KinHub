using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.Families;
using Microsoft.AspNetCore.DataProtection;

namespace DA.KinHub.Infrastructure.Pagination;

internal sealed class FamilyMemberCursorCodec(IDataProtectionProvider dataProtectionProvider, TimeProvider timeProvider) : IFamilyMemberCursorCodec
{
    private const string Collection = "family-members";
    private const string FormatVersion = "1";
    private const string OrderVersion = "family-members-v1";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string Encode(Guid familyId, FamilyPageCursorDirection direction, int effectivePageSize, FamilyMemberPageAnchor anchor)
    {
        try
        {
            var payload = new CursorPayload(
                FormatVersion,
                Collection,
                OrderVersion,
                direction == FamilyPageCursorDirection.Next ? "next" : "previous",
                effectivePageSize,
                anchor.MembershipCreatedAt,
                anchor.MembershipId,
                timeProvider.GetUtcNow().Add(Lifetime));

            var protector = CreateProtector(familyId);
            var json = JsonSerializer.Serialize(payload, SerializerOptions);
            return Base64UrlEncode(protector.Protect(Encoding.UTF8.GetBytes(json)));
        }
        catch (Exception exception) when (IsUnavailable(exception))
        {
            throw new ProtectedDataUnavailableException("The cursor could not be protected.", exception);
        }
    }

    public DecodedFamilyMemberCursor Decode(string opaqueCursor, Guid familyId)
    {
        if (string.IsNullOrWhiteSpace(opaqueCursor))
        {
            throw new FamilyPageCursorInvalidException("The cursor is invalid.");
        }

        try
        {
            var protector = CreateProtector(familyId);
            var json = Encoding.UTF8.GetString(protector.Unprotect(Base64UrlDecode(opaqueCursor)));
            var payload = JsonSerializer.Deserialize<CursorPayload>(json, SerializerOptions)
                ?? throw new FamilyPageCursorInvalidException("The cursor payload is missing.");

            if (payload.FormatVersion != FormatVersion
                || payload.Collection != Collection
                || payload.OrderVersion != OrderVersion
                || payload.EffectivePageSize <= 0
                || payload.MembershipId == Guid.Empty)
            {
                throw new FamilyPageCursorInvalidException("The cursor is invalid.");
            }

            if (payload.ExpiresAt <= timeProvider.GetUtcNow())
            {
                throw new FamilyPageCursorInvalidException("The cursor has expired.");
            }

            return new DecodedFamilyMemberCursor(
                payload.Direction switch
                {
                    "next" => FamilyPageCursorDirection.Next,
                    "previous" => FamilyPageCursorDirection.Previous,
                    _ => throw new FamilyPageCursorInvalidException("The cursor direction is invalid.")
                },
                payload.EffectivePageSize,
                new FamilyMemberPageAnchor(payload.MembershipCreatedAt, payload.MembershipId));
        }
        catch (FamilyPageCursorInvalidException)
        {
            throw;
        }
        catch (CryptographicException exception)
        {
            throw new FamilyPageCursorInvalidException("The cursor is invalid.", exception);
        }
        catch (FormatException exception)
        {
            throw new FamilyPageCursorInvalidException("The cursor is invalid.", exception);
        }
        catch (JsonException exception)
        {
            throw new FamilyPageCursorInvalidException("The cursor is invalid.", exception);
        }
        catch (Exception exception) when (IsUnavailable(exception))
        {
            throw new ProtectedDataUnavailableException("The cursor could not be unprotected.", exception);
        }
    }

    private IDataProtector CreateProtector(Guid familyId)
        => dataProtectionProvider.CreateProtector("KinHub", Collection, OrderVersion, $"family:{familyId:D}");

    private static string Base64UrlEncode(byte[] value)
        => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        var padding = normalized.Length % 4;
        if (padding > 0)
        {
            normalized = normalized.PadRight(normalized.Length + (4 - padding), '=');
        }

        return Convert.FromBase64String(normalized);
    }

    private static bool IsUnavailable(Exception exception)
        => exception is IOException || exception.InnerException is IOException;

    private sealed record CursorPayload(
        string FormatVersion,
        string Collection,
        string OrderVersion,
        string Direction,
        int EffectivePageSize,
        DateTimeOffset MembershipCreatedAt,
        Guid MembershipId,
        DateTimeOffset ExpiresAt);
}
