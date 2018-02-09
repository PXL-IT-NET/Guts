using System;
using System.Windows;

namespace Exercise14
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Title = DecNaarBin(12);
        }

        private string DecNaarBin(int waarde)
        {
            string result = String.Empty;
            int division;

            division = waarde / 128;
            waarde = waarde % 128;
            result = result + division;

            division = waarde / 64;
            waarde = waarde % 64;
            result = result + division;

            division = waarde / 32;
            waarde = waarde % 32;
            result = result + division;

            division = waarde / 16;
            waarde = waarde % 16;
            result = result + division;

            division = waarde / 8;
            waarde = waarde % 8;
            result = result + division;

            division = waarde / 4;
            waarde = waarde % 4;
            result = result + division;

            division = waarde / 2;
            waarde = waarde % 2;
            result = result + division + waarde;

            return result;
        }
    }
}
