using System.Globalization;

namespace ArduinoDataLogger.Services;

public static class MeasurementCsvExporter
{
    public const string Header = "Timestamp,Value";

    public static IReadOnlyList<string> CreateLines(IEnumerable<Measurement> measurements)
    {
        ArgumentNullException.ThrowIfNull(measurements);

        return measurements
            .Select(item => string.Create(CultureInfo.InvariantCulture,
                $"{item.Timestamp:HH:mm:ss.fff},{item.Value:F2}"))
            .Prepend(Header)
            .ToArray();
    }
}
