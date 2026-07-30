using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DA.KinHub.Domain.Common;
using DA.KinHub.Domain.KinList;
using Microsoft.AspNetCore.DataProtection;

namespace DA.KinHub.Infrastructure.Pagination;

internal sealed class ActiveItemsCursorCodec(IDataProtectionProvider dataProtectionProvider, TimeProvider timeProvider) : IActiveItemsCursorCodec
{
    private const string Collection = "active-items";
    private const string FormatVersion = "1";
    private const string OrderVersion = "active-items-v1";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string Encode(Guid familyId, Guid applicationUserId, ActiveItemsCursorDirection direction, int effectivePageSize, ActiveItemsPageAnchor anchor)
    {
        try
        {
            var payload = new CursorPayload(
                FormatVersion,
                Collection,
                OrderVersion,
                direction == ActiveItemsCursorDirection.Next ? "next" : "previous",
                effectivePageSize,
                anchor.GroupCreatedAt,
                anchor.GroupId,
                anchor.PositionInGroup,
                anchor.ItemId,
                timeProvider.GetUtcNow().Add(Lifetime));

            var protector = CreateProtector(familyId, applicationUserId);
            var json = JsonSerializer.Serialize(payload, SerializerOptions);
            var protectedBytes = protector.Protect(Encoding.UTF8.GetBytes(json));
            return Base64UrlEncode(protectedBytes);
        }
        catch (Exception exception) when (IsUnavailable(exception))
        {
            throw new ProtectedDataUnavailableException("The cursor could not be protected.", exception);
        }
    }

    public DecodedActiveItemsCursor Decode(string opaqueCursor, Guid familyId, Guid applicationUserId)
    {
        if (string.IsNullOrWhiteSpace(opaqueCursor))
        {
            throw new ActiveItemsCursorInvalidException("The cursor is invalid.");
        }

        try
        {
            var protectedBytes = Base64UrlDecode(opaqueCursor);
            var protector = CreateProtector(familyId, applicationUserId);
            var json = Encoding.UTF8.GetString(protector.Unprotect(protectedBytes));
            var payload = JsonSerializer.Deserialize<CursorPayload>(json, SerializerOptions)
                ?? throw new ActiveItemsCursorInvalidException("The cursor payload is missing.");

            if (payload.FormatVersion != FormatVersion
                || payload.Collection != Collection
                || payload.OrderVersion != OrderVersion
                || payload.EffectivePageSize <= 0
                || payload.GroupId == Guid.Empty
                || payload.ItemId == Guid.Empty
                || payload.PositionInGroup < 0)
            {
                throw new ActiveItemsCursorInvalidException("The cursor is invalid.");
            }

            if (payload.ExpiresAt <= timeProvider.GetUtcNow())
            {
                throw new ActiveItemsCursorInvalidException("The cursor has expired.");
            }

            var direction = payload.Direction switch
            {
                "next" => ActiveItemsCursorDirection.Next,
                "previous" => ActiveItemsCursorDirection.Previous,
                _ => throw new ActiveItemsCursorInvalidException("The cursor direction is invalid.")
            };

            return new DecodedActiveItemsCursor(
                direction,
                payload.EffectivePageSize,
                new ActiveItemsPageAnchor(payload.GroupCreatedAt, payload.GroupId, payload.PositionInGroup, payload.ItemId));
        }
        catch (ActiveItemsCursorInvalidException)
        {
            throw;
        }
        catch (CryptographicException exception)
        {
            throw new ActiveItemsCursorInvalidException("The cursor is invalid.", exception);
        }
        catch (FormatException exception)
        {
            throw new ActiveItemsCursorInvalidException("The cursor is invalid.", exception);
        }
        catch (JsonException exception)
        {
            throw new ActiveItemsCursorInvalidException("The cursor is invalid.", exception);
        }
        catch (Exception exception) when (IsUnavailable(exception))
        {
            throw new ProtectedDataUnavailableException("The cursor could not be unprotected.", exception);
        }
    }

    private IDataProtector CreateProtector(Guid familyId, Guid applicationUserId)
        => dataProtectionProvider.CreateProtector(
            "KinHub",
            Collection,
            OrderVersion,
            "status:active",
            $"family:{familyId:D}",
            $"user:{applicationUserId:D}");

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
        DateTimeOffset GroupCreatedAt,
        Guid GroupId,
        int PositionInGroup,
        Guid ItemId,
        DateTimeOffset ExpiresAt);
}
