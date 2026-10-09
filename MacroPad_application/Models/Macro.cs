namespace MacroPad_application.Models;

public record Macro(
    string? ArduinoDeviceName,
    int ButtonCode,
    string ActionType,
    int[] Keys,
    string? ApplicationPath 
    );
