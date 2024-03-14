using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Security.Claims;
using Guts.Domain.RoleAggregate;
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

        protected string GetUserEmail()
        {
            if (User == null) return string.Empty;

            Claim emailClaim = User.FindFirst(ClaimTypes.Email);
            if (emailClaim is null)
            {
                return string.Empty;
            }

            return emailClaim.Value;
        }

        protected IList<string> GetUserRoles()
        {
            var roleClaims = User.FindAll(ClaimTypes.Role);
            return roleClaims.Select(roleClaim => roleClaim.Value).ToList();
        }

        protected bool IsStudent()
        {
            if (User == null) return false;

            return User.IsInRole(Role.Constants.Student);
        }

        protected bool IsLector()
        {
            if (User == null) return false;

            return User.IsInRole(Role.Constants.Lector);
        }

        protected bool IsOwnUserId(int userId)
        {
            return GetUserId() == userId;
        }

    }
}