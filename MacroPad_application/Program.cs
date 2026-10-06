//--------------------------------------//
//     Fortuna Alexandru Cristian       //
//--------------------------------------//
//              18.09.2026              //
//--------------------------------------//
//               MacroPad               //
//--------------------------------------//

namespace MacroPad_application;
using System;

using Controllers;
using Models;

class Program
{
    static void Main()
    {
        ConfigurationReader configReader = new ConfigurationReader();
        MacController macController = new MacController();
        MacroPadController  macroPadController = new MacroPadController();
            
        Console.WriteLine("MacroPad is running!");
        
        Macro[] definedMacro = configReader.ReadMacros("../../../Configuration/macros.json");
        int buttonCode = 30;
        
        Macro? macro = macroPadController.FindMacro(definedMacro, buttonCode);
        
        if (macro != null)
        {
            macController.ExecuteMacro(macro);
        }
    }
}