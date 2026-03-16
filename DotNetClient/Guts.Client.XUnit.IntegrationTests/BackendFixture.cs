using Guts.Client.XUnit.IntegrationTests.Infrastructure;

namespace Guts.Client.XUnit.IntegrationTests;

public class BackendFixture : IDisposable
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);
    private static int _numberOfFixtures;
    internal static MockGutsApiServer BackendMock { get; private set; } = null!;

    public BackendFixture()
    {
        Semaphore.Wait();
        try
        {
            if (_numberOfFixtures == 0)
            {
                BackendMock = MockGutsApiServer.Start();
            }

            _numberOfFixtures++;
        }
        finally
        {
            Semaphore.Release();
        }
    }

    public void Dispose()
    {
        Semaphore.Wait();
        try
        {
            _numberOfFixtures--;
            if (_numberOfFixtures == 0)
            {
                BackendMock.Dispose();
            }
        }
        finally
        {
            Semaphore.Release();
        }
    }
}