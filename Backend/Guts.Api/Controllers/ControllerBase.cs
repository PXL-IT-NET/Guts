using System;
using System.Security.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    public abstract class ControllerBase : Controller
    {
        protected int GetUserId()
        {
            if (User == null) return -1;

            var nameIdentifierClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (nameIdentifierClaim == null)
            {
                throw new AuthenticationException("Could not find NameIdentifier claim of user.");
            }

            if (int.TryParse(nameIdentifierClaim.Value, out int userId) && userId > 0)
            {
                return userId;
            }

            throw new AuthenticationException($"The NameIdentifier ('{nameIdentifierClaim.Value}') of the user should be a positive integer.");
        }
    }
}