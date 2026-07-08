namespace Kin.KinHub.App.Functions.Common;

public sealed class FunctionController : ControllerBase
{
    public FunctionController(HttpContext httpContext)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = httpContext,
        };
    }
}
