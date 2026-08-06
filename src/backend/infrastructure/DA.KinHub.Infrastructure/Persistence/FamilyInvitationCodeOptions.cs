using Microsoft.Extensions.Options;

namespace DA.KinHub.Infrastructure.Persistence;

public sealed class FamilyInvitationCodeOptions
{
    public const string SectionName = "FamilyInvitations";

    public string CurrentKeyVersion { get; set; } = string.Empty;

    public Dictionary<string, string> HmacKeys { get; set; } = new(StringComparer.Ordinal);
}

public sealed class FamilyInvitationCodeOptionsValidator : IValidateOptions<FamilyInvitationCodeOptions>
{
    public ValidateOptionsResult Validate(string? name, FamilyInvitationCodeOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CurrentKeyVersion))
        {
            return ValidateOptionsResult.Fail("Family invitation HMAC current key version is required.");
        }

        if (options.HmacKeys.Count == 0)
        {
            return ValidateOptionsResult.Fail("At least one family invitation HMAC key is required.");
        }

        if (!options.HmacKeys.TryGetValue(options.CurrentKeyVersion, out var currentKey) || string.IsNullOrWhiteSpace(currentKey))
        {
            return ValidateOptionsResult.Fail("The current family invitation HMAC key version must exist and contain a value.");
        }

        return ValidateOptionsResult.Success;
    }
}
