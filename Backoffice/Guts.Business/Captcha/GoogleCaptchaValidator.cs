using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Guts.Business.Communication;
using Microsoft.Extensions.Logging;

namespace Guts.Business.Captcha
{
    internal class GoogleCaptchaValidator : ICaptchaValidator
    {
        private readonly string _validationUrl;
        private readonly string _secret;
        private readonly IHttpClient _httpClient;
        private readonly ILogger<GoogleCaptchaValidator> _logger;

        public GoogleCaptchaValidator(string validationUrl, string secret, IHttpClient httpClient, ILogger<GoogleCaptchaValidator> logger)
        {
            _validationUrl = validationUrl;
            _secret = secret;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<CaptchaVerificationResult> Validate(string captchaToken, IPAddress clientIpAddress)
        {
            try
            {
                return await _httpClient.PostAsFormUrlEncodedContentAsync<CaptchaVerificationResult>(_validationUrl,
                    new KeyValuePair<string, string>("secret", _secret),
                    new KeyValuePair<string, string>("response", captchaToken),
                    new KeyValuePair<string, string>("remoteip", clientIpAddress.ToString()));
            }
            catch(Exception e)
            {
                _logger.LogError(e, "Error while validating captcha token.");
                return new CaptchaVerificationResult { Success = false };
            }
        }
    }
}