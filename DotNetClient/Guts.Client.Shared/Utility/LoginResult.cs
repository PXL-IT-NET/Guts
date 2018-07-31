namespace Guts.Client.Shared.Utility
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public static LoginResult CreateSuccess()
        {
            return new LoginResult { Success = true };
        }

        public static LoginResult CreateError(string errorMessage)
        {
            return new LoginResult { Success = false, Message = errorMessage };
        }
    }
}