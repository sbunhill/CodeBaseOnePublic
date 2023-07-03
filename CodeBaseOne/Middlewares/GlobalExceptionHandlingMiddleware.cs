using System.Diagnostics.CodeAnalysis;

namespace CodeBaseOne.Middlewares
{
    /// <summary>
    /// ToDo
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        /// <summary>
        /// ToDo
        /// </summary>
        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger("GlobalExceptionHandlingMiddleware");
        }

        /// <summary>
        /// ToDo
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                context.Response.StatusCode = 500;
            }
        }
    }
}
