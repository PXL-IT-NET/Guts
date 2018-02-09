using System.Net;
using System.Threading.Tasks;

namespace Guts.Business.Captcha
{
    public interface ICaptchaValidator
    {
        Task<CaptchaVerificationResult> Validate(string captchaToken, IPAddress clientIpAddress);
    }
}