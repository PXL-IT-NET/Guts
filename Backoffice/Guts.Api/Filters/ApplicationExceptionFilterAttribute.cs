using System;
using System.Net;
using Guts.Api.Models;
using Guts.Common;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
namespace Guts.Api.Filters
{
    /// <inheritdoc />
    public class ApplicationExceptionFilterAttribute : ExceptionFilterAttribute
    {
        private readonly ILogger _logger;

        /// <inheritdoc />
        public ApplicationExceptionFilterAttribute(ILogger<ApplicationExceptionFilterAttribute> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public override void OnException(ExceptionContext context)
        {
            switch (context.Exception)
            {
                case ContractException _:
                case InvalidOperationException _:
                    _logger.LogWarning(context.Exception, $"Bad request detected: {GetRequestUrl(context)}");
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Result = new JsonResult(ErrorModel.FromException(context.Exception));
                    break;
                default:
                    _logger.LogError(context.Exception,
                        $"An unhandled exception occurred in the application. Request: {GetRequestUrl(context)}");
                    context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Result = new JsonResult(ErrorModel.FromString("An unexpected error has occurred."));
                    break;
            }
        }

        private string GetRequestUrl(ExceptionContext context)
        {
            if (context.HttpContext?.Request == null) return string.Empty;
            return $"{context.HttpContext.Request.Method} - {context.HttpContext.Request.GetDisplayUrl()}";
        }
    }
}
