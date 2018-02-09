using System.Windows;
using System.Windows.Controls;

namespace Exercise1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TheListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (theListBox.SelectedIndex >= 0)
            {
                theListBox.Items.Remove(theListBox.SelectedValue);
            }
        }
    }
}
