using System.ComponentModel.DataAnnotations;

namespace Guts.Api.Models;

public class ResendLectorVerificationModel
{
    [Required(ErrorMessage = "User identification is required")]
    public string UserId { get; set; }

    [Required(ErrorMessage = "Resend token is required")]
    public string ResendToken { get; set; }
}
