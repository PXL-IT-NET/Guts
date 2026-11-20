using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Api.ScheduledJobs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers;

[Produces("application/json")]
[Route("api/scheduled-jobs")]
public class ScheduledJobsController(IQuartzDashboardTicketStore ticketStore) : ControllerBase
{
    /// <summary>
    /// Creates a short-lived, single-use login ticket for the Admin-only Quartz Dashboard.
    /// </summary>
    [HttpPost("dashboard-ticket")]
    [Authorize(Policy = ApiConstants.AdminOnlyPolicy)]
    [ProducesResponseType(typeof(QuartzDashboardTicketModel), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public IActionResult CreateDashboardTicket()
    {
        string ticket = ticketStore.CreateTicket(User);
        string pathBase = GetNormalizedPathBase();
        string loginUrl = $"{Request.Scheme}://{Request.Host}{pathBase}/api/scheduled-jobs/dashboard-login?ticket={Uri.EscapeDataString(ticket)}";

        return Ok(new QuartzDashboardTicketModel
        {
            LoginUrl = loginUrl
        });
    }

    /// <summary>
    /// Exchanges a single-use dashboard ticket for an Admin-only HttpOnly cookie and redirects to the Quartz dashboard.
    /// </summary>
    [HttpGet("dashboard-login")]
    [AllowAnonymous]
    [ProducesResponseType((int)HttpStatusCode.Redirect)]
    [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
    public async Task<IActionResult> LoginToDashboard([FromQuery] string ticket)
    {
        ClaimsPrincipal principal = ticketStore.ConsumeTicket(ticket);
        if (principal == null)
        {
            return Unauthorized();
        }

        await HttpContext.SignInAsync(
            ApiConstants.QuartzDashboardCookieScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            });

        return Redirect($"{GetNormalizedPathBase()}/quartz");
    }

    private string GetNormalizedPathBase()
    {
        return Request.PathBase.Value?.TrimEnd('/') ?? string.Empty;
    }
}
