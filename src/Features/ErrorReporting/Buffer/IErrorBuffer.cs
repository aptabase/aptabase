namespace Aptabase.Features.ErrorReporting.Buffer;

public interface IErrorBuffer
{
    void Add(ref TrackingError error);
    TrackingError[] TakeAll();
}
