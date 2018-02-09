using System;
using System.Windows;
using System.Windows.Controls;

namespace Exercise7
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int getal = 0;
        private int resultaat = 0;
        private string vorigeOperator = "+";
        private string volgendeOperator;
        private bool nieuwGetal = true;

        public MainWindow()
        {
            InitializeComponent();

            displayTextBlock.Text = "0";
        }

        private void ButtonDigit_Click(object sender, RoutedEventArgs e)
        {
            MaakLeeg();
            displayTextBlock.Text += ((Button)sender).Content.ToString();
        }

        private void MaakLeeg()
        {
            if (nieuwGetal)
            {
                displayTextBlock.Text = "";
                nieuwGetal = false;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            displayTextBlock.Text = "0";
            Initialiseer();
        }

        private void ButtonOperator_Click(object sender, RoutedEventArgs e)
        {
            getal = Convert.ToInt32(displayTextBlock.Text);
            volgendeOperator = ((Button)sender).Content.ToString();
            Count();
        }

        private void Count()
        {
            switch (vorigeOperator)
            {
                case "+":
                    resultaat = (resultaat + getal);
                    break;
                case "-":
                    resultaat = (resultaat - getal);
                    break;
                default:
                    MessageBox.Show("Verkeerde operator");
                    break;
            }
            displayTextBlock.Text = Convert.ToString(resultaat);
            if ((volgendeOperator == "="))
            {
                Initialiseer();
            }
            else
            {
                vorigeOperator = volgendeOperator;
            }
            nieuwGetal = true;
        }

        private void Initialiseer()
        {
            resultaat = 0;
            vorigeOperator = "+";
        }
    }
}
