using System;
using NUnit.Framework;

namespace Guts.Domain.Tests
{
    public abstract class DomainTestBase
    {
        protected Random Random;

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
            Random = new Random();
        }
    }
}