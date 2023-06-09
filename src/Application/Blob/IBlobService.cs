namespace Aptabase.Application.Blob;

public class Blob
{
    public string Path { get; set; } = "";
    public byte[] Content { get; set; } = new byte[0];
    public string ContentType { get; set; } = "";
}

public interface IBlobService
{
    Task<string> UploadAsync(string prefix, byte[] content, string contentType, CancellationToken cancellationToken);
    Task<Blob?> DownloadAsync(string path, CancellationToken cancellationToken);
}