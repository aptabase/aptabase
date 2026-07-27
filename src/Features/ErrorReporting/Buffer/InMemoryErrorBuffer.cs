namespace Aptabase.Features.ErrorReporting.Buffer;

public class InMemoryErrorBuffer : IErrorBuffer
{
    private List<TrackingError> _buffer = new();
    private object _lock = new object();

    public void Add(ref TrackingError error)
    {
        lock (_lock)
        {
            _buffer.Add(error);
        }
    }

    public TrackingError[] TakeAll()
    {
        lock (_lock)
        {
            var all = _buffer.ToArray();
            _buffer.Clear();
            return all;
        }
    }

    public int Count()
    {
        lock (_lock)
        {
            return _buffer.Count;
        }
    }
}
