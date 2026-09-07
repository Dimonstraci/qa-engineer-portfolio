using ArduinoDataLogger.Services;

namespace Grib_V2._0test.Tests;

public sealed class MeasurementParserTests
{
    [Theory]
    [InlineData("12.5", 12.5)]
    [InlineData(" -3.25 ", -3.25)]
    [InlineData("1e3", 1000)]
    [InlineData("0", 0)]
    [InlineData(".75", 0.75)]
    public void TryParse_ValidInvariantNumber_ReturnsValue(string input, double expected)
    {
        Assert.True(MeasurementParser.TryParse(input, out var actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12,5")]
    [InlineData("not-a-number")]
    [InlineData("12.3.4")]
    [InlineData("NaN")]
    public void TryParse_InvalidOrLocaleSpecificNumber_ReturnsFalse(string input) =>
        Assert.False(MeasurementParser.TryParse(input, out _));
}
