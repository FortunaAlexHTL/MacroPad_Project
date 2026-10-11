namespace MacroPad_application.Models;

public record Macro(
    string? ArduinoDeviceId,
    int ButtonCode,
    string ActionType,
    int[] Keys,
    string? ApplicationPath 
    );
