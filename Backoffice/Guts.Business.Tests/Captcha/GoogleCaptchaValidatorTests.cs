using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Guts.Business.Captcha;
using Guts.Business.Communication;
using Guts.Common.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Guts.Business.Tests.Captcha;

[TestFixture]
public class GoogleCaptchaValidatorTests
{
    [Test]
    public void Validate_ShouldSendTokenInfoToGoogleAndRetrieveResult()
    {
        //Arrange
        var validationUrl = Guid.NewGuid().ToString();
        var secret = Guid.NewGuid().ToString();
        var captchaToken = Guid.NewGuid().ToString();
        var random = new Random();
        var ip = new IPAddress(random.NextPositive());

        var verificationResult = new CaptchaVerificationResult
        {
            Success = true
        };

        var httpClientMock = new Mock<IHttpClient>();
        httpClientMock
            .Setup(client =>
                client.PostAsFormUrlEncodedContentAsync<CaptchaVerificationResult>(It.IsAny<string>(),
                    It.IsAny<KeyValuePair<string, string>[]>())).ReturnsAsync(() => verificationResult);

        var loggerMock = new Mock<ILogger<GoogleCaptchaValidator>>();

        var validator = new GoogleCaptchaValidator(validationUrl, secret, httpClientMock.Object, loggerMock.Object);


        //Act
        var result = validator.Validate(captchaToken, ip).Result;

        //Assert
        Assert.That(result, Is.EqualTo(verificationResult));
        httpClientMock.Verify(
            client => client.PostAsFormUrlEncodedContentAsync<CaptchaVerificationResult>(validationUrl,
                It.Is((KeyValuePair<string, string>[] data) => data.Any(d => d.Key == "secret" && d.Value == secret) 
                                                               && data.Any(d => d.Key == "response" && d.Value == captchaToken)
                                                               && data.Any(d => d.Key == "remoteip" && d.Value == ip.ToString())
                )), Times.Once);
    }

    [Test]
    public void Validate_ExceptionThrownByHttpClient_ShouldLogErrorAndReturnFailureResult()
    {
        //Arrange
        var validationUrl = Guid.NewGuid().ToString();
        var secret = Guid.NewGuid().ToString();
        var captchaToken = Guid.NewGuid().ToString();
        var ip = new IPAddress(Random.Shared.NextPositive());

        var httpClientMock = new Mock<IHttpClient>();
        httpClientMock
            .Setup(client =>
                client.PostAsFormUrlEncodedContentAsync<CaptchaVerificationResult>(It.IsAny<string>(),
                    It.IsAny<KeyValuePair<string, string>[]>()))
            .ThrowsAsync(new Exception("Network error"));

        var loggerMock = new Mock<ILogger<GoogleCaptchaValidator>>();

        var validator = new GoogleCaptchaValidator(validationUrl, secret, httpClientMock.Object, loggerMock.Object);

        //Act
        var result = validator.Validate(captchaToken, ip).Result;

        //Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.False);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
    }
}