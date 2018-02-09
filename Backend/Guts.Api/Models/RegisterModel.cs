using System.ComponentModel.DataAnnotations;

namespace Guts.Api.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please fill in a valid (pxl) email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; }

        [Required(ErrorMessage = "You must prove that your are not a robot")]
        public string CaptchaToken { get; set; }
    }
}