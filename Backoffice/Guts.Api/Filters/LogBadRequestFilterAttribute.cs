using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Guts.Api.Filters
{
    [ExcludeFromCodeCoverage]
    public class LogBadRequestFilterAttribute : ResultFilterAttribute
    {
        private readonly ILogger _logger;

        public LogBadRequestFilterAttribute(ILogger<LogBadRequestFilterAttribute> logger)
        {
            _logger = logger;
        }

        public override void OnResultExecuted(ResultExecutedContext context)
        {
            if (context.HttpContext?.Response == null) return;
            if (context.HttpContext.Response.StatusCode == (int)HttpStatusCode.BadRequest)
            {
                StringBuilder messageBuilder = new StringBuilder();
                messageBuilder.AppendLine("Bad request detected.");
                messageBuilder.AppendLine($"Request: {context.HttpContext.Request.Method} - {context.HttpContext.Request.GetDisplayUrl()}");

                if (context.Result is BadRequestObjectResult badRequestObjectResult)
                {
                    string badRequestValue = JsonConvert.SerializeObject(badRequestObjectResult.Value);
                    messageBuilder.AppendLine($"Response object: {badRequestValue}");
                }

                _logger.LogWarning(messageBuilder.ToString());
            }
            base.OnResultExecuted(context);
        }
    }
}