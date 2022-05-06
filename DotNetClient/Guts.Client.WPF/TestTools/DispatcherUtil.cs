using System.Windows.Threading;

namespace Guts.Client.WPF.TestTools;

/// <summary>
/// Util to let process the <see cref="Dispatcher"/> queue of method invocation execute in a test.
/// Useful when the code being tested updates the UI from another thread.
/// </summary>
public static class DispatcherUtil
{
    /// <summary>
    /// Processes events from other threads that possibly have updated the UI.
    /// </summary>
    public static void DoEvents()
    {
        DispatcherFrame frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
            new DispatcherOperationCallback(ExitFrame), frame);
        Dispatcher.PushFrame(frame);
    }

    /// <summary>
    /// Processes events from other threads that possibly update the UI. Useful if those events have a time delay
    /// (e.g. Progressbar in the UI should be change every second).
    /// </summary>
    /// <param name="condition">While the condition evaluates to true, events will keep being processed.</param>
    /// <param name="timeoutInMilliseconds">
    /// When the <paramref name="condition"/> does not become true after this many milliseconds, the event processing stops.
    /// </param>
    public static void DoEventsWhile(Func<bool> condition, int timeoutInMilliseconds)
    {
        DateTime endTime = DateTime.Now.AddMilliseconds(timeoutInMilliseconds);

        while (condition() && DateTime.Now < endTime)
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(ExitFrame), frame);
            Dispatcher.PushFrame(frame);
        }
    }

    private static object ExitFrame(object frame)
    {
        ((DispatcherFrame)frame).Continue = false;
        return null;
    }
}