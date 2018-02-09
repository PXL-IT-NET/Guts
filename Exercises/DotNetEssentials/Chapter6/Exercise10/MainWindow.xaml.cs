using System;
using System.Windows;
using System.Windows.Threading;

namespace Exercise10
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            loginProgressBar.Value = 0;

            _timer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(500)};
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (loginProgressBar.Value == loginProgressBar.Maximum)
            {
                _timer.Stop();
                OKButton.IsEnabled = false;
                MessageBox.Show("U heeft slechts 5 seconden om in te loggen!");
            }
            else
            {
                loginProgressBar.Value += 1;
            }
        }
    }
}
