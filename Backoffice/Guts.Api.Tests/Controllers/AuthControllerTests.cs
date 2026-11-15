using Guts.Api.Controllers;
using Guts.Api.Models;
using Guts.Api.Tests.Builders;
using Guts.Application.Captcha;
using Guts.Application.Communication;
using Guts.Application.Security;
using Guts.Application.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Guts.Application;
using Guts.Domain.LoginSessionAggregate;
using Guts.Domain.RoleAggregate;
using Guts.Domain.UserAggregate;

namespace Guts.Api.Tests.Controllers;

[TestFixture]
public class AuthControllerTests
{
    private AuthController _controller;
    private Mock<UserManager<User>> _userManagerMock;
    private Mock<ICaptchaValidator> _captchaValidatorMock;
    private Mock<IMailSender> _mailSenderMock;
    private Mock<IPasswordHasher<User>> _passwordHasherMock;
    private Mock<ITokenAccessPassFactory> _accessPassFactoryMock;
    private Mock<ILoginSessionService> _loginSessionServiceMock;

    [SetUp]
    public void Setup()
    {
        var userStoreMock = new Mock<IUserStore<User>>();
        _passwordHasherMock = new();
        var lookupNormalizerMock = new Mock<ILookupNormalizer>();
        var errorsMock = new Mock<IdentityErrorDescriber>();
        var userManagerLoggerMock = new Mock<ILogger<UserManager<User>>>();

        _userManagerMock = new(
            userStoreMock.Object,
            null,
            _passwordHasherMock.Object,
            null,
            null,
            lookupNormalizerMock.Object,
            errorsMock.Object,
            null,
            userManagerLoggerMock.Object);

        _userManagerMock.Setup(manager => manager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(new IdentityResult());
        _userManagerMock.Setup(manager => manager.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        var expectedCaptchaResult = new CaptchaVerificationResult { Success = true };
        _captchaValidatorMock = new();
        _captchaValidatorMock.Setup(validator => validator.Validate(It.IsAny<string>(), It.IsAny<IPAddress>()))
            .ReturnsAsync(expectedCaptchaResult);

        _mailSenderMock = new();

        _accessPassFactoryMock = new();

        _loginSessionServiceMock = new();

        var controllerLoggerMock = new Mock<ILogger<AuthController>>();

        _controller = new(_userManagerMock.Object,
            _captchaValidatorMock.Object,
            _mailSenderMock.Object,
            _accessPassFactoryMock.Object,
            _loginSessionServiceMock.Object,
            controllerLoggerMock.Object);

        var context = new ControllerContextBuilder().WithClientIp().Build();
        _controller.ControllerContext = context;
    }

    [Test]
    public void RegisterShouldReturnBadRequestIfModelIsInvalid()
    {
        //Arrange
        var model = new RegisterModelBuilder().Build();

        var errorKey = "someValidationError";
        _controller.ModelState.AddModelError(errorKey, Guid.NewGuid().ToString());

        //Act
        var result = _controller.Register(model).Result as BadRequestObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        var serializableError = result.Value as SerializableError;
        Assert.That(serializableError, Is.Not.Null);
        Assert.That(serializableError.Keys, Has.One.EqualTo(errorKey));
        _userManagerMock.Verify(manager => manager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _mailSenderMock.Verify(sender => sender.SendConfirmUserEmailMessageAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    [TestCase("invalid@nottrusted.be")]
    [TestCase("invalid@fakepxl.be")]
    public void RegisterShouldReturnBadRequestForNonPxlEmailAddress(string invalidEmail)
    {
        //Arrange
        var model = new RegisterModelBuilder().WithEmail(invalidEmail).Build();

        //Act
        var result = _controller.Register(model).Result as BadRequestObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Does.Contain("PXL").IgnoreCase);
        _userManagerMock.Verify(manager => manager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _mailSenderMock.Verify(sender => sender.SendConfirmUserEmailMessageAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void RegisterShouldCreateUserAndSendConfirmationMail()
    {
        //Arrange
        var model = new RegisterModelBuilder().WithValidStudentEmail().Build();

        var confirmationToken = Guid.NewGuid().ToString();
        _userManagerMock.Setup(manager => manager.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync(confirmationToken);

        var identityResult = IdentityResult.Success;
        _userManagerMock.Setup(manager => manager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(identityResult);

        //Act
        var result = _controller.Register(model).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);

        _userManagerMock.Verify(
            manager => manager.CreateAsync(
                It.Is<User>(user => user.Email == model.Email && user.UserName == model.Email), model.Password),
            Times.Once);

        _userManagerMock.Verify(
            manager => manager.GenerateEmailConfirmationTokenAsync(It.Is<User>(user => user.Email == model.Email)),
            Times.Once);

        _mailSenderMock.Verify(sender => sender.SendConfirmUserEmailMessageAsync(It.Is<User>(user => user.Email == model.Email), confirmationToken), Times.Once);
    }

    [Test]
    public void RegisterShouldAssignStudentRoleForStudentPxlEmailAddress()
    {
        //Arrange
        var model = new RegisterModelBuilder().WithValidStudentEmail().Build();

        var identityResult = IdentityResult.Success;
        _userManagerMock.Setup(manager => manager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(identityResult);

        //Act
        var result = _controller.Register(model).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _userManagerMock.Verify(manager => manager.AddToRoleAsync(It.Is<User>(user => user.Email == model.Email), Role.Constants.Student), Times.Once);
    }

    [Test]
    public void RegisterShouldAssignLectorRoleForPxlEmailAddress()
    {
        //Arrange
        var model = new RegisterModelBuilder().WithValidLectorEmail().Build();

        var identityResult = IdentityResult.Success;
        _userManagerMock.Setup(manager => manager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(identityResult);

        //Act
        var result = _controller.Register(model).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _userManagerMock.Verify(manager => manager.AddToRoleAsync(It.Is<User>(user => user.Email == model.Email), Role.Constants.Lector), Times.Once);
    }

    [Test]
    public void RegisterShouldReturnBadRequestIfCreatingUserFails()
    {
        //Arrange
        var model = new RegisterModelBuilder().WithValidStudentEmail().Build();

        var errorCode = Guid.NewGuid().ToString();
        var identityResult = IdentityResult.Failed(new IdentityError
        {
            Code = errorCode,
            Description = Guid.NewGuid().ToString()
        });

        _userManagerMock.Setup(manager => manager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(identityResult);

        //Act
        var result = _controller.Register(model).Result as BadRequestObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);

        var serializableError = result.Value as SerializableError;
        Assert.That(serializableError, Is.Not.Null);
        Assert.That(serializableError.Keys, Has.One.EqualTo(errorCode));

        _userManagerMock.Verify(
            manager => manager.CreateAsync(
                It.Is<User>(user => user.Email == model.Email && user.UserName == model.Email), model.Password),
            Times.Once);
        _mailSenderMock.Verify(sender => sender.SendConfirmUserEmailMessageAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void RegisterShouldValidateCaptcha()
    {
        //Arrange
        var model = new RegisterModelBuilder().WithValidStudentEmail().Build();

        //Act
        var result = _controller.Register(model).Result;

        //Assert
        Assert.That(result, Is.Not.Null);
        _captchaValidatorMock.Verify(validator => validator.Validate(model.CaptchaToken, _controller.HttpContext.Connection.RemoteIpAddress));
    }

    [Test]
    public void RegisterShouldReturnBadRequestIfCaptchaIsInvalid()
    {
        //Arrange
        var model = new RegisterModelBuilder().WithValidStudentEmail().Build();
        var expectedCaptchaResult = new CaptchaVerificationResult { Success = false };
        _captchaValidatorMock.Setup(validator => validator.Validate(It.IsAny<string>(), It.IsAny<IPAddress>()))
            .ReturnsAsync(expectedCaptchaResult);

        //Act
        var result = _controller.Register(model).Result as BadRequestObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _captchaValidatorMock.Verify(validator => validator.Validate(model.CaptchaToken, _controller.HttpContext.Connection.RemoteIpAddress));
        _userManagerMock.Verify(manager => manager.CreateAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _mailSenderMock.Verify(sender => sender.SendConfirmUserEmailMessageAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void CreateTokenShoudReturnBadRequestIfModelIsInvalid()
    {
        //Arrange
        var model = new LoginModelBuilder().Build();

        var errorKey = "someValidationError";
        _controller.ModelState.AddModelError(errorKey, Guid.NewGuid().ToString());

        //Act
        var result = _controller.CreateToken(model).Result as BadRequestObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        var serializableError = result.Value as SerializableError;
        Assert.That(serializableError, Is.Not.Null);
        Assert.That(serializableError.Keys, Has.One.EqualTo(errorKey));

        _userManagerMock.Verify(manager => manager.FindByNameAsync(It.IsAny<string>()), Times.Never);
        _mailSenderMock.Verify(sender => sender.SendConfirmUserEmailMessageAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _passwordHasherMock.Verify(hasher => hasher.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void CreateTokenShoudReturnUnAuthorizedIfUserIsNotFound()
    {
        //Arrange
        var model = new LoginModelBuilder().Build();
        _userManagerMock.Setup(manager => manager.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(() => null);

        //Act
        var result = _controller.CreateToken(model).Result as UnauthorizedResult;

        //Assert
        Assert.That(result, Is.Not.Null);

        _userManagerMock.Verify(manager => manager.FindByNameAsync(model.Email), Times.Once);
        _mailSenderMock.Verify(sender => sender.SendConfirmUserEmailMessageAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _passwordHasherMock.Verify(hasher => hasher.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void CreateTokenShoudReturnUnAuthorizedIfPasswordsDontMatch()
    {
        //Arrange
        var model = new LoginModelBuilder().Build();

        var existingUser = new User
        {
            PasswordHash = Guid.NewGuid().ToString()
        };
        _userManagerMock.Setup(manager => manager.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

        _userManagerMock
            .Setup(manager => manager.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        //Act
        var result = _controller.CreateToken(model).Result as UnauthorizedResult;

        //Assert
        Assert.That(result, Is.Not.Null);

        _userManagerMock.Verify(manager => manager.FindByNameAsync(model.Email), Times.Once);
        _userManagerMock.Verify(manager => manager.CheckPasswordAsync(existingUser, model.Password), Times.Once);
        _mailSenderMock.Verify(sender => sender.SendConfirmUserEmailMessageAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);

    }

    [Test]
    public void CreateTokenShoudReturnBadRequestAndSendConfirmationMailWhenEmailIsNotConfirmed()
    {
        //Arrange
        var model = new LoginModelBuilder().Build();

        var existingUser = new User
        {
            EmailConfirmed = false
        };
        _userManagerMock.Setup(manager => manager.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

        var confirmationToken = Guid.NewGuid().ToString();
        _userManagerMock.Setup(manager => manager.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync(confirmationToken);

        _userManagerMock
            .Setup(manager => manager.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        //Act
        var result = _controller.CreateToken(model).Result as BadRequestObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        var serializableError = result.Value as SerializableError;
        Assert.That(serializableError, Is.Not.Null);
        Assert.That(serializableError.Keys, Has.One.EqualTo("EmailNotConfirmed"));

        _userManagerMock.Verify(manager => manager.FindByNameAsync(model.Email), Times.Once);
        _userManagerMock.Verify(manager => manager.CheckPasswordAsync(existingUser, model.Password), Times.Once);
        _mailSenderMock.Verify(sender => sender.SendConfirmUserEmailMessageAsync(existingUser, confirmationToken), Times.Once);
    }

    [Test]
    public void CreateToken_ShoudReturnOkResultWithTokenIfCredentialsAreCorrect()
    {
        //Arrange
        var model = new LoginModelBuilder().Build();

        var existingUser = new User
        {
            EmailConfirmed = true
        };
        _userManagerMock.Setup(manager => manager.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

        var confirmationToken = Guid.NewGuid().ToString();
        _userManagerMock.Setup(manager => manager.GenerateEmailConfirmationTokenAsync(It.IsAny<User>()))
            .ReturnsAsync(confirmationToken);

        _userManagerMock
            .Setup(manager => manager.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var createdAccesPass = new TokenAccessPass
        {
            Token = Guid.NewGuid().ToString()
        };
        _accessPassFactoryMock
            .Setup(factory => factory.Create(It.IsAny<User>(), It.IsAny<IList<Claim>>(), It.IsAny<IList<string>>()))
            .Returns(createdAccesPass);

        var existingClaims = new List<Claim>();
        _userManagerMock.Setup(manager => manager.GetClaimsAsync(It.IsAny<User>())).ReturnsAsync(existingClaims);

        var existingRoles = new List<string>();
        _userManagerMock.Setup(manager => manager.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(existingRoles);

        //Act
        var result = _controller.CreateToken(model).Result as OkObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);

        _userManagerMock.Verify(manager => manager.FindByNameAsync(model.Email), Times.Once);
        _userManagerMock.Verify(manager => manager.CheckPasswordAsync(existingUser, model.Password), Times.Once);
        _mailSenderMock.Verify(sender => sender.SendConfirmUserEmailMessageAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(manager => manager.GetClaimsAsync(existingUser), Times.Once);
        _accessPassFactoryMock.Verify(factory => factory.Create(existingUser, existingClaims, existingRoles), Times.Once);

        _loginSessionServiceMock.Verify(
            service => service.SetLoginTokenForSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);

        Assert.That(result.Value, Is.SameAs(createdAccesPass));
    }

    [Test]
    public void CreateToken_ShoudSaveTokenToSessionIfASessionIsProvided()
    {
        //Arrange
        var model = new LoginModelBuilder().WithSession().Build();

        var existingUser = new User
        {
            EmailConfirmed = true
        };
        _userManagerMock.Setup(manager => manager.FindByNameAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

        _userManagerMock
            .Setup(manager => manager.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var createdAccesPass = new TokenAccessPass
        {
            Token = Guid.NewGuid().ToString()
        };

        _accessPassFactoryMock
            .Setup(factory => factory.Create(It.IsAny<User>(), It.IsAny<IList<Claim>>(), It.IsAny<IList<string>>()))
            .Returns(createdAccesPass);

        //Act
        var result = _controller.CreateToken(model).Result as OkObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);

        _loginSessionServiceMock.Verify(
            service => service.SetLoginTokenForSessionAsync(model.LoginSessionPublicIdentifier,
                createdAccesPass.Token), Times.Once);

        Assert.That(result.Value, Is.SameAs(createdAccesPass));
    }

    [Test]
    public void ConfirmEmailShouldReturnBadRequestIfModelIsInvalid()
    {
        //Arrange
        var model = new ConfirmEmailModelBuilder().Build();

        var errorKey = "someValidationError";
        _controller.ModelState.AddModelError(errorKey, Guid.NewGuid().ToString());

        //Act
        var result = _controller.ConfirmEmail(model).Result as BadRequestObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        var serializableError = result.Value as SerializableError;
        Assert.That(serializableError, Is.Not.Null);
        Assert.That(serializableError.Keys, Has.One.EqualTo(errorKey));
        _userManagerMock.Verify(manager => manager.FindByIdAsync(It.IsAny<string>()), Times.Never);
        _userManagerMock.Verify(manager => manager.ConfirmEmailAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void ConfirmEmailShouldReturnOkIfUserCannotBeFound()
    {
        //Arrange
        var model = new ConfirmEmailModelBuilder().Build();
        _userManagerMock.Setup(manager => manager.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(() => null);

        //Act
        var result = _controller.ConfirmEmail(model).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _userManagerMock.Verify(manager => manager.FindByIdAsync(model.UserId), Times.Once);
        _userManagerMock.Verify(manager => manager.ConfirmEmailAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void ConfirmEmailShouldReturnBadRequestIfConfirmationFails()
    {
        //Arrange
        var model = new ConfirmEmailModelBuilder().Build();
        var existingUser = new User();
        var errorCode = Guid.NewGuid().ToString();
        var identityResult = IdentityResult.Failed(new IdentityError
        {
            Code = errorCode,
            Description = Guid.NewGuid().ToString()
        });

        _userManagerMock.Setup(manager => manager.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(() => existingUser);
        _userManagerMock.Setup(manager => manager.ConfirmEmailAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(identityResult);

        //Act
        var result = _controller.ConfirmEmail(model).Result as BadRequestObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);

        var serializableError = result.Value as SerializableError;
        Assert.That(serializableError, Is.Not.Null);
        Assert.That(serializableError.Keys, Has.One.EqualTo(errorCode));

        _userManagerMock.Verify(manager => manager.FindByIdAsync(model.UserId), Times.Once);
        _userManagerMock.Verify(manager => manager.ConfirmEmailAsync(existingUser, model.Token), Times.Once);
    }

    [Test]
    public void ConfirmEmailShouldReturnOkIfConfirmationSucceeds()
    {
        //Arrange
        var model = new ConfirmEmailModelBuilder().Build();
        var existingUser = new User();

        _userManagerMock.Setup(manager => manager.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(() => existingUser);
        _userManagerMock.Setup(manager => manager.ConfirmEmailAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        //Act
        var result = _controller.ConfirmEmail(model).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(existingUser.LastVerifiedOnUtc, Is.Not.Null);
        _userManagerMock.Verify(manager => manager.FindByIdAsync(model.UserId), Times.Once);
        _userManagerMock.Verify(manager => manager.ConfirmEmailAsync(existingUser, model.Token), Times.Once);
        _userManagerMock.Verify(manager => manager.UpdateAsync(existingUser), Times.Once);
    }

    [Test]
    public void SendForgotPasswordMailShouldReturnBadRequestIfModelIsInvalid()
    {
        //Arrange
        var model = new ForgotPasswordModelBuilder().Build();

        var errorKey = "someValidationError";
        _controller.ModelState.AddModelError(errorKey, Guid.NewGuid().ToString());

        //Act
        var result = _controller.SendForgotPasswordMail(model).Result as BadRequestObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        var serializableError = result.Value as SerializableError;
        Assert.That(serializableError, Is.Not.Null);
        Assert.That(serializableError.Keys, Has.One.EqualTo(errorKey));
        _userManagerMock.Verify(manager => manager.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        _mailSenderMock.Verify(sender => sender.SendForgotPasswordMessageAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void SendForgotPasswordMailShouldReturnOkResultIfUserCannotBeFound()
    {
        //Arrange
        var model = new ForgotPasswordModelBuilder().Build();

        _userManagerMock.Setup(manager => manager.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(() => null);

        //Act
        var result = _controller.SendForgotPasswordMail(model).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _userManagerMock.Verify(manager => manager.FindByEmailAsync(model.Email), Times.Once);
        _mailSenderMock.Verify(sender => sender.SendForgotPasswordMessageAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void SendForgotPasswordMailShouldReturnOkResultIfUserEmailIsNotYetConfirmed()
    {
        //Arrange
        var model = new ForgotPasswordModelBuilder().Build();

        var existingUser = new User
        {
            EmailConfirmed = false
        };
        _userManagerMock.Setup(manager => manager.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

        //Act
        var result = _controller.SendForgotPasswordMail(model).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _userManagerMock.Verify(manager => manager.FindByEmailAsync(model.Email), Times.Once);
        _mailSenderMock.Verify(sender => sender.SendForgotPasswordMessageAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void SendForgotPasswordMailShouldReturnBadRequestIfCaptchaIsInvalid()
    {
        //Arrange
        var model = new ForgotPasswordModelBuilder().WithValidEmail().Build();
        var expectedCaptchaResult = new CaptchaVerificationResult { Success = false };
        _captchaValidatorMock.Setup(validator => validator.Validate(It.IsAny<string>(), It.IsAny<IPAddress>()))
            .ReturnsAsync(expectedCaptchaResult);

        //Act
        var result = _controller.SendForgotPasswordMail(model).Result as BadRequestObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _captchaValidatorMock.Verify(validator => validator.Validate(model.CaptchaToken, _controller.HttpContext.Connection.RemoteIpAddress));
        _userManagerMock.Verify(manager => manager.FindByEmailAsync(It.IsAny<string>()), Times.Never);
        _mailSenderMock.Verify(sender => sender.SendConfirmUserEmailMessageAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void SendForgotPasswordMailShouldCreatePasswordTokenAndSendConfirmationMail()
    {
        //Arrange
        var model = new ForgotPasswordModelBuilder().WithValidEmail().Build();

        var existingUser = new User
        {
            Email = model.Email,
            EmailConfirmed = true
        };
        _userManagerMock.Setup(manager => manager.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

        var resetPasswordToken = Guid.NewGuid().ToString();
        _userManagerMock.Setup(manager => manager.GeneratePasswordResetTokenAsync(It.IsAny<User>()))
            .ReturnsAsync(resetPasswordToken);

        //Act
        var result = _controller.SendForgotPasswordMail(model).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);

        _userManagerMock.Verify(manager => manager.FindByEmailAsync(model.Email), Times.Once);
        _mailSenderMock.Verify(sender => sender.SendForgotPasswordMessageAsync(existingUser, resetPasswordToken), Times.Once);
    }

    [Test]
    public void ResetPasswordShouldReturnBadRequestIfModelIsInvalid()
    {
        //Arrange
        var model = new ResetPasswordModelBuilder().Build();

        var errorKey = "someValidationError";
        _controller.ModelState.AddModelError(errorKey, Guid.NewGuid().ToString());

        //Act
        var result = _controller.ResetPassword(model).Result as BadRequestObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        var serializableError = result.Value as SerializableError;
        Assert.That(serializableError, Is.Not.Null);
        Assert.That(serializableError.Keys, Has.One.EqualTo(errorKey));
        _userManagerMock.Verify(manager => manager.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void ResetPasswordShouldReturnOkIfPasswordCouldBeChanged()
    {
        //Arrange
        var model = new ResetPasswordModelBuilder().Build();

        var existingUser = new User();
        _userManagerMock.Setup(manager => manager.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

        _userManagerMock
            .Setup(mangager =>
                mangager.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        //Act
        var result = _controller.ResetPassword(model).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(existingUser.LastVerifiedOnUtc, Is.Not.Null);
        _userManagerMock.Verify(manager => manager.FindByIdAsync(model.UserId), Times.Once);
        _userManagerMock.Verify(manager => manager.ResetPasswordAsync(existingUser, model.Token, model.Password), Times.Once);
        _userManagerMock.Verify(manager => manager.UpdateAsync(existingUser), Times.Once);
    }

    [Test]
    public void ResetPasswordShouldReturnOkIfUserCouldNotBeFound()
    {
        //Arrange
        var model = new ResetPasswordModelBuilder().Build();

        _userManagerMock.Setup(manager => manager.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(() => null);

        //Act
        var result = _controller.ResetPassword(model).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _userManagerMock.Verify(manager => manager.FindByIdAsync(model.UserId), Times.Once);
        _userManagerMock.Verify(manager => manager.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void ResetPasswordShouldReturnBadRequestIfPasswordCouldNotBeChanged()
    {
        //Arrange
        var model = new ResetPasswordModelBuilder().Build();

        var existingUser = new User();
        _userManagerMock.Setup(manager => manager.FindByIdAsync(It.IsAny<string>())).ReturnsAsync(existingUser);

        var errorCode = Guid.NewGuid().ToString();
        var identityResult = IdentityResult.Failed(new IdentityError
        {
            Code = errorCode,
            Description = Guid.NewGuid().ToString()
        });
        _userManagerMock
            .Setup(mangager =>
                mangager.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(identityResult);

        //Act
        var result = _controller.ResetPassword(model).Result as BadRequestObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        var serializableError = result.Value as SerializableError;
        Assert.That(serializableError, Is.Not.Null);
        Assert.That(serializableError.Keys, Has.One.EqualTo(errorCode));

        _userManagerMock.Verify(manager => manager.FindByIdAsync(model.UserId), Times.Once);
        _userManagerMock.Verify(manager => manager.ResetPasswordAsync(existingUser, model.Token, model.Password), Times.Once);
    }

    [Test]
    public void ChangePasswordShouldReturnBadRequestIfModelIsInvalid()
    {
        //Arrange
        var model = new ChangePasswordModelBuilder().Build();

        var errorKey = "someValidationError";
        _controller.ModelState.AddModelError(errorKey, Guid.NewGuid().ToString());

        //Act
        var result = _controller.ChangePassword(model).Result as BadRequestObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        var serializableError = result.Value as SerializableError;
        Assert.That(serializableError, Is.Not.Null);
        Assert.That(serializableError.Keys, Has.One.EqualTo(errorKey));
        _userManagerMock.Verify(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
        _userManagerMock.Verify(manager => manager.ChangePasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void ChangePasswordShouldReturnOkIfPasswordCouldBeChanged()
    {
        //Arrange
        var model = new ChangePasswordModelBuilder().Build();

        var existingUser = new User();
        _userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(existingUser);
        _userManagerMock
            .Setup(manager => manager.ChangePasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        //Act
        var result = _controller.ChangePassword(model).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _userManagerMock.Verify(manager => manager.GetUserAsync(_controller.User), Times.Once);
        _userManagerMock.Verify(manager => manager.ChangePasswordAsync(existingUser, model.CurrentPassword, model.NewPassword), Times.Once);
    }

    [Test]
    public void ChangePasswordShouldReturnBadRequestIfChangingPasswordFails()
    {
        //Arrange
        var model = new ChangePasswordModelBuilder().Build();

        var existingUser = new User();
        _userManagerMock.Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(existingUser);

        var errorCode = Guid.NewGuid().ToString();
        var identityResult = IdentityResult.Failed(new IdentityError
        {
            Code = errorCode,
            Description = Guid.NewGuid().ToString()
        });

        _userManagerMock
            .Setup(manager => manager.ChangePasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(identityResult);

        //Act
        var result = _controller.ChangePassword(model).Result as BadRequestObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        var serializableError = result.Value as SerializableError;
        Assert.That(serializableError, Is.Not.Null);
        Assert.That(serializableError.Keys, Has.One.EqualTo(errorCode));

        _userManagerMock.Verify(manager => manager.GetUserAsync(_controller.User), Times.Once);
        _userManagerMock.Verify(manager => manager.ChangePasswordAsync(existingUser, model.CurrentPassword, model.NewPassword), Times.Once);
    }

    [Test]
    public void CreateLoginSession_ShouldCreateASessionUsingRemoteIpOfClient()
    {
        //Arrange
        var createdSession = new LoginSession();
        _loginSessionServiceMock.Setup(service => service.CreateSessionAsync(It.IsAny<string>())).ReturnsAsync(createdSession);

        var clientIp = "123.1.123.0";
        var context = new ControllerContextBuilder().WithClientIp(clientIp).Build();
        _controller.ControllerContext = context;

        //Act
        var result = _controller.CreateLoginSession().Result as OkObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.SameAs(createdSession));
        _loginSessionServiceMock.Verify(service => service.CreateSessionAsync(clientIp), Times.Once);
    }

    [Test]
    public void CreateLoginSession_ShouldCleanUpOldSessions()
    {
        //Act
        _controller.CreateLoginSession().Wait();

        //Assert
        _loginSessionServiceMock.Verify(service => service.CleanUpOldSessionsAsync(), Times.Once);
    }

    [Test]
    public void RetrieveLoginSession_ShouldReturnSessionWhenSecretAndClientIpMatch()
    {
        //Arrange
        var clientIp = "100.1.123.0";
        var context = new ControllerContextBuilder().WithClientIp(clientIp).Build();
        _controller.ControllerContext = context;

        var existingSession = new LoginSession
        {
            PublicIdentifier = Guid.NewGuid().ToString(),
            IpAddress = clientIp,
            SessionToken = Guid.NewGuid().ToString()
        };

        _loginSessionServiceMock.Setup(service => service.GetSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(existingSession);

        //Act
        var result = _controller.RetrieveLoginSession(existingSession.PublicIdentifier, existingSession.SessionToken).Result as ObjectResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.SameAs(existingSession));
        _loginSessionServiceMock.Verify(service => service.GetSessionAsync(existingSession.PublicIdentifier, clientIp, existingSession.SessionToken), Times.Once);
    }

    [Test]
    public void RetrieveLoginSession_ShouldReturnBadRequestWhenSessionCannotBeFound()
    {
        //Arrange
        var clientIp = "100.1.123.0";
        var context = new ControllerContextBuilder().WithClientIp(clientIp).Build();
        _controller.ControllerContext = context;

        var somePublicIdentifier = Guid.NewGuid().ToString();
        var someSecretToken = Guid.NewGuid().ToString();

        _loginSessionServiceMock.Setup(service => service.GetSessionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Throws<DataNotFoundException>();

        //Act
        var result = _controller.RetrieveLoginSession(somePublicIdentifier, someSecretToken).Result as BadRequestResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _loginSessionServiceMock.Verify(service => service.GetSessionAsync(somePublicIdentifier, clientIp, someSecretToken), Times.Once);
    }

    [Test]
    public void CancelLoginSession_ShouldCancelSessionIfFound()
    {
        //Arrange
        var clientIp = "200.1.123.0";
        var context = new ControllerContextBuilder().WithClientIp(clientIp).Build();
        _controller.ControllerContext = context;

        var sessionPublicIdentifier = Guid.NewGuid().ToString();

        //Act
        var result = _controller.CancelLoginSession(sessionPublicIdentifier).Result as OkResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _loginSessionServiceMock.Verify(service => service.CancelSessionAsync(sessionPublicIdentifier, clientIp), Times.Once);
    }

    [Test]
    public void CancelLoginSession_ShouldReturnBadRequestWhenSessionCannotBeFound()
    {
        //Arrange
        var clientIp = "100.1.123.0";
        var context = new ControllerContextBuilder().WithClientIp(clientIp).Build();
        _controller.ControllerContext = context;

        var somePublicIdentifier = Guid.NewGuid().ToString();

        _loginSessionServiceMock.Setup(service => service.CancelSessionAsync(It.IsAny<string>(), It.IsAny<string>())).Throws<DataNotFoundException>();

        //Act
        var result = _controller.CancelLoginSession(somePublicIdentifier).Result as BadRequestResult;

        //Assert
        Assert.That(result, Is.Not.Null);
        _loginSessionServiceMock.Verify(service => service.CancelSessionAsync(somePublicIdentifier, clientIp), Times.Once);
    }

    [TestCase(Role.Constants.Lector)]
    [TestCase(Role.Constants.Lector, Role.Constants.Admin)]
    public async Task CreateToken_LockedLectorWithValidPassword_ReturnsForbiddenSendsEmailAndSkipsLoginSession(params string[] roles)
    {
        // Arrange
        var model = new LoginModelBuilder().WithSession().Build();
        var lockedLector = new User
        {
            Id = 42,
            Email = "lector@pxl.be",
            UserName = "lector@pxl.be",
            EmailConfirmed = true
        };
        _userManagerMock.Setup(m => m.FindByNameAsync(model.Email)).ReturnsAsync(lockedLector);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(lockedLector, model.Password)).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(lockedLector)).ReturnsAsync([.. roles]);
        _userManagerMock.Setup(m => m.GenerateUserTokenAsync(
                lockedLector,
                TokenOptions.DefaultProvider,
                AuthController.LectorVerificationTokenPurpose))
            .ReturnsAsync("verify-token-123");
        _userManagerMock.Setup(m => m.GenerateUserTokenAsync(
                lockedLector,
                TokenOptions.DefaultProvider,
                AuthController.LectorVerificationResendTokenPurpose))
            .ReturnsAsync("resend-token-456");

        // Act
        var result = await _controller.CreateToken(model) as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo((int)HttpStatusCode.Forbidden));
        var payload = result.Value as LectorVerificationRequiredModel;
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload.Code, Is.EqualTo("LectorVerificationRequired"));
        Assert.That(payload.UserId, Is.EqualTo(42));
        Assert.That(payload.Email, Is.EqualTo("lector@pxl.be"));
        Assert.That(payload.ResendToken, Is.EqualTo("resend-token-456"));

        _mailSenderMock.Verify(s => s.SendLectorVerificationEmailMessageAsync(lockedLector, "verify-token-123"), Times.Once);
        _accessPassFactoryMock.Verify(f => f.Create(It.IsAny<User>(), It.IsAny<IList<Claim>>(), It.IsAny<IList<string>>()), Times.Never);
        _loginSessionServiceMock.Verify(s => s.SetLoginTokenForSessionAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task CreateToken_LockedLectorWithWrongPassword_ReturnsUnauthorizedWithoutSendingEmail()
    {
        // Arrange
        var model = new LoginModelBuilder().Build();
        var lockedLector = new User { Id = 42, Email = "lector@pxl.be", EmailConfirmed = true };
        _userManagerMock.Setup(m => m.FindByNameAsync(model.Email)).ReturnsAsync(lockedLector);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(lockedLector, model.Password)).ReturnsAsync(false);
        _userManagerMock.Setup(m => m.GetRolesAsync(lockedLector)).ReturnsAsync([Role.Constants.Lector]);

        // Act
        var result = await _controller.CreateToken(model) as UnauthorizedResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        _mailSenderMock.Verify(s => s.SendLectorVerificationEmailMessageAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task CreateToken_RecentlyVerifiedLector_ReturnsOkWithToken()
    {
        // Arrange
        var model = new LoginModelBuilder().Build();
        var verifiedLector = new User { Id = 42, Email = "lector@pxl.be", EmailConfirmed = true };
        verifiedLector.MarkEmailVerified(DateTime.UtcNow.AddDays(-10));
        var accessPass = new TokenAccessPass { Token = "jwt-token" };

        _userManagerMock.Setup(m => m.FindByNameAsync(model.Email)).ReturnsAsync(verifiedLector);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(verifiedLector, model.Password)).ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(verifiedLector)).ReturnsAsync([Role.Constants.Lector]);
        _userManagerMock.Setup(m => m.GetClaimsAsync(verifiedLector)).ReturnsAsync([]);
        _accessPassFactoryMock.Setup(f => f.Create(verifiedLector, It.IsAny<IList<Claim>>(), It.IsAny<IList<string>>())).Returns(accessPass);

        // Act
        var result = await _controller.CreateToken(model) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.SameAs(accessPass));
        _mailSenderMock.Verify(s => s.SendLectorVerificationEmailMessageAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task ResendLectorVerification_ValidResendToken_SendsFreshEmailAndReturnsOk()
    {
        // Arrange
        var lockedLector = new User { Id = 42, Email = "lector@pxl.be", EmailConfirmed = true };
        var model = new ResendLectorVerificationModel { UserId = "42", ResendToken = "resend-token" };
        _userManagerMock.Setup(m => m.FindByIdAsync("42")).ReturnsAsync(lockedLector);
        _userManagerMock.Setup(m => m.VerifyUserTokenAsync(
                lockedLector,
                TokenOptions.DefaultProvider,
                AuthController.LectorVerificationResendTokenPurpose,
                "resend-token"))
            .ReturnsAsync(true);
        _userManagerMock.Setup(m => m.GetRolesAsync(lockedLector)).ReturnsAsync([Role.Constants.Lector]);
        _userManagerMock.Setup(m => m.GenerateUserTokenAsync(
                lockedLector,
                TokenOptions.DefaultProvider,
                AuthController.LectorVerificationTokenPurpose))
            .ReturnsAsync("fresh-verify-token");

        // Act
        var result = await _controller.ResendLectorVerification(model) as OkResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        _mailSenderMock.Verify(s => s.SendLectorVerificationEmailMessageAsync(lockedLector, "fresh-verify-token"), Times.Once);
    }

    [Test]
    public async Task ResendLectorVerification_InvalidResendToken_ReturnsBadRequest()
    {
        // Arrange
        var lockedLector = new User { Id = 42, Email = "lector@pxl.be", EmailConfirmed = true };
        var model = new ResendLectorVerificationModel { UserId = "42", ResendToken = "bad-token" };
        _userManagerMock.Setup(m => m.FindByIdAsync("42")).ReturnsAsync(lockedLector);
        _userManagerMock.Setup(m => m.VerifyUserTokenAsync(
                lockedLector,
                TokenOptions.DefaultProvider,
                AuthController.LectorVerificationResendTokenPurpose,
                "bad-token"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ResendLectorVerification(model) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        _mailSenderMock.Verify(s => s.SendLectorVerificationEmailMessageAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task VerifyLector_ValidToken_MarksEmailVerifiedAndConfirmedAndReturnsOk()
    {
        // Arrange
        var lockedLector = new User { Id = 42, Email = "lector@pxl.be", EmailConfirmed = false };
        var model = new VerifyLectorModel { UserId = "42", Token = "verify-token" };
        _userManagerMock.Setup(m => m.FindByIdAsync("42")).ReturnsAsync(lockedLector);
        _userManagerMock.Setup(m => m.VerifyUserTokenAsync(
                lockedLector,
                TokenOptions.DefaultProvider,
                AuthController.LectorVerificationTokenPurpose,
                "verify-token"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.VerifyLector(model) as OkResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(lockedLector.EmailConfirmed, Is.True);
        Assert.That(lockedLector.LastVerifiedOnUtc, Is.Not.Null);
        _userManagerMock.Verify(m => m.UpdateAsync(lockedLector), Times.Once);
    }

    [Test]
    public async Task VerifyLector_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var lockedLector = new User { Id = 42, Email = "lector@pxl.be", EmailConfirmed = true };
        var model = new VerifyLectorModel { UserId = "42", Token = "expired" };
        _userManagerMock.Setup(m => m.FindByIdAsync("42")).ReturnsAsync(lockedLector);
        _userManagerMock.Setup(m => m.VerifyUserTokenAsync(
                lockedLector,
                TokenOptions.DefaultProvider,
                AuthController.LectorVerificationTokenPurpose,
                "expired"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.VerifyLector(model) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(lockedLector.LastVerifiedOnUtc, Is.Null);
        _userManagerMock.Verify(m => m.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task VerifyLector_UserNotFound_ReturnsOkWithoutLeaking()
    {
        // Arrange
        var model = new VerifyLectorModel { UserId = "999", Token = "any" };
        _userManagerMock.Setup(m => m.FindByIdAsync("999")).ReturnsAsync((User)null);

        // Act
        var result = await _controller.VerifyLector(model) as OkResult;

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task VerifyLector_And_ResendLectorVerification_InvalidModelStateOrUpdateFailure_ReturnExpectedResponses()
    {
        _controller.ModelState.AddModelError("Token", "Required");
        Assert.That(await _controller.VerifyLector(new VerifyLectorModel()), Is.InstanceOf<BadRequestObjectResult>());
        Assert.That(await _controller.ResendLectorVerification(new ResendLectorVerificationModel()), Is.InstanceOf<BadRequestObjectResult>());
        _controller.ModelState.Clear();

        _userManagerMock.Setup(m => m.FindByIdAsync("999")).ReturnsAsync((User)null);
        Assert.That(
            await _controller.ResendLectorVerification(new ResendLectorVerificationModel { UserId = "999", ResendToken = "tok" }),
            Is.InstanceOf<OkResult>());

        var user = new User { Id = 42, Email = "lector@pxl.be", EmailConfirmed = false };
        _userManagerMock.Setup(m => m.FindByIdAsync("42")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.VerifyUserTokenAsync(
                user,
                TokenOptions.DefaultProvider,
                AuthController.LectorVerificationTokenPurpose,
                "valid-tok"))
            .ReturnsAsync(true);
        _userManagerMock.Setup(m => m.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "UpdateErr", Description = "Update failed" }));

        var badResult = await _controller.VerifyLector(new VerifyLectorModel { UserId = "42", Token = "valid-tok" })
            as BadRequestObjectResult;
        Assert.That(badResult, Is.Not.Null);
    }
}