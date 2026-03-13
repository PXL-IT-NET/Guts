using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace Dummy.Tests.Infrastructure;

internal sealed class MockGutsApiServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cancellationTokenSource;

    private readonly ConcurrentQueue<CapturedRequest> _capturedRequests;

    public string BaseUrl { get; }

    public IReadOnlyCollection<CapturedRequest> CapturedRequests => _capturedRequests.ToList();

    private MockGutsApiServer(string baseUrl)
    {
        BaseUrl = baseUrl;
        _listener = new HttpListener();
        _listener.Prefixes.Add(baseUrl);
        _capturedRequests = new ConcurrentQueue<CapturedRequest>();
        _cancellationTokenSource = new CancellationTokenSource();

        _listener.Start();
        Task.Run(() => ListenAsync(_cancellationTokenSource.Token));
    }

    public static MockGutsApiServer Start()
    {
        var baseUrl = "http://localhost:5001/";
        return new MockGutsApiServer(baseUrl);
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext? context = null;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (HttpListenerException)
            {
                if (cancellationToken.IsCancellationRequested) return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            if (context is null) continue;

            _ = Task.Run(() => HandleRequestAsync(context), cancellationToken);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        string requestBody;
        using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
        {
            requestBody = await reader.ReadToEndAsync();
        }

        var request = new CapturedRequest(
            context.Request.HttpMethod,
            context.Request.Url?.AbsolutePath ?? string.Empty,
            context.Request.Headers["Authorization"] ?? string.Empty,
            requestBody);

        _capturedRequests.Enqueue(request);

        var statusCode = HttpStatusCode.NotFound;
        var responseBody = "{}";

        if (request.Path.Equals("/api/testruns/forexercise", StringComparison.OrdinalIgnoreCase) ||
            request.Path.Equals("/api/testruns/forproject", StringComparison.OrdinalIgnoreCase))
        {
            statusCode = HttpStatusCode.Created;
        }

        context.Response.StatusCode = (int)statusCode;
        var buffer = Encoding.UTF8.GetBytes(responseBody);
        context.Response.ContentType = "application/json";
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer);
        context.Response.OutputStream.Close();
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _listener.Stop();
        _listener.Close();
        _cancellationTokenSource.Dispose();
    }
}

internal record CapturedRequest(string Method, string Path, string AuthorizationHeader, string Body);
