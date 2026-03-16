using Guts.Client.XUnit.IntegrationTests.Infrastructure;

namespace Guts.Client.XUnit.IntegrationTests;

public class BackendFixture : IDisposable
{
    internal MockGutsApiServer BackendMock { get; } = MockGutsApiServer.Start();

    public void Dispose()
    {
        BackendMock.Dispose();
    }
}