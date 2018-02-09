using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;

namespace Guts.Client.UI
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public delegate Task<ApiResult> DoLoginEventHandler(string username, string password);
        public event DoLoginEventHandler DoLogin;

        public LoginWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private async void LoginButtonClick(object sender, RoutedEventArgs e)
        {
            if (DoLogin == null)
            {
                throw new FieldAccessException($"No handler set for {nameof(DoLogin)} event.");
            }

            GoIntoLoadingState();
            var result = await DoLogin.Invoke(UserNameTextBox.Text, PasswordBox.Password);
            ComeOutOfLoadingState();

            if (result.Success)
            {
                Close();
            }
            else
            {
                MessageTextBlock.Text = string.IsNullOrEmpty(result.Message) ? "Unknown error." : result.Message;
            }
        }

        private void ComeOutOfLoadingState()
        {
            LoginButton.IsEnabled = true;
            LoadingImage.Visibility = Visibility.Hidden;
        }

        private void GoIntoLoadingState()
        {
            MessageTextBlock.Text = string.Empty;
            LoginButton.IsEnabled = false;
            LoadingImage.Visibility = Visibility.Visible;
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
