// Custom Exception Middleware to catch common exceptions caused by client disconnects
// Can be removed on .NET 8 because of https://github.com/dotnet/aspnetcore/pull/46330
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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning("Request to {Path} was cancelled.", context.Request.Path.Value);

            if (!context.Response.HasStarted)
                context.Response.StatusCode = 418; // I'm a teapot
        }
        catch (BadHttpRequestException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogWarning("Request to {Path} was cancelled.", context.Request.Path.Value);

            if (!context.Response.HasStarted)
                context.Response.StatusCode = 418; // I'm a teapot
        }
    }
}