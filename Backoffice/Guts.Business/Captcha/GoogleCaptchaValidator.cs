using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Guts.Business.Communication;
using Microsoft.Extensions.Logging;

namespace Guts.Business.Captcha;

internal class GoogleCaptchaValidator(
    string validationUrl,
    string secret,
    IHttpClient httpClient,
    ILogger<GoogleCaptchaValidator> logger)
    : ICaptchaValidator
{
    public async Task<CaptchaVerificationResult> Validate(string captchaToken, IPAddress clientIpAddress)
    {
        try
        {
            return await httpClient.PostAsFormUrlEncodedContentAsync<CaptchaVerificationResult>(validationUrl,
                new KeyValuePair<string, string>("secret", secret),
                new KeyValuePair<string, string>("response", captchaToken),
                new KeyValuePair<string, string>("remoteip", clientIpAddress.ToString()));
        }
        catch(Exception e)
        {
            logger.LogError(e, "Error while validating captcha token.");
            return new CaptchaVerificationResult { Success = false };
        }
    }
}