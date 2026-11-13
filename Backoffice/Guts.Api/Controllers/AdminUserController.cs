using System.Collections.Generic;
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
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string search = null,
        [FromQuery] string role = null,
        [FromQuery] bool OrphanedOnly = false)
    {
        var users = await adminUserService.SearchUsersAsync(search, role, OrphanedOnly, GetUserId());
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

    [HttpDelete("Orphaned")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.Forbidden)]
    public async Task<IActionResult> DeleteAllOrphanedUsers()
    {
        int deletedCount = await adminUserService.DeleteAllOrphanedUsersAsync(GetUserId());
        return Ok(new { deletedCount });
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
