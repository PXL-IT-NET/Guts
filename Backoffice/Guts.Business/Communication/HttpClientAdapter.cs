using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Guts.Business.Communication
{
    public class HttpClientAdapter : IHttpClient, IDisposable
    {
        private readonly HttpClient _httpClient;

        public HttpClientAdapter()
        {
            _httpClient = new HttpClient();
        }

        public async Task<TResponse> PostAsFormUrlEncodedContentAsync<TResponse>(string url, params KeyValuePair<string, string>[] keyValuePairs)
        {
            var formContent = new FormUrlEncodedContent(keyValuePairs);

            var response = await _httpClient.PostAsync(url, formContent);
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResponse>(json);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}