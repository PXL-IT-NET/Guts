using System;
using System.Linq;
using Guts.Client.Shared.TestTools;
using Guts.Client.Shared.Utility;
using NUnit.Framework;

namespace Guts.Client.Core.Tests
{
    [TestFixture]
    public class MonitoredTestFixtureAttributeTests
    {
        [Test]
        public void Constructor_ShouldUseGutsSettingsJson()
        {
            //Arrange
            var courseCode = Guid.NewGuid().ToString();
            var chapterCode = Guid.NewGuid().ToString();
            var exerciseCode = Guid.NewGuid().ToString();

            //Act
            var testFixture = new ExerciseTestFixtureAttribute(courseCode, chapterCode, exerciseCode);

            //Assert
            var sender = testFixture.GetPrivateFieldValue<TestRunResultSender>();
            Assert.That(sender, Is.Not.Null);

            var authorizationHandler = sender.GetPrivateFieldValue<IAuthorizationHandler>() as AuthorizationHandler;
            Assert.That(authorizationHandler, Is.Not.Null);

            var loginWindowFactory = authorizationHandler.GetPrivateFieldValue<ILoginWindowFactory>() as LoginWindowFactory;
            Assert.That(loginWindowFactory, Is.Not.Null);

            var factoryFields = loginWindowFactory.GetAllPrivateFieldValues<string>().ToList();
            Assert.That(factoryFields, Has.All.StartWith("http"));
        }
    }
}