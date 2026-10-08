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
    /// <returns>The first matching macro, or null if no matching macro exists.</returns>
    /// <remarks>Writes a console message when no match is found, including for an empty array.</remarks>
    public Macro? FindMacro(Macro[] macros, int buttonCode)
    {
        Macro? macro = null;
        
        for (int i = 0; i < macros.Length && macro == null; i++)
        {
            if (buttonCode == macros[i].buttonCode)
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
