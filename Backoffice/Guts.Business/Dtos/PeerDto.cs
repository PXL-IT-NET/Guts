namespace Guts.Business.Dtos
{
    public class PeerDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string FullName => (FirstName + " " + LastName).Trim();
    }
}