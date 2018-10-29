using System.Net;
using System.Threading.Tasks;
using Guts.Api.Models;
using Guts.Business.Captcha;
using Guts.Business.Communication;
using Guts.Business.Security;
using Guts.Business.Services;
using Guts.Data;
using Guts.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Guts.Api.Controllers
{
    /// <summary>
    /// Register and confirm users, authenticate users (bearer token).
    /// </summary>
    [Produces("application/json")]
    [Route("api/Auth")]
    public class AuthController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ICaptchaValidator _captchaValidator;
        private readonly IMailSender _mailSender;
        private readonly ITokenAccessPassFactory _tokenAccessPassFactory;
        private readonly ILoginSessionService _loginSessionService;

        public AuthController(UserManager<User> userManager,
            IPasswordHasher<User> passwordHasher,
            ICaptchaValidator captchaValidator,
            IMailSender mailSender,
            ITokenAccessPassFactory tokenAccessPassFactory,
            ILoginSessionService loginSessionService)
        {
            _userManager = userManager;
            _passwordHasher = passwordHasher;
            _captchaValidator = captchaValidator;
            _mailSender = mailSender;
            _tokenAccessPassFactory = tokenAccessPassFactory;
            _loginSessionService = loginSessionService;
        }

        /// <summary>
        /// Registers a new user in the system.
        /// If successful, a confirmation email is send to the users email address, the user can prove he or she owns the email address.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!model.Email.ToLower().EndsWith("pxl.be"))
            {
                return BadRequest("Only PXL email adresses are allowed.");
            }

            var validationResult = await _captchaValidator.Validate(model.CaptchaToken, Request.HttpContext.Connection.RemoteIpAddress);
            if (!validationResult.Success)
            {
                return BadRequest("Could not verify captcha.");
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var role = user.Email.ToLower().EndsWith("student.pxl.be") ? Role.Constants.Student : Role.Constants.Lector;
                await _userManager.AddToRoleAsync(user, role);
                await SendConfirmUserEmailMessage(user);
                return Ok();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return BadRequest(ModelState);
        }

        /// <summary>
        /// Confirms the email address of a registered user by providing the token that was send to the users email adres.
        /// </summary>
        [HttpPost("confirmemail")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                //don't reveal if the user does not exist
                return Ok();
            }

            var result = await _userManager.ConfirmEmailAsync(user, model.Token);

            if (result.Succeeded)
            {
                return Ok();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return BadRequest(ModelState);
        }

        /// <summary>
        /// Sends an email to a user with a token (link) that can be used to reset the password of the user.
        /// </summary>
        [HttpPost("forgotpassword")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> SendForgotPasswordMail([FromBody] ForgotPasswordModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var validationResult = await _captchaValidator.Validate(model.CaptchaToken, Request.HttpContext.Connection.RemoteIpAddress);
            if (!validationResult.Success)
            {
                return BadRequest("Could not verify captcha.");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || !user.EmailConfirmed)
            {
                //don't reveal if the user does not exist or the email is not yet confirmed
                return Ok();
            }

            var resetPasswordToken = await _userManager.GeneratePasswordResetTokenAsync(user);

            await _mailSender.SendForgotPasswordMessageAsync(user, resetPasswordToken);

            return Ok();
        }

        /// <summary>
        /// Resets the password of a user using a token that was sent to the users email address.
        /// </summary>
        [HttpPost("resetpassword")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                //don't reveal if the user does not exist
                return Ok();
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                return Ok();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }
            return BadRequest(ModelState);
        }

        /// <summary>
        /// Request a (bearer) token that can be used to authenticate yourself in future requests.
        /// </summary>
        [HttpPost("token")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(TokenAccessPass), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType((int)HttpStatusCode.Unauthorized)]
        public async Task<IActionResult> CreateToken([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                return Unauthorized();
            }

            if (_passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password) != PasswordVerificationResult.Success)
            {
                return Unauthorized();
            }

            if (!user.EmailConfirmed)
            {
                await SendConfirmUserEmailMessage(user);

                ModelState.AddModelError("EmailNotConfirmed", $"Please confirm your email address. A confirmation mail has been sent to {user.Email}.");
                return BadRequest(ModelState);
            }

            var currentClaims = await _userManager.GetClaimsAsync(user);
            var userRoles = await _userManager.GetRolesAsync(user);
            var tokenAccessPass = _tokenAccessPassFactory.Create(user, currentClaims, userRoles);

            if (!string.IsNullOrEmpty(model.LoginSessionPublicIdentifier))
            {
                await _loginSessionService.SetLoginTokenForSessionAsync(model.LoginSessionPublicIdentifier, tokenAccessPass.Token);
            }

            return Ok(tokenAccessPass);
        }

        [HttpPost("loginsession")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginSession), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> CreateLoginSession()
        {
            await _loginSessionService.CleanUpOldSessionsAsync();
            var session = await _loginSessionService.CreateSessionAsync(HttpContext.Connection.RemoteIpAddress.ToString());
            return Ok(session);
        }

        [HttpPost("loginsession/{publicIdentifier}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginSession), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> RetrieveLoginSession(string publicIdentifier, [FromBody] string secretToken)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress.ToString();
            try
            {
                var session = await _loginSessionService.GetSessionAsync(publicIdentifier, clientIp, secretToken);
                return Ok(session);
            }
            catch (DataNotFoundException)
            {
                return BadRequest();
            }
        }

        [HttpPatch("loginsession/{publicIdentifier}/cancel")]
        [AllowAnonymous]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> CancelLoginSession(string publicIdentifier)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress.ToString();

            try
            {
                await _loginSessionService.CancelSessionAsync(publicIdentifier, clientIp);
                return Ok();
            }
            catch (DataNotFoundException)
            {
                return BadRequest();
            }
        }

        private async Task SendConfirmUserEmailMessage(User user)
        {
            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _mailSender.SendConfirmUserEmailMessageAsync(user, confirmationToken);
        }

    }
}