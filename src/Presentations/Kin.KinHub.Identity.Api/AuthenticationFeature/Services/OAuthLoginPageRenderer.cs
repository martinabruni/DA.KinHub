using Microsoft.AspNetCore.WebUtilities;
using System.Net;

namespace Kin.KinHub.Identity.Api.AuthenticationFeature;

public interface IOAuthLoginPageRenderer
{
    string Render(
        OAuthAuthorizeRequest request,
        OAuthRegisteredClient client,
        string scope,
        string authorizationServerUrl,
        string registrationUiUrl,
        string? errorMessage = null);
}

public sealed class OAuthLoginPageRenderer : IOAuthLoginPageRenderer
{
    public string Render(
        OAuthAuthorizeRequest request,
        OAuthRegisteredClient client,
        string scope,
        string authorizationServerUrl,
        string registrationUiUrl,
        string? errorMessage = null)
    {
        static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

        var errorBlock = string.IsNullOrWhiteSpace(errorMessage)
            ? string.Empty
            : $"<p style=\"color:#b91c1c;margin:0 0 16px;\">{Encode(errorMessage)}</p>";
        var scopes = SplitScopes(scope);
        var hasElevatedScope = scopes.Contains(OAuthScopes.Write, StringComparer.Ordinal) || scopes.Contains(OAuthScopes.Admin, StringComparer.Ordinal);
        var scopeList = string.Join(
            string.Empty,
            scopes.Select(scopeValue => $"<li style=\"display:inline-block;margin:0 8px 8px 0;padding:6px 10px;border-radius:999px;background:#e2e8f0;font-size:14px;\">{Encode(scopeValue)}</li>"));
        var elevatedConsentBlock = hasElevatedScope
            ? """
        <label style="display:flex;gap:10px;align-items:flex-start;margin:0 0 16px;padding:12px;border:1px solid #fecaca;border-radius:10px;background:#fff1f2;">
            <input type="checkbox" name="approve_elevated_access" value="true" style="margin-top:4px;" />
            <span>I understand this client is requesting elevated access that can modify or delete KinHub data.</span>
        </label>
"""
            : string.Empty;
        var registerBlock = string.IsNullOrWhiteSpace(registrationUiUrl)
            ? string.Empty
            : $$"""
        <p style="margin:16px 0 0;font-size:14px;color:#475569;">
            Need an account?
            <a href="{{Encode(QueryHelpers.AddQueryString(registrationUiUrl, "returnTo", QueryHelpers.AddQueryString(new Uri(new Uri(authorizationServerUrl), "/authorize").ToString(), new Dictionary<string, string?>
            {
                ["response_type"] = request.ResponseType,
                ["client_id"] = request.ClientId,
                ["redirect_uri"] = request.RedirectUri,
                ["scope"] = scope,
                ["state"] = request.State,
                ["code_challenge"] = request.CodeChallenge,
                ["code_challenge_method"] = request.CodeChallengeMethod,
            })))}}" style="color:#2563eb;text-decoration:none;font-weight:600;">Create one here</a>.
        </p>
""";

        return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>KinHub OAuth</title>
</head>
<body style="font-family:system-ui,sans-serif;background:#f8fafc;color:#0f172a;padding:32px;">
    <main style="max-width:420px;margin:0 auto;background:white;padding:24px;border-radius:12px;box-shadow:0 10px 30px rgba(15,23,42,.08);">
        <h1 style="margin-top:0;">Authorize {{Encode(client.ClientName)}}</h1>
        <p style="margin:0 0 8px;">Sign in to review the requested KinHub scopes for <strong>{{Encode(client.ClientName)}}</strong>.</p>
        <ul style="list-style:none;padding:0;margin:0 0 16px;">{{scopeList}}</ul>
        {{errorBlock}}
        <form method="post" action="/authorize">
            <input type="hidden" name="response_type" value="{{Encode(request.ResponseType)}}" />
            <input type="hidden" name="client_id" value="{{Encode(request.ClientId)}}" />
            <input type="hidden" name="redirect_uri" value="{{Encode(request.RedirectUri)}}" />
            <input type="hidden" name="scope" value="{{Encode(scope)}}" />
            <input type="hidden" name="state" value="{{Encode(request.State)}}" />
            <input type="hidden" name="code_challenge" value="{{Encode(request.CodeChallenge)}}" />
            <input type="hidden" name="code_challenge_method" value="{{Encode(request.CodeChallengeMethod)}}" />
            <label style="display:block;margin-bottom:12px;">
                <span style="display:block;margin-bottom:6px;">Email</span>
                <input type="email" name="email" autocomplete="username" required style="width:100%;padding:10px;border:1px solid #cbd5e1;border-radius:8px;" />
            </label>
            <label style="display:block;margin-bottom:16px;">
                <span style="display:block;margin-bottom:6px;">Password</span>
                <input type="password" name="password" autocomplete="current-password" required style="width:100%;padding:10px;border:1px solid #cbd5e1;border-radius:8px;" />
            </label>
            {{elevatedConsentBlock}}
            <div style="display:flex;gap:12px;">
                <button type="submit" name="decision" value="approve" style="flex:1;padding:10px 16px;border:0;border-radius:8px;background:#2563eb;color:white;font-weight:600;">Continue</button>
                <button type="submit" name="decision" value="deny" formnovalidate style="flex:1;padding:10px 16px;border:1px solid #cbd5e1;border-radius:8px;background:white;color:#0f172a;font-weight:600;">Deny</button>
            </div>
        </form>
        {{registerBlock}}
    </main>
</body>
</html>
""";
    }

    private static string[] SplitScopes(string? scope) =>
        string.IsNullOrWhiteSpace(scope)
            ? []
            : scope.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
