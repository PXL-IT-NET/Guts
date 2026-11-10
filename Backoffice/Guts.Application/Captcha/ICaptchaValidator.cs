using System.Net;
using System.Threading.Tasks;

namespace Guts.Application.Captcha;

public interface ICaptchaValidator
{
    Task<CaptchaVerificationResult> Validate(string captchaToken, IPAddress clientIpAddress);
}