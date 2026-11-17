using System;
using System.Linq;
using System.Threading.Tasks;
using Guts.Application.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Guts.Api.Filters;

public class ExamGuestLockdownFilter : IAsyncAuthorizationFilter
{
    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        bool isExamGuest = user?.HasClaim(c =>
            c.Type == GutsClaimTypes.ExamGuest &&
            string.Equals(c.Value, "true", StringComparison.OrdinalIgnoreCase)) ?? false;

        if (!isExamGuest)
        {
            return Task.CompletedTask;
        }

        bool isAllowedEndpoint = context.ActionDescriptor.EndpointMetadata
            .OfType<AllowExamGuestAttribute>()
            .Any();

        if (!isAllowedEndpoint)
        {
            context.Result = new ForbidResult();
            return Task.CompletedTask;
        }

        string claimedExamId = user.FindFirst(GutsClaimTypes.ExamId)?.Value;
        string routeExamId = context.RouteData.Values.TryGetValue("examId", out var value)
            ? value?.ToString()
            : null;

        if (string.IsNullOrWhiteSpace(claimedExamId) ||
            !string.Equals(claimedExamId, routeExamId, StringComparison.Ordinal))
        {
            context.Result = new ForbidResult();
        }

        return Task.CompletedTask;
    }
}
