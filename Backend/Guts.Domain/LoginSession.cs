using System;
using System.ComponentModel.DataAnnotations;

namespace Guts.Domain
{
    public class LoginSession : IDomainObject
    {
        public int Id { get; set; }

        [Required]
        public string PublicIdentifier { get; set; }

        [Required]
        public string SessionToken { get; set; }

        [Required]
        public string IpAddress { get; set; }

        public string LoginToken { get; set; }

        public bool IsCancelled { get; set; }

        public DateTime CreateDateTime { get; set; }
    }
}