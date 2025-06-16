using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Leaderboard.Infrastructure
{
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;
        private readonly IWebHostEnvironment _env;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "An unhandled exception occurred");

            var response = new
            {
                error = new
                {
                    message = GetUserFriendlyMessage(context.Exception),
                    type = context.Exception.GetType().Name,
                    details = _env.IsDevelopment() ? context.Exception.ToString() : null
                }
            };

            context.Result = new JsonResult(response)
            {
                StatusCode = GetStatusCode(context.Exception)
            };

            context.ExceptionHandled = true;
        }

        private static string GetUserFriendlyMessage(Exception exception)
        {
            return exception switch
            {
                ArgumentException => exception.Message,
                KeyNotFoundException => "resource does not exist",
                _ => "service is busy"
            };
        }

        private static int GetStatusCode(Exception exception)
        {
            return exception switch
            {
                ArgumentException => (int)HttpStatusCode.BadRequest,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                _ => (int)HttpStatusCode.InternalServerError
            };
        }
    }
}
