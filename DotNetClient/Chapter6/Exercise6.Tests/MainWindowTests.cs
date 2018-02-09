using System;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Threading;
using Guts.Client;
using Guts.Client.TestTools;
using NUnit.Framework;

namespace Exercise6.Tests
{
    [MonitoredTestFixture("dotNet1", 6, 6, "http://localhost.fiddler/Guts.Api/")]
    [Apartment(ApartmentState.STA)]
    public class MainWindowTests
    {
        private MainWindow _window;
        private bool _hasMinutesRectangle;
        private bool _hasSecondsRectangle;
        private Rectangle _minutesRectangle;
        private Rectangle _secondsRectangle;
        private bool _hasDispatcherTimer;
        private DispatcherTimer _dispatcherTimer;
        private EventHandler _tickEvent;

        [SetUp]
        public void Setup()
        {
            _window = new MainWindow();

            _hasMinutesRectangle = _window.HasPrivateField<Rectangle>(field => field.Name.ToLower().Contains("minu"));
            if (_hasMinutesRectangle)
            {
                _minutesRectangle = _window.GetPrivateFieldValue<Rectangle>(field => field.Name.ToLower().Contains("minu"));
            }

            _hasSecondsRectangle = _window.HasPrivateField<Rectangle>(field => field.Name.ToLower().Contains("sec"));
            if (_hasSecondsRectangle)
            {
                _secondsRectangle = _window.GetPrivateFieldValue<Rectangle>(field => field.Name.ToLower().Contains("sec"));
            }

            _hasDispatcherTimer = _window.HasPrivateField<DispatcherTimer>();
            if (_hasDispatcherTimer)
            {
                _dispatcherTimer = _window.GetPrivateFieldValue<DispatcherTimer>();

                _tickEvent = _dispatcherTimer.GetPrivateFieldValueByName<EventHandler>(nameof(DispatcherTimer.Tick));
            }
        }

        [MonitoredTest("Should have 2 rectangles in a canvas"), Order(1)]
        public void ShouldHaveTwoRectanglesInACanvas()
        {
            Assert.That(_hasMinutesRectangle, Is.True, () => "No private Rectangle field found with a name that contains 'minu'. Create a Rectangle with a name like 'minutesRectangle'");
            Assert.That(_hasSecondsRectangle, Is.True, () => "No private Rectangle field found with a name that contains 'sec'. Create a Rectangle with a name like 'secondsRectangle'");

            Assert.That(_secondsRectangle, Is.Not.Null, () => "The seconds Rectangle field variable should contain an instance of the Rectangle class");
            Assert.That(_secondsRectangle.Parent, Is.Not.Null, () => "The seconds Rectangle should be inside a Canvas");
            Assert.That(_secondsRectangle.Parent, Is.TypeOf<Canvas>(), () => "The seconds Rectangle should be inside a Canvas");

            Assert.That(_minutesRectangle, Is.Not.Null, () => "The minutes Rectangle field variable should contain an instance of the Rectangle class");
            Assert.That(_minutesRectangle.Parent, Is.Not.Null, () => "The minutes Rectangle should be inside a Canvas");
            Assert.That(_minutesRectangle.Parent, Is.TypeOf<Canvas>(), () => "The minutes Rectangle should be inside a Canvas");
        }

        [MonitoredTest("Should have a DispatcherTimer and a method that handles its ticks"), Order(2)]
        public void ShouldHaveADispatcherTimerAndMethodThatHandlesTicks()
        {
            Assert.That(_hasDispatcherTimer, Is.True, () => "No private field found of the type DispatcherTimer");

            Assert.That(_tickEvent, Is.Not.Null, () => "No event handler set for the Tick event of the DispatcherTimer");

            var invocationList = _tickEvent.GetInvocationList();
            Assert.That(invocationList.Length, Is.GreaterThan(0), () => "No event handler set for the Tick event of the DispatcherTimer");
        }

        [MonitoredTest("Should tick every second"), Order(3)]
        public void ShouldTickEverySecond()
        {
            Assert.That(_dispatcherTimer.Interval, Is.Not.Null, () => "No interval set for the Tick event of the DispatcherTimer");
            Assert.That(_dispatcherTimer.Interval, Is.EqualTo(TimeSpan.FromSeconds(1)), () => "The interval set for the Tick event of the DispatcherTimer should be exactly one second");
            Assert.That(_dispatcherTimer.IsEnabled, Is.True, () => "The dispatcher timer is not started. Use the 'Start' method to start the timer");
        }

        [MonitoredTest("Should make the seconds rectangle wider on every tick"), Order(4)]
        public void ShouldMakeTheSecondsRectangleWiderOnEveryTick()
        {
            _dispatcherTimer.Stop();

            var originalWidth = _secondsRectangle.Width;

            InvokeTickEvent();

            var newWidth = _secondsRectangle.Width;

            Assert.That(newWidth, Is.GreaterThan(originalWidth));
        }

        [MonitoredTest("Should have a zero width seconds rectangle after 60 ticks"), Order(5)]
        public void ShouldHaveAZeroWidthSecondsRectangleAfter60Ticks()
        {
            _dispatcherTimer.Stop();

             _secondsRectangle.Width = 0;

            InvokeTickEvent(60);

            Assert.That(_secondsRectangle.Width, Is.EqualTo(0));
        }

        [MonitoredTest("Should make the minutes rectangle wider every 60 ticks"), Order(6)]
        public void ShouldMakeTheMinutesRectangleWiderEvery60Ticks()
        {
            _dispatcherTimer.Stop();

            var originalWidth = _minutesRectangle.Width;

            InvokeTickEvent(60);

            var newWidth = _minutesRectangle.Width;

            Assert.That(newWidth, Is.GreaterThan(originalWidth));
        }

        [MonitoredTest("Should have a zero width minutes rectangle after one hour"), Order(7)]
        public void ShouldHaveAZeroWidthMinutesRectangleAfterAnHour()
        {
            _dispatcherTimer.Stop();

            _minutesRectangle.Width = 0;

            InvokeTickEvent(60 * 60);

            Assert.That(_minutesRectangle.Width, Is.EqualTo(0));
        }

        private void InvokeTickEvent(int numberOfTimes)
        {
            for (int i = 0; i < numberOfTimes; i++)
            {
                InvokeTickEvent();
            }
        }

        private void InvokeTickEvent()
        {
            if (_tickEvent == null) return;

            var invocationList = _tickEvent.GetInvocationList();
            Assert.That(invocationList.Length, Is.GreaterThan(0));

            foreach (var handlerDelegate in invocationList)
            {
                handlerDelegate.Method.Invoke(handlerDelegate.Target, new Object[] {_dispatcherTimer, EventArgs.Empty});
            }
        }
    }
}
