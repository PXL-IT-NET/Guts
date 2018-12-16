using Guts.Api.Models.Converters;
using Guts.Business.Tests.Builders;
using NUnit.Framework;

namespace Guts.Api.Tests.Models.Converters
{

    [TestFixture]
    internal class UserConverterTests
    {
        private UserConverter _converter;
       

        [SetUp]
        public void Setup()
        {
            _converter = new UserConverter();
        }

        [Test]
        public void FromUser_ShouldCorrectlyConvertValidData()
        {
            //Arrange
            var user = new UserBuilder().Build();

            //Act
            var model = _converter.FromUser(user);

            //Assert
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Id, Is.EqualTo(user.Id));
            Assert.That(model.FullName, Contains.Substring(user.FirstName));
            Assert.That(model.FullName, Contains.Substring(user.LastName));
        }

        [Test]
        [TestCase("Wesley", "Hendrikx", "Wesley Hendrikx")]
        [TestCase("", "Hendrikx", "Hendrikx")]
        [TestCase("Wesley", "", "Wesley")]
        [TestCase(null, null, "")]
        public void FromUser_ShouldCorrectlyComposeFullName(string firstName, string lastName, string expected)
        {
            //Arrange
            var user = new UserBuilder().Build();
            user.FirstName = firstName;
            user.LastName = lastName;

            //Act
            var model = _converter.FromUser(user);

            //Assert
            Assert.That(model.FullName, Is.EqualTo(expected));
        }
    }
}
