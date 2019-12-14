using System;
using System.ComponentModel.DataAnnotations;

namespace Guts.Domain.LoginSessionAggregate
{
    public class LoginSession : AggregateRoot
    {
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