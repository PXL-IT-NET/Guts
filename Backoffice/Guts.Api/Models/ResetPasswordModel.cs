using System.ComponentModel.DataAnnotations;

namespace Guts.Api.Models
{
    public class ResetPasswordModel
    {
        [Required(ErrorMessage = "User identification is required")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Confirmation token is required")]
        public string Token { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; }
    }
}