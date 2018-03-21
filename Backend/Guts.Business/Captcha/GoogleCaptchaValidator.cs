using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Guts.Business.Captcha
{
    public class GoogleCaptchaValidator : ICaptchaValidator
    {
        private readonly string _validationUrl;
        private readonly string _secret;

        public GoogleCaptchaValidator(string validationUrl, string secret)
        {
            _validationUrl = validationUrl;
            _secret = secret;
        }

        public async Task<CaptchaVerificationResult> Validate(string captchaToken, IPAddress clientIpAddress)
        {
            var httpClient = new HttpClient();
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", _secret),
                new KeyValuePair<string, string>("response", captchaToken),
                new KeyValuePair<string, string>("remoteip", clientIpAddress.ToString())
            });
            var googleVerifyResponse = await httpClient.PostAsync(_validationUrl, formContent);
            var json = await googleVerifyResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<CaptchaVerificationResult>(json);
        }
    }
}