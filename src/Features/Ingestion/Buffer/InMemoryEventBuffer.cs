namespace Aptabase.Features.Ingestion.Buffer;

public interface IEventBuffer
{
    void Add(ref TrackingEvent @event);
    void AddRange(ref TrackingEvent[] events);
    void AddRange(ref IEnumerable<TrackingEvent> events);
    TrackingEvent[] TakeAll();
}

public class InMemoryEventBuffer : IEventBuffer
{
    private List<TrackingEvent> _buffer = new();
    private object _lock = new object();

    public void Add(ref TrackingEvent @event)
    {
        lock (_lock)
        {
            _buffer.Add(@event);
        }
    }

    public void AddRange(ref TrackingEvent[] events)
    {
        lock (_lock)
        {
            _buffer.AddRange(events);
        }
    }

    public void AddRange(ref IEnumerable<TrackingEvent> events)
    {
        lock (_lock)
        {
            _buffer.AddRange(events);
        }
    }

    public TrackingEvent[] TakeAll()
    {
        lock (_lock)
        {
            var items = _buffer.ToArray();
            _buffer.Clear();
            return items;
        }
    }
}