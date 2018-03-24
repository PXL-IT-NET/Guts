using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Guts.Business.Communication;
using Newtonsoft.Json;

namespace Guts.Business.Captcha
{
    public class GoogleCaptchaValidator : ICaptchaValidator
    {
        private readonly string _validationUrl;
        private readonly string _secret;
        private readonly IHttpClient _httpClient;

        public GoogleCaptchaValidator(string validationUrl, string secret, IHttpClient httpClient)
        {
            _validationUrl = validationUrl;
            _secret = secret;
            _httpClient = httpClient;
        }

        public async Task<CaptchaVerificationResult> Validate(string captchaToken, IPAddress clientIpAddress)
        {
            return await _httpClient.PostAsFormUrlEncodedContentAsync<CaptchaVerificationResult>(_validationUrl,
                new KeyValuePair<string, string>("secret", _secret),
                new KeyValuePair<string, string>("response", captchaToken),
                new KeyValuePair<string, string>("remoteip", clientIpAddress.ToString()));
        }
    }
}