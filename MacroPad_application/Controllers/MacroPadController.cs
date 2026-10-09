// Description: Finds the configured macro for a received button code.

namespace MacroPad_application.Controllers;

using Models;

public class MacroPadController
{
    /// <summary>
    /// Finds the first macro whose button code matches the received code.
    /// </summary>
    /// <param name="macros">The macro array to search, containing no null entries.</param>
    /// <param name="buttonCode">The button code received from the Arduino.</param>
    /// <param name="arduinoDeviceName"></param>
    /// <returns>The first matching macro, or null if no matching macro exists.</returns>
    /// <remarks>Writes a console message when no match is found, including for an empty array.</remarks>
    public Macro? FindMacro(Macro[] macros, int buttonCode, string arduinoDeviceName)
    {
        Macro? macro = null;
        
        for (int i = 0; i < macros.Length && macro == null; i++)
        {
            if (arduinoDeviceName == macros[i].ArduinoDeviceName && buttonCode == macros[i].ButtonCode)
            {
                macro = macros[i];
            }
        }

        if (macro == null)
        {
            Console.WriteLine($"No macro configured with button {buttonCode}.");
        }
        
        return macro;
    }
}
