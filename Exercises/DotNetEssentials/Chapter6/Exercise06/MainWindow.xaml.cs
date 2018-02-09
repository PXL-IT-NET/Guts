using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Exercise06
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Rectangle _secondsRectangle;
        private Rectangle _minutesRectangle;
        private DispatcherTimer _dispatcherTimer;
        private int _totalSeconds;

        public MainWindow()
        {
            InitializeComponent();

            _minutesRectangle = CreateRectangle(0, 20, 0, 30);
            _secondsRectangle = CreateRectangle(0, 70, 0, 30);
            _dispatcherTimer = new DispatcherTimer();

            _dispatcherTimer.Tick += DispatcherTimer_Tick;
            _dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            _dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            _totalSeconds += 1;

            int secondsToDraw = _totalSeconds % 60;
            _secondsRectangle.Width = secondsToDraw * 10;

            int totalMinutes  = _totalSeconds / 60;
            int minuteToDraw = totalMinutes % 60;

            _minutesRectangle.Width = minuteToDraw * 10;
        }

        private Rectangle CreateRectangle(double x, double y, double width, double height)
        {
            var rectangle = new Rectangle
            {
                Fill = new SolidColorBrush(Colors.DarkSalmon),
                Width = width,
                Height = height,
                Margin = new Thickness(x, y, 0, 0)
            };
            drawingCanvas.Children.Add(rectangle);

            return rectangle;
        }
    }
}
