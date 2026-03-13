using Xunit.Abstractions;
using Xunit.Sdk;

namespace Guts.Client.XUnit.Utility;

internal class CapturingMessageBus(IMessageBus innerMessageBus) : IMessageBus
{
    private string _failureMessage = string.Empty;

    public bool QueueMessage(IMessageSinkMessage message)
    {
        if (message is ITestFailed testFailed)
        {
            _failureMessage = string.Join(Environment.NewLine, testFailed.Messages);
        }

        return innerMessageBus.QueueMessage(message);
    }

    public string FailureMessage => _failureMessage;

    public void Dispose()
    {
        innerMessageBus.Dispose();
    }
}
