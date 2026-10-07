//--------------------------------------//
//     Fortuna Alexandru Cristian       //
//--------------------------------------//
//              18.09.2026              //
//--------------------------------------//
//               MacroPad               //
//--------------------------------------//

namespace MacroPad_application;
using System;
using System.IO.Ports;

using Controllers;
using Models;

class Program
{
    private const string ConfigFile = "../../../Configuration/config.json";
    static void Main()
    {
        ConfigurationReader configurationReader = new ConfigurationReader();
        MacController macController = new MacController();
        MacroPadController  macroPadController = new MacroPadController();
        
        Console.WriteLine("MacroPad is running!\n");
        
        string configPath = Path.GetFullPath(ConfigFile);

        string configDirectory = Path.GetDirectoryName(configPath)
                                 ?? throw new InvalidOperationException("Configuration directory not found.");

        Config definedConfig = configurationReader.ReadConfig(configPath);

        string macrosPath = Path.GetFullPath(
            definedConfig.MacrosFile, configDirectory);

        Macro[] definedMacro = configurationReader.ReadMacros(macrosPath);

        SerialPort arduino = new SerialPort(definedConfig.SerialPort, definedConfig.BaudRate);
        
        arduino.Open();
        Console.WriteLine($"MacroPad is listening on port {arduino.PortName}.\n");
        
        while (true)
        {
            string receivedData = arduino.ReadLine();
            
            if (int.TryParse(receivedData, out int buttonCode))
            {
                Macro? macro = macroPadController.FindMacro(definedMacro, buttonCode);
                if (macro != null)
                {
                    macController.ExecuteMacro(macro);
                }
            }
            else
            {
                Console.WriteLine($"Invalid button code: {receivedData}.\n");
            }
        }
        
    }
}