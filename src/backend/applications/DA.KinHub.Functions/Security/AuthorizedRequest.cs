using DA.KinHub.Domain.Identity;
using System.Security.Claims;

namespace DA.KinHub.Functions.Security;

public sealed record AuthorizedRequest(ClaimsPrincipal Principal, ExternalIdentity ExternalIdentity);
