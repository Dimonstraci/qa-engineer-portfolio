using System.Globalization;

namespace ArduinoDataLogger.Services;

public static class MeasurementParser
{
    public static bool TryParse(string? input, out double value)
    {
        var parsed = double.TryParse(input?.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        if (parsed && double.IsFinite(value))
            return true;

        value = default;
        return false;
    }
}
