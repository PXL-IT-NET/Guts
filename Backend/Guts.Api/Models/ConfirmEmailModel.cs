using System.ComponentModel.DataAnnotations;

namespace Guts.Api.Models
{
    public class ConfirmEmailModel
    {
        [Required(ErrorMessage = "User identification is required")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Confirmation token is required")]
        public string Token { get; set; }
    }
}