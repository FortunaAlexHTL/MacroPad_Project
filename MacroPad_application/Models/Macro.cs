namespace MacroPad_application.Models
{
    public record Macro(
        int buttonCode,
        string ActionType,
        int[] keys,
        string? ApplicationPath
    );
}