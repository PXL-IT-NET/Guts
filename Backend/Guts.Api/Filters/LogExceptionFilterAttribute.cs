using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            _logger.LogError(context.Exception, "Unexpected API exception");
            base.OnException(context);
        }
    }
}
