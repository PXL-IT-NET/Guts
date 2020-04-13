using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Guts.Api.Filters
{
    public class LogExceptionFilterAttribute : ExceptionFilterAttribute
    {
        private readonly ILogger _logger;

        public LogExceptionFilterAttribute(ILogger<LogExceptionFilterAttribute> logger)
        {
            _logger = logger;
        }

        public override void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, $"Unexpected API exception. Request: {GetRequestUrl(context)}");
            base.OnException(context);
        }

        private string GetRequestUrl(ExceptionContext context)
        {
            if (context.HttpContext?.Request == null) return string.Empty;
            return $"{context.HttpContext.Request.Method} - {context.HttpContext.Request.GetDisplayUrl()}";
        }
    }
}
