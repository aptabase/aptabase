using System.Data;
using Aptabase.Application.Blob;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aptabase.Controllers;

public class UploadedFile
{
    public byte[] Content { get; set; } = new byte[0];
    public string ContentType { get; set; } = "";
}

[ApiController, AllowAnonymous]
public class UploadsController : Controller
{
    private readonly IBlobService _blobService;

    public UploadsController(IBlobService blobService)
    {
        _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
    }

    [HttpGet("/uploads/{**path}")]
    [ResponseCache(Duration = 365 * 24 * 60 * 60)] // Cache for 1 year because uploaded files are immutable
    public async Task<IActionResult> ReadFile(string path, CancellationToken cancellationToken)
    {
        var file = await _blobService.DownloadAsync(path, cancellationToken);
        if (file == null)
            return NotFound();

        return File(file.Content, file.ContentType);
    }
}
