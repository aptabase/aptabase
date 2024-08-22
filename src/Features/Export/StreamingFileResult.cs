using Microsoft.AspNetCore.Mvc;

namespace Aptabase.Features.Export;

public class StreamingFileResult(Func<Stream, HttpContext, CancellationToken, Task> streamContent, string contentType, string fileName) : IActionResult
{
    private readonly Func<Stream, HttpContext, CancellationToken, Task> _streamContent = streamContent;
    private readonly string _contentType = contentType;
    private readonly string _fileName = fileName;

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var httpContext = context.HttpContext;
        var response = httpContext.Response;

        response.Headers.TryAdd("Content-Disposition", new[] { $"attachment; filename={_fileName}" });
        response.ContentType = _contentType;

        await _streamContent(response.Body, httpContext, httpContext.RequestAborted);
    }
}
