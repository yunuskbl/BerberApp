using System.Net;
using System.Text.Json;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Models;
using FluentValidation;

namespace BerberApp.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = HttpStatusCode.InternalServerError;
        var message = "Sunucu hatası oluştu.";
        var errors = new List<string>();

        switch (exception)
        {
            case ValidationException ve:
                statusCode = HttpStatusCode.BadRequest;
                message = "Doğrulama hatası.";
                errors = ve.Errors.Select(e => e.ErrorMessage).ToList();
                break;
            case NotFoundException e:
                statusCode = HttpStatusCode.NotFound;
                message = e.Message;
                break;
            case BadRequestException e:
                statusCode = HttpStatusCode.BadRequest;
                message = e.Message;
                break;
            case UnauthorizedException e:
                statusCode = HttpStatusCode.Unauthorized;
                message = e.Message;
                break;
            case ConflictException e:
                statusCode = HttpStatusCode.Conflict;
                message = e.Message;
                break;
            default:
                _logger.LogError(exception, "Beklenmeyen hata: {Message}", exception.Message);
                break;
        }

        if (statusCode != HttpStatusCode.InternalServerError)
            _logger.LogWarning("İşlem hatası: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = errors.Any()
            ? ApiResponse.Fail(message, errors)
            : ApiResponse.Fail(message);

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}