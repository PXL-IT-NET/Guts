using System.ComponentModel.DataAnnotations;

namespace Guts.Api.Models;

public class VerifyLectorModel
{
    [Required(ErrorMessage = "User identification is required")]
    public string UserId { get; set; }

    [Required(ErrorMessage = "Verification token is required")]
    public string Token { get; set; }
}
