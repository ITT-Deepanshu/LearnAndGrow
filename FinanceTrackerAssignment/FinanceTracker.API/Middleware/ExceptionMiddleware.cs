using FinanceTracker.API.Common;
using System.Text.Json;

namespace FinanceTracker.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AppException ex)
            {
                await Handle(context, ex.Message);
            }
            catch (Exception)
            {
                await Handle(context, "Something went wrong");
            }
        }

        private static async Task Handle(HttpContext context, string message)
        {
            var response = new ApiResponse<string>
            {
                Success = false,
                Message = message
            };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}