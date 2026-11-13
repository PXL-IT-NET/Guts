using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Application.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Guts.Api.Filters;

public class ImpersonationReadOnlyFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        string method = context.HttpContext.Request.Method;

        bool isImpersonating = user?.HasClaim(c => c.Type == GutsClaimTypes.ImpersonatorId) ?? false;
        bool isMutation = HttpMethods.IsPost(method) ||
                          HttpMethods.IsPut(method) ||
                          HttpMethods.IsPatch(method) ||
                          HttpMethods.IsDelete(method);

        if (isImpersonating && isMutation)
        {
            context.Result = new ObjectResult(ErrorModel.FromString("State-changing actions are not allowed while impersonating a user."))
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await next();
    }
}
