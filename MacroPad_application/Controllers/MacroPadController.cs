// Description: Finds the configured macro for a device name and received button code.

namespace MacroPad_application.Controllers;

using Models;

public class MacroPadController
{
    /// <summary>
    /// Finds the first macro whose device name and button code match the received message.
    /// </summary>
    /// <param name="macros">The macro array to search, containing no null entries.</param>
    /// <param name="buttonCode">The button code received from the Arduino.</param>
    /// <param name="arduinoDeviceName">The sending device name; matching is exact and case-sensitive.</param>
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
