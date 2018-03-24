using System;
using System.Linq;
using System.Net.Mail;
using Guts.Business.Communication;
using Guts.Common.Extensions;
using Guts.Domain;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Communication
{
    [TestFixture]
    public class MailSenderTests
    {
        private Mock<ISmtpClient> _smtpClientMock;
        private MailSender _mailSender;
        private string _fromEmail;
        private string _webAppBaseUrl;
        private User _user;

        [SetUp]
        public void Setup()
        {
            _smtpClientMock = new Mock<ISmtpClient>();
            _fromEmail = "someFromEmailAddress@test.com";
            _webAppBaseUrl = "http://someWebAppUrl.com";
            _mailSender = new MailSender(_smtpClientMock.Object, _fromEmail, _webAppBaseUrl);
            _user = new User { Id = new Random().NextPositive(), Email = "someUserEmailAddress@test.com" };
        }

        [Test]
        public void SendConfirmUserEmailMessageAsync_ShouldSendMailWithConfirmationData()
        {
            //Arrange
            var confirmationToken = Guid.NewGuid().ToString();

            //Act
            _mailSender.SendConfirmUserEmailMessageAsync(_user, confirmationToken).Wait();

            //Assert
            _smtpClientMock.Verify(client => client.SendMailAsync(It.Is((MailMessage message) => 
                VerifyMailMessage(message, _fromEmail, _webAppBaseUrl, _user.Id.ToString(), confirmationToken))), Times.Once);
        }

        [Test]
        public void SendForgotPasswordMessageAsync_ShouldSendMailWithConfirmPasswordData()
        {
            //Arrange
            var forgotPasswordToken = Guid.NewGuid().ToString();

            //Act
            _mailSender.SendForgotPasswordMessageAsync(_user, forgotPasswordToken).Wait();

            //Assert
            _smtpClientMock.Verify(client => client.SendMailAsync(It.Is((MailMessage message) => 
                VerifyMailMessage(message, _fromEmail, _webAppBaseUrl, _user.Id.ToString(), forgotPasswordToken))), Times.Once);
        }

        private bool VerifyMailMessage(MailMessage message, string fromEmail, params string[] bodyParts)
        {
            if (message.From.Address.ToLower() != fromEmail.ToLower()) return false;
            var body = message.Body.ToLower();

            return bodyParts.All(bodyPart => body.Contains(bodyPart.ToLower()));
        }
    }
}
