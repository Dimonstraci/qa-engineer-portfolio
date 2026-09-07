namespace ArduinoDataLogger.Services;

/// <summary>Thread-safe bounded history of measurements shown in the chart.</summary>
public sealed class MeasurementBuffer
{
    private readonly object _sync = new();
    private readonly List<Measurement> _items = new();
    private readonly int _capacity;

    public MeasurementBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        _capacity = capacity;
    }

    public void Add(Measurement measurement)
    {
        lock (_sync)
        {
            _items.Add(measurement);
            if (_items.Count > _capacity)
                _items.RemoveAt(0);
        }
    }

    public void Clear()
    {
        lock (_sync)
            _items.Clear();
    }

    public IReadOnlyList<Measurement> Snapshot()
    {
        lock (_sync)
            return _items.ToArray();
    }
}
