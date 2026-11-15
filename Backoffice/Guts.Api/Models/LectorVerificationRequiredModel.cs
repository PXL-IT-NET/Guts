namespace Guts.Api.Models;

public class LectorVerificationRequiredModel
{
    public string Code { get; set; } = "LectorVerificationRequired";
    public int UserId { get; set; }
    public string Email { get; set; }
    public string ResendToken { get; set; }
    public string Message { get; set; }
}
