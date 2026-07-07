using Kin.KinHub.Core.Api.Common;
using Kin.KinHub.Shared.Kernel.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kin.KinHub.KinList.Api.Common;

internal static class KinListHttpResultMapper
{
    public static IActionResult ToActionResult<T>(ControllerBase controller, Result<T> result) =>
        SharedHttpResultMapper.ToActionResult(controller, result, unauthorizedIsForbidden: true);

    public static IActionResult ToCreatedActionResult<T>(ControllerBase controller, Result<T> result) =>
        SharedHttpResultMapper.ToCreatedActionResult(controller, result, unauthorizedIsForbidden: true);
}
