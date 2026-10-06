namespace MacroPad_application.Controllers;

using Models;

public class MacroPadController
{
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