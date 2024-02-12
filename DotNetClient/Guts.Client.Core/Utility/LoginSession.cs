namespace Guts.Client.Core.Utility
{
    public class LoginSession
    {
        public string PublicIdentifier { get; set; } = string.Empty;
        public string SessionToken { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string LoginToken { get; set; } = string.Empty;
        public bool IsCancelled { get; set; }
    }
}