using AUTH_Sevice.Application.DTOs;
using AUTH_Sevice.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace AUTH_Sevice.Middlewares
{

    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, response) = exception switch
            {
                ValidationException validationEx => (
                    HttpStatusCode.BadRequest,
                    (object)new ValidationErrorResponse(
                        "VALIDATION_ERROR",
                        "One or more validation errors occurred.",
                        validationEx.Errors
                            .GroupBy(e => e.PropertyName)
                            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))),

                InvalidCredentialsException or UserLockedException => (
                    HttpStatusCode.Unauthorized,
                    new ErrorResponse("AUTHENTICATION_FAILED", exception.Message)),

                EmailAlreadyExistsException => (
                    HttpStatusCode.Conflict,
                    new ErrorResponse("CONFLICT", exception.Message)),

                UserNotFoundException => (
                    HttpStatusCode.NotFound,
                    new ErrorResponse("NOT_FOUND", exception.Message)),

                InvalidRefreshTokenException => (
                    HttpStatusCode.Unauthorized,
                    new ErrorResponse("INVALID_TOKEN", exception.Message)),

                UserInactiveException => (
                    HttpStatusCode.Forbidden,
                    new ErrorResponse("ACCOUNT_INACTIVE", exception.Message)),

                UnauthorizedAccessException => (
                    HttpStatusCode.Unauthorized,
                    new ErrorResponse("UNAUTHORIZED", exception.Message)),

                _ => (
                    HttpStatusCode.InternalServerError,
                    new ErrorResponse("INTERNAL_ERROR", "An unexpected error occurred."))
            };

            context.Response.StatusCode = (int)statusCode;
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await context.Response.WriteAsync(json);
        }
    }
}
