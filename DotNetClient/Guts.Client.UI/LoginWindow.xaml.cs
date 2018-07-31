using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Guts.Client.Shared.Utility;

namespace Guts.Client.Classic.UI
{
    public partial class LoginWindow : Window, ILoginWindow
    {
        public event CredentialsProvidedHandler CredentialsProvided;

        public void Start()
        {
            Show();
            Focus();

            System.Windows.Threading.Dispatcher.Run();
        }

        public LoginWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
        }

        private async void LoginButtonClick(object sender, RoutedEventArgs e)
        {
            if (CredentialsProvided == null)
            {
                throw new FieldAccessException($"No handler set for {nameof(CredentialsProvided)} event.");
            }

            GoIntoLoadingState();
            var result = await CredentialsProvided.Invoke(UserNameTextBox.Text, PasswordBox.Password);
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
