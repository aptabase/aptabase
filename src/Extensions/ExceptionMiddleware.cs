// Custom Exception Middleware to catch common exceptions caused by client disconnects
// Some conditions can be removed on .NET 8 because of https://github.com/dotnet/aspnetcore/pull/46330
using System.Net;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == (int)HttpStatusCode.RequestTimeout)
        {
            _logger.LogWarning("Request to {Path} timed out. {Exception}", context.Request.Path.Value, ex.Message);
        }
        catch (TaskCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning("Request to {Path} was cancelled.", context.Request.Path.Value);

            context.Response.StatusCode = 418; // I'm a teapot
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning("Request to {Path} was cancelled.", context.Request.Path.Value);

            context.Response.StatusCode = 418; // I'm a teapot
        }
        catch (BadHttpRequestException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning("Request to {Path} was cancelled.", context.Request.Path.Value);

            context.Response.StatusCode = 418; // I'm a teapot
        }
        catch (HttpRequestException ex)
        {
            context.Response.StatusCode = (int)(ex.StatusCode ?? HttpStatusCode.InternalServerError);

            _logger.LogError(ex, "Dependency error on {Path}", context.Request.Path.Value);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            _logger.LogError(ex, "Unexpected error on {Path}", context.Request.Path.Value);
        }
    }
}