namespace ArduinoDataLogger.Services;

/// <summary>One value received from the Arduino together with its receipt time.</summary>
public sealed record Measurement(DateTime Timestamp, double Value);
