namespace MacroPad_application.Models;

public record Config(
    string SerialPort,
    int BaudRate,
    string MacrosFile
    );