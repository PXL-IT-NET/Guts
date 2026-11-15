using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Application;
using Guts.Application.Dtos;
using Guts.Application.Security;
using Guts.Application.Services;
using Guts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers;

[Produces("application/json")]
[Route("api/impersonation")]
[Authorize(Policy = ApiConstants.LectorOrAdminPolicy)]
public class ImpersonationController(IImpersonationService impersonationService) : ControllerBase
{
    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<ImpersonatableUserDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> SearchImpersonatableUsers(
        [FromQuery] string search = null,
        [FromQuery] int page = 1,
        [FromQuery] int? pageSize = null)
    {
        if (page < 1 || (pageSize.HasValue && pageSize.Value < 1))
        {
            return BadRequest();
        }

        var users = await impersonationService.SearchImpersonatableUsersAsync(search, [.. GetUserRoles()], page, pageSize);
        return Ok(users);
    }

    [HttpPost("{userId}")]
    [ProducesResponseType(typeof(TokenAccessPass), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> Impersonate(int userId)
    {
        if (userId < 1) return BadRequest();
        try
        {
            var tokenAccessPass = await impersonationService.ImpersonateAsync(
                GetUserId(),
                [.. GetUserRoles()],
                IsImpersonating(),
                userId);
            return Ok(tokenAccessPass);
        }
        catch (DataNotFoundException)
        {
            return NotFound();
        }
        catch (ContractException ex)
        {
            return BadRequest(ErrorModel.FromException(ex));
        }
    }
}
