using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Guts.Business.Communication;
using Guts.Business.Dtos.Gamification;
using Guts.Common.Extensions;
using Guts.Domain.UserAggregate;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Communication;

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
        _user = new User
        {
            Id = new Random().NextPositive(),
            FirstName = "Alice",
            Email = "someUserEmailAddress@test.com"
        };
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

    [Test]
    public void SendStreakReminderEmailAsync_ShouldSendPersonalizedMailWithStreakAndCourseData()
    {
        //Arrange
        string courseName = "Object-Oriented Programming";
        int courseId = 42;
        int currentStreak = 3;
        string targetStudentName = "Bob Builder";
        var recommendations = new List<StreakReminderAssignmentDto>
        {
            new()
            {
                AssignmentId = 101,
                TopicDescription = "Chapter 2: Classes",
                AssignmentDescription = "Exercise 2.1 Bank Account",
                GreenTestsCount = 2,
                TotalTestsCount = 5,
                WebUrl = "http://someWebAppUrl.com/courses/42/chapters/Ch2/testresults?assignmentId=101"
            }
        };

        //Act
        _mailSender.SendStreakReminderEmailAsync(_user, courseName, courseId, currentStreak, targetStudentName, recommendations).Wait();

        //Assert
        _smtpClientMock.Verify(client => client.SendMailAsync(It.Is((MailMessage message) =>
            message.Subject.Contains(courseName) &&
            message.To.Any(t => t.Address == _user.Email) &&
            VerifyMailMessage(
                message,
                _fromEmail,
                "Alice",
                courseName,
                "3",
                "Bob Builder",
                "Chapter 2: Classes",
                "Exercise 2.1 Bank Account",
                "courses/42/gamification"))), Times.Once);
    }

    [Test]
    public void SendStreakReminderEmailAsync_WhenFirstNameIsNull_ShouldFallbackToStudentGreeting()
    {
        //Arrange
        _user.FirstName = null;
        string courseName = "DotNet Advanced";
        int courseId = 10;
        int currentStreak = 1;

        //Act
        _mailSender.SendStreakReminderEmailAsync(_user, courseName, courseId, currentStreak, null, Array.Empty<StreakReminderAssignmentDto>()).Wait();

        //Assert
        _smtpClientMock.Verify(client => client.SendMailAsync(It.Is((MailMessage message) =>
            message.Subject.Contains(courseName) &&
            VerifyMailMessage(message, _fromEmail, "Dear student", courseName, "courses/10/gamification"))), Times.Once);
    }

    [Test]
    public void SendStreakReminderEmailAsync_WhenTargetStudentIsNull_ShouldNotIncludeTargetCallout()
    {
        //Arrange
        string courseName = "DotNet Advanced";
        int courseId = 10;
        int currentStreak = 2;

        //Act
        _mailSender.SendStreakReminderEmailAsync(_user, courseName, courseId, currentStreak, null, Array.Empty<StreakReminderAssignmentDto>()).Wait();

        //Assert
        _smtpClientMock.Verify(client => client.SendMailAsync(It.Is((MailMessage message) =>
            !message.Body.Contains("overtaking", StringComparison.OrdinalIgnoreCase))), Times.Once);
    }

    private static bool VerifyMailMessage(MailMessage message, string fromEmail, params string[] bodyParts)
    {
        if (!string.Equals(message.From.Address, fromEmail, StringComparison.OrdinalIgnoreCase)) return false;
        var body = message.Body;

        return bodyParts.All(bodyPart => body.Contains(bodyPart, StringComparison.OrdinalIgnoreCase));
    }
}
