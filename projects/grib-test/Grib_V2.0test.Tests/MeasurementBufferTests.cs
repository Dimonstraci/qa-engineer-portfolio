using ArduinoDataLogger.Services;

namespace Grib_V2._0test.Tests;

public sealed class MeasurementBufferTests
{
    [Fact]
    public void Add_WhenCapacityExceeded_KeepsNewestMeasurements()
    {
        var buffer = new MeasurementBuffer(2);
        var time = new DateTime(2026, 1, 1, 10, 0, 0);

        buffer.Add(new Measurement(time, 1));
        buffer.Add(new Measurement(time.AddSeconds(1), 2));
        buffer.Add(new Measurement(time.AddSeconds(2), 3));

        var result = buffer.Snapshot();

        Assert.Collection(result,
            item => Assert.Equal(2, item.Value),
            item => Assert.Equal(3, item.Value));
    }

    [Fact]
    public void Snapshot_ReturnsIndependentCopy()
    {
        var buffer = new MeasurementBuffer(2);
        buffer.Add(new Measurement(DateTime.UnixEpoch, 1));

        var snapshot = buffer.Snapshot();
        buffer.Clear();

        Assert.Single(snapshot);
        Assert.Empty(buffer.Snapshot());
    }

    [Fact]
    public void Add_WithCapacityOne_ReplacesPreviousMeasurement()
    {
        var buffer = new MeasurementBuffer(1);
        buffer.Add(new Measurement(DateTime.UnixEpoch, 1));
        buffer.Add(new Measurement(DateTime.UnixEpoch.AddSeconds(1), 2));

        var result = Assert.Single(buffer.Snapshot());

        Assert.Equal(2, result.Value);
    }

    [Fact]
    public void Clear_AfterSeveralMeasurements_RemovesAllMeasurements()
    {
        var buffer = new MeasurementBuffer(3);
        buffer.Add(new Measurement(DateTime.UnixEpoch, 1));
        buffer.Add(new Measurement(DateTime.UnixEpoch.AddSeconds(1), 2));

        buffer.Clear();

        Assert.Empty(buffer.Snapshot());
    }

    [Fact]
    public void Add_PreservesTimestampAndNegativeValue()
    {
        var timestamp = new DateTime(2026, 9, 7, 12, 30, 45, 987);
        var buffer = new MeasurementBuffer(2);

        buffer.Add(new Measurement(timestamp, -17.75));

        Assert.Equal(new Measurement(timestamp, -17.75), Assert.Single(buffer.Snapshot()));
    }

    [Fact]
    public void Add_WhenFull_PreservesChronologicalOrderOfRemainingMeasurements()
    {
        var buffer = new MeasurementBuffer(3);
        var timestamp = DateTime.UnixEpoch;
        for (var value = 1; value <= 4; value++)
            buffer.Add(new Measurement(timestamp.AddSeconds(value), value));

        Assert.Equal(new[] { 2d, 3d, 4d }, buffer.Snapshot().Select(item => item.Value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveCapacity_Throws(int capacity) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new MeasurementBuffer(capacity));
}
