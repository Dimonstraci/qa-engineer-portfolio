using ArduinoDataLogger.Services;

namespace Grib_V2._0test.Tests;

public sealed class MeasurementCsvExporterTests
{
    [Fact]
    public void CreateLines_ExportsHeaderAndInvariantDecimalValue()
    {
        var lines = MeasurementCsvExporter.CreateLines(new[]
        {
            new Measurement(new DateTime(2026, 9, 7, 8, 5, 1, 123), 12.5)
        });

        Assert.Equal(new[] { "Timestamp,Value", "08:05:01.123,12.50" }, lines);
    }

    [Fact]
    public void CreateLines_WithNoMeasurements_ExportsOnlyHeader()
    {
        Assert.Equal(new[] { "Timestamp,Value" }, MeasurementCsvExporter.CreateLines(Array.Empty<Measurement>()));
    }

    [Theory]
    [InlineData(1.234, "1.23")]
    [InlineData(1.235, "1.24")]
    [InlineData(-0.5, "-0.50")]
    public void CreateLines_FormatsValuesWithTwoDecimalPlaces(double value, string expectedValue)
    {
        var lines = MeasurementCsvExporter.CreateLines(new[]
        {
            new Measurement(new DateTime(2026, 1, 1, 0, 0, 0), value)
        });

        Assert.Equal($"00:00:00.000,{expectedValue}", lines[1]);
    }

    [Fact]
    public void CreateLines_WithNullMeasurements_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => MeasurementCsvExporter.CreateLines(null!));
}
