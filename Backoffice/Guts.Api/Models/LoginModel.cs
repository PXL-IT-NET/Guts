using System.ComponentModel.DataAnnotations;
using Guts.Domain.ValueObjects;

namespace Guts.Api.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please fill in a valid (pxl) email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        public string LoginSessionPublicIdentifier { get; set; }
    }
}