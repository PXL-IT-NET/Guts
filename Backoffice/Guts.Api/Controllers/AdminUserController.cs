using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Application;
using Guts.Application.Dtos;
using Guts.Application.Services;
using Guts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers;

[Produces("application/json")]
[Route("api/admin/users")]
[Authorize(Policy = ApiConstants.AdminOnlyPolicy)]
public class AdminUserController(IAdminUserService adminUserService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminUserDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string search = null,
        [FromQuery] string role = null,
        [FromQuery] bool orphanedOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int? pageSize = null)
    {
        if (page < 1 || (pageSize.HasValue && pageSize.Value < 1))
        {
            return BadRequest();
        }

        var users = await adminUserService.SearchUsersAsync(search, role, orphanedOnly, GetUserId(), page, pageSize);
        return Ok(users);
    }

    [HttpDelete("{userId}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        if (userId < 1) return BadRequest();
        try
        {
            await adminUserService.DeleteUserAsync(userId, GetUserId());
            return NoContent();
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

    [HttpPost("delete-batch")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> DeleteUsers([FromBody] DeleteUsersModel model)
    {
        if (model == null || !ModelState.IsValid || model.UserIds == null || model.UserIds.Count == 0 || model.UserIds.Any(id => id < 1))
        {
            return BadRequest();
        }

        try
        {
            int deletedCount = await adminUserService.DeleteUsersAsync(model.UserIds, GetUserId());
            return Ok(new { deletedCount });
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

    [HttpPost("{userId}/promote-admin")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> PromoteToAdmin(int userId)
    {
        if (userId < 1) return BadRequest();
        try
        {
            await adminUserService.PromoteToAdminAsync(userId);
            return NoContent();
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

    [HttpPost("{userId}/demote-admin")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> DemoteFromAdmin(int userId)
    {
        if (userId < 1) return BadRequest();
        try
        {
            await adminUserService.DemoteFromAdminAsync(userId);
            return NoContent();
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
