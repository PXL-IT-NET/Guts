namespace Guts.Client.Shared.Utility
{
    public class LoginSession
    {
        public string PublicIdentifier { get; set; }
        public string SessionToken { get; set; }
        public string IpAddress { get; set; }
        public string LoginToken { get; set; }
        public bool IsCancelled { get; set; }
    }
}