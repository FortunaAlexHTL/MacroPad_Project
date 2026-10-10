//--------------------------------------//
//     Fortuna Alexandru Cristian       //
//--------------------------------------//
//              18.09.2026              //
//--------------------------------------//
//               MacroPad               //
//--------------------------------------//


namespace MacroPad_application;
using System;
using System.Diagnostics;

using Connections;
using Controllers;
using Models;

class Program
{
    private const string ConfigFileName = "config.json";
    static void Main()
    {
        // Starting up controllers and configuration reader
        MacOSController macOsController = new MacOSController();
        MacroPadController  macroPadController = new MacroPadController();
        ArduinoController  arduinoController = new ArduinoController();
        ConfigurationReader configurationReader = new ConfigurationReader();
        // ------------------------------------------------
        
        // Getting configuration and macros
        string configurationDirectoryPath = configurationReader.GetConfigurationDirectoryPath();
        
        string configPath = Path.Combine(configurationDirectoryPath, ConfigFileName);
        Config definedConfig = configurationReader.ReadConfig(configPath);
        
        string macrosPath = Path.Combine(configurationDirectoryPath, definedConfig.MacrosFile);
        Macro[] definedMacros = configurationReader.ReadMacros(macrosPath);
        // --------------------------------
        
        Console.WriteLine("- MacroPad is running ...\n");
        
        Console.WriteLine("- Searching for device ...");
        ArduinoConnection[] arduinoConnections = [];
        Stopwatch discoveryTimer = Stopwatch.StartNew();
        
        string[] candidatePortNames = [];
        int candidateIndex = 0;
        
        
        while (true)
        {
            for (int i = 0; i < arduinoConnections.Length; i++)
            {
                string? receivedData = arduinoConnections[i].ReadMessage();
                    
                if (receivedData != null)
                {
                    ProcessMessage(receivedData, arduinoConnections[i].GetDevice().Name, definedMacros, macroPadController, macOsController);
                }
            }
            
            arduinoConnections = arduinoController.RemoveInactiveConnections(arduinoConnections);

            if (discoveryTimer.ElapsedMilliseconds >= 3000 && !arduinoController.IsDiscoveryRunning() && candidateIndex >= candidatePortNames.Length)
            {
                candidatePortNames = arduinoController.GetNewPortNames(arduinoConnections);
                candidateIndex = 0;
                    
                discoveryTimer.Restart();
            }

            if (!arduinoController.IsDiscoveryRunning() && candidateIndex < candidatePortNames.Length)
            {
                arduinoController.StartDiscovery(candidatePortNames[candidateIndex], definedConfig.BaudRate);
                candidateIndex++;
            }
                
            Arduino? discoveredDevice = arduinoController.UpdateDiscovery();

            if (discoveredDevice != null)
            {
                arduinoConnections = arduinoController.AddConnection(arduinoConnections, discoveredDevice, definedConfig.BaudRate);
            }

            Thread.Sleep(10);
        }
    }

    /// <summary>
    /// Routes a complete device message to macro execution or identity and invalid-message reporting.
    /// </summary>
    /// <param name="receivedData">A non-null complete message returned by the connection.</param>
    /// <param name="deviceName">The sending device's name, used for macro matching and identity checks.</param>
    /// <param name="definedMacros">The macro mappings loaded at startup.</param>
    /// <param name="macroPadController">Finds the mapping for the device name and button code.</param>
    /// <param name="macOsController">Executes the selected keyboard or application action.</param>
    /// <remarks>Unknown codes execute no action. Macro-execution errors propagate to the caller.</remarks>
    private static void ProcessMessage(string receivedData, string deviceName, Macro[] definedMacros, MacroPadController macroPadController,
        MacOSController macOsController)
    {
        if (int.TryParse(receivedData, out int buttonCode))
        {
            Macro? macro = macroPadController.FindMacro(
                definedMacros, buttonCode, deviceName);

            if (macro != null)
            {
                macOsController.ExecuteMacro(macro);
            }
        }
        else
        {
            string[] parts = receivedData.Split(':');

            if (parts.Length == 2 &&
                parts[0] == "MACROPAD" &&
                parts[1] == deviceName)
            {
                Console.WriteLine($"- Identity reply received from {deviceName}.");
            }
            else
            {
                Console.WriteLine($"- Invalid message received: {receivedData}.");
            }
        }
    }
}
