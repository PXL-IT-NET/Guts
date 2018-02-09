using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Exercise3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Random _random;
        private double _sum;
        private int _numberOfClicks;

        public MainWindow()
        {
            InitializeComponent();
            _random = new Random();
            _sum = 0;
            _numberOfClicks = 0;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var number = _random.Next(200,401);
            _sum += number;
            _numberOfClicks++;

            randomTextBox.Text = number.ToString();
            sumTextBox.Text = _sum.ToString();
            averageTextBox.Text = (_sum / _numberOfClicks).ToString();
        }
    }
}
