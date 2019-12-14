using System.ComponentModel.DataAnnotations;

namespace Guts.Api.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "First name is required")]
        [MinLength(2, ErrorMessage = "First name must be at least 2 characters long")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [MinLength(2, ErrorMessage = "Last name must be at least 2 characters long")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please fill in a valid (pxl) email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; }

        [Required(ErrorMessage = "You must prove that you are not a robot")]
        public string CaptchaToken { get; set; }
    }
}