using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using Guts.Client.Shared.Utility;

namespace Guts.Client.Classic.UI
{
    public partial class LoginWindow : Window, ILoginWindow
    {
        private readonly IHttpHandler _httpHandler;

        public event TokenRetrievedHandler TokenRetrieved;

        public async Task StartLoginProcedureAsync()
        {
            await Task.Run(() =>
            {
                Show();
                Focus();
                System.Windows.Threading.Dispatcher.Run();
            });
        }

        public LoginWindow(IHttpHandler httpHandler)
        {
            InitializeComponent();

            _httpHandler = httpHandler;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Topmost = true;
        }

        private async void LoginButtonClick(object sender, RoutedEventArgs e)
        {
            if (TokenRetrieved == null)
            {
                throw new FieldAccessException($"No handler set for {nameof(TokenRetrieved)} event.");
            }

            var tokenRequest = new TokenRequest
            {
                Email = UserNameTextBox.Text,
                Password = PasswordBox.Password
            };

            GoIntoLoadingState();

            try
            {
                var tokenResponse =
                    await _httpHandler.PostAsJsonAsync<TokenRequest, TokenResponse>("api/auth/token",
                        tokenRequest);

                TokenRetrieved.Invoke(tokenResponse.Token);

                Close();
            }
            catch (HttpRequestException requestException)
            {
                MessageTextBlock.Text = string.IsNullOrEmpty(requestException.Message) ? "Unknown error." : requestException.Message;
            }
            catch (HttpResponseException responseException)
            {
                var message = responseException.Message;
                if (string.IsNullOrEmpty(message) && responseException.ResponseStatusCode == HttpStatusCode.Unauthorized)
                {
                    message = "Invalid email / password combination.";
                }
                MessageTextBlock.Text = string.IsNullOrEmpty(message) ? "Unknown error." : message;
            }

            ComeOutOfLoadingState();
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

        private class TokenRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        private class TokenResponse
        {
            public string Token { get; set; }
        }
    }
}
