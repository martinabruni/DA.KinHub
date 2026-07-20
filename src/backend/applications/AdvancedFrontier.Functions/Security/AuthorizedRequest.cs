using AdvancedFrontier.Domain.Identity;
using System.Security.Claims;

namespace AdvancedFrontier.Functions.Security;

public sealed record AuthorizedRequest(ClaimsPrincipal Principal, ExternalIdentity ExternalIdentity);
