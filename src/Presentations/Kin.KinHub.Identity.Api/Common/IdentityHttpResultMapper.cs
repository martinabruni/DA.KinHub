using Kin.KinHub.Shared.Kernel.Results;
using Kin.KinHub.Shared.Kernel.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.Identity.Api.Common;

/// <summary>
/// Identity-flavoured mapper: <see cref="ResultStatus.Unauthorized"/> maps to 401 Unauthorized
/// (authentication semantics). Delegates to <see cref="SharedHttpResultMapper"/>.
/// </summary>
public static class IdentityHttpResultMapper
{
    public static IActionResult ToActionResult<T>(ControllerBase controller, Result<T> result) =>
        SharedHttpResultMapper.ToActionResult(controller, result, unauthorizedIsForbidden: false);

    public static IActionResult ToCreatedActionResult<T>(ControllerBase controller, Result<T> result) =>
        SharedHttpResultMapper.ToCreatedActionResult(controller, result, unauthorizedIsForbidden: false);
}
