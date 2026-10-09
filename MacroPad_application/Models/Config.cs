namespace MacroPad_application.Models;

public record Config(
    int NumberOfButtons,
    int BaudRate,
    string MacrosFile
    );