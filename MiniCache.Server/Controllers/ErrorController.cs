using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MiniCache.Server.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class ErrorController : ControllerBase
{
    [Route("/error")]
    public IActionResult Handle()
    {
        var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
        var detail = context?.Error?.Message;

        return Problem(
            detail: detail,
            title: "An unexpected error occurred.",
            statusCode: StatusCodes.Status500InternalServerError);
    }
}
