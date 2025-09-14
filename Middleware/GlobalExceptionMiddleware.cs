using System.Net;
using System.Text.Json;
using UretimAPI.DTOs.Common;
using UretimAPI.Exceptions;

namespace UretimAPI.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = exception switch
            {
                NotFoundException notFoundEx => new ApiResponse<object>
                {
                    Success = false,
                    Message = notFoundEx.Message,
                    Data = null,
                    Errors = new List<string> { notFoundEx.Message }
                },
                ValidationException validationEx => new ApiResponse<object>
                {
                    Success = false,
                    Message = validationEx.Message,
                    Data = null,
                    Errors = validationEx.Errors
                },
                DuplicateException duplicateEx => new ApiResponse<object>
                {
                    Success = false,
                    Message = duplicateEx.Message,
                    Data = null,
                    Errors = new List<string> { duplicateEx.Message }
                },
                BusinessException businessEx => new ApiResponse<object>
                {
                    Success = false,
                    Message = businessEx.Message,
                    Data = null,
                    Errors = new List<string> { businessEx.Message }
                },
                _ => new ApiResponse<object>
                {
                    Success = false,
                    Message = "An internal server error occurred.",
                    Data = null,
                    Errors = new List<string> { "Internal server error" }
                }
            };

            context.Response.StatusCode = exception switch
            {
                NotFoundException => (int)HttpStatusCode.NotFound,
                ValidationException => (int)HttpStatusCode.BadRequest,
                DuplicateException => (int)HttpStatusCode.Conflict,
                BusinessException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}