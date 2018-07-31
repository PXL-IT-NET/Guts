using System.Net.Http;
using System.Threading.Tasks;

namespace Guts.Client.Shared.Utility
{
    public interface IHttpHandler
    {
        void UseBearerToken(string token);
        Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T value);
        Task<TResponse> PostAsJsonAsync<TValue, TResponse>(string requestUri, TValue value);
        Task<T> GetAsJsonAsync<T>(string requestUri);
    }
}