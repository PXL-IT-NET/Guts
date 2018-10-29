using Guts.Business.Services;
using Guts.Business.Tests.Builders;
using Guts.Data;
using Guts.Data.Repositories;
using Guts.Domain;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Guts.Business.Tests.Services
{
    [TestFixture]
    public class LoginSessionServiceTests
    {
        private LoginSessionService _service;

        private Mock<ILoginSesssionRepository> _loginSessionRepositoryMock;

        [SetUp]
        public void Setup()
        {
            _loginSessionRepositoryMock = new Mock<ILoginSesssionRepository>();
            _service = new LoginSessionService(_loginSessionRepositoryMock.Object);
        }

        [Test]
        public void CreateSessionAsync_ShouldCreateANewSessionAndSaveIt()
        {
            //Arrange
            var ipAddress = "123.0.100.1";
            var generatedId = new Random().Next(1, int.MaxValue);

            _loginSessionRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<LoginSession>())).ReturnsAsync(
                (LoginSession session) =>
                {
                    session.Id = generatedId;
                    return session;
                });

            //Act
            var createdLoginSession = _service.CreateSessionAsync(ipAddress).Result;

            //Assert
            Assert.That(createdLoginSession, Is.Not.Null);
            Assert.That(createdLoginSession.Id, Is.EqualTo(generatedId));
            Assert.That(createdLoginSession.IpAddress, Is.EqualTo(ipAddress));
            Assert.That(createdLoginSession.IsCancelled, Is.False);
            Assert.That(createdLoginSession.LoginToken, Is.Null);
            Assert.That(createdLoginSession.PublicIdentifier, Has.Length.GreaterThanOrEqualTo(36)); //Should be a GUID
            Assert.That(createdLoginSession.SessionToken, Has.Length.GreaterThanOrEqualTo(36)); //Should be a GUID
            Assert.That(createdLoginSession.SessionToken, Is.Not.EqualTo(createdLoginSession.PublicIdentifier));
            Assert.That(createdLoginSession.CreateDateTime, Is.EqualTo(DateTime.UtcNow).Within(5).Seconds);

            _loginSessionRepositoryMock.Verify(repo => repo.AddAsync(It.Is((LoginSession s) => s.IpAddress == ipAddress)), Times.Once);
        }

        [Test]
        public void GetSessionAsync_ShouldUseRepo()
        {
            //Arrange
            var ipAddress = "111.210.100.1";
            var publicIdentifier = Guid.NewGuid().ToString();
            var sessionToken = Guid.NewGuid().ToString();

            var existingSession = new LoginSessionBuilder().Build();

            _loginSessionRepositoryMock
                .Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingSession);

            //Act
            var retrievedLoginSession = _service.GetSessionAsync(publicIdentifier, ipAddress, sessionToken).Result;

            //Assert
            Assert.That(retrievedLoginSession, Is.Not.Null);
            Assert.That(retrievedLoginSession, Is.EqualTo(existingSession));

            _loginSessionRepositoryMock.Verify(repo => repo.GetSingleAsync(publicIdentifier, ipAddress, sessionToken), Times.Once);
        }

        [Test]
        public void CancelSessionAsync_ShouldFindTheSessionAndMarkItAsCancelled()
        {
            //Arrange
            var ipAddress = "111.210.100.1";
            var publicIdentifier = Guid.NewGuid().ToString();

            var existingSession = new LoginSessionBuilder().WithIsCancelled(false).Build();

            _loginSessionRepositoryMock
                .Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingSession);

            //Act
            _service.CancelSessionAsync(publicIdentifier, ipAddress).Wait();

            //Assert
            Assert.That(existingSession.IsCancelled, Is.True);

            _loginSessionRepositoryMock.Verify(repo => repo.GetSingleAsync(publicIdentifier, ipAddress, null), Times.Once);
            _loginSessionRepositoryMock.Verify(repo => repo.UpdateAsync(existingSession), Times.Once);
        }

        [Test]
        public void CancelSessionAsync_ShouldLetTheExceptionBubbleUpIfTheSessionCannotBeFound()
        {
            //Arrange
            var ipAddress = "111.210.100.1";
            var publicIdentifier = Guid.NewGuid().ToString();

            _loginSessionRepositoryMock
                .Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws<DataNotFoundException>();

            //Act
            Assert.That(() => _service.CancelSessionAsync(publicIdentifier, ipAddress), Throws.InstanceOf<DataNotFoundException>());

            //Assert
            _loginSessionRepositoryMock.Verify(repo => repo.GetSingleAsync(publicIdentifier, ipAddress, null), Times.Once);
            _loginSessionRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<LoginSession>()), Times.Never);
        }

        [Test]
        public void CleanUpOldSessionsAsync_ShouldFindAllSessionsOlderThanOneHourAndDeleteThemInBulk()
        {
            //Arrange
            var utcNow = DateTime.UtcNow;
            var allSessions = new List<LoginSession>
            {
                new LoginSessionBuilder().WithCreationDateTime(utcNow.AddHours(-25)).Build(),
                new LoginSessionBuilder().WithCreationDateTime(utcNow.AddHours(-2)).Build(),
                new LoginSessionBuilder().WithCreationDateTime(utcNow.AddMinutes(-55)).Build(),
                new LoginSessionBuilder().WithCreationDateTime(utcNow).Build()
            };

            var sessionsThatShouldBeDeleted =
                allSessions.Where(s => s.CreateDateTime < DateTime.UtcNow.AddHours(-1)).ToList();

            _loginSessionRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(allSessions);

            //Act
            _service.CleanUpOldSessionsAsync().Wait();

            //Assert
            _loginSessionRepositoryMock.Verify(repo => repo.GetAllAsync(), Times.Once);
            _loginSessionRepositoryMock.Verify(
                repo => repo.DeleteBulkAsync(It.Is((IEnumerable<LoginSession> sessions) =>
                    sessions.All(s => sessionsThatShouldBeDeleted.Contains(s)))), Times.Once);
        }

        [Test]
        public void SetLoginTokenForSessionAsync_ShouldFindTheSessionAndSetTheToken()
        {
            //Arrange
            var loginToken = Guid.NewGuid().ToString();

            var existingSession = new LoginSessionBuilder().Build();

            _loginSessionRepositoryMock
                .Setup(repo => repo.GetSingleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(existingSession);

            //Act
            _service.SetLoginTokenForSessionAsync(existingSession.PublicIdentifier, loginToken).Wait();

            //Assert
            Assert.That(existingSession.LoginToken, Is.EqualTo(loginToken));

            _loginSessionRepositoryMock.Verify(repo => repo.GetSingleAsync(existingSession.PublicIdentifier, null, null), Times.Once);
            _loginSessionRepositoryMock.Verify(repo => repo.UpdateAsync(existingSession), Times.Once);
        }

    }
}