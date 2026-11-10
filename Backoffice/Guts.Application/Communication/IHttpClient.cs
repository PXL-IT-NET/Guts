using System.Collections.Generic;
using System.Threading.Tasks;

namespace Guts.Application.Communication;

public interface IHttpClient
{
    Task<TResponse> PostAsFormUrlEncodedContentAsync<TResponse>(string url, params KeyValuePair<string, string>[] keyValuePairs);
}