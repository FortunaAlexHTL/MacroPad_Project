//--------------------------------------//
//     Fortuna Alexandru Cristian       //
//--------------------------------------//
//              18.09.2026              //
//--------------------------------------//
//               MacroPad               //
//--------------------------------------//


// Description: Coordinates configuration, board discovery, presets, and macro execution.

namespace MacroPad_application;
using System;
using System.Diagnostics;

using Backup;
using Connections;
using Controllers;
using Models;

class Program
{
    private const string ConfigFileName = "config.json";
    static void Main()
    {
        // Starting up controllers, configuration management and backups
        Logger logger = new Logger();
        MacOSController macOsController = new MacOSController();
        MacroPadController  macroPadController = new MacroPadController();
        ArduinoController  arduinoController = new ArduinoController();
        ConfigurationManager configurationManager = new ConfigurationManager();
        BackupManager backupManager = new BackupManager();
        // ------------------------------------------------
        
        Console.WriteLine("- MacroPad is running ...\n");
        logger.LogInfo("MacroPad application started.");
        // Getting configuration and macros
        Config? definedConfig = null;
        Macro[] definedMacros = [];
        bool readSuccesfull = configurationManager.ReadAllFiles(ref definedConfig, ref definedMacros);
        if (!readSuccesfull || definedConfig == null)
        {
            Console.WriteLine("Configuration could not be loaded after recovery attempts. Please, reconfigure the settings.");
            Thread.Sleep(5000);
            // TODO - Implement opening settings app if everything fails and ask user to reconfigure settings, then continue startup.
            return;
        }
        // --------------------------------
        ArduinoConnection[] arduinoConnections = [];
        Stopwatch discoveryTimer = Stopwatch.StartNew();
        
        string[] candidatePortNames = [];
        int candidateIndex = 0;
        
        Console.WriteLine("- Searching for device ...");
        logger.LogInfo("Search for devices started.");
        
        while (readSuccesfull)
        {
            for (int i = 0; i < arduinoConnections.Length; i++)
            {
                string? receivedData = arduinoConnections[i].ReadMessage();
                    
                if (receivedData != null)
                {
                    ProcessMessage(receivedData, arduinoConnections[i].GetDevice().Id, definedMacros, macroPadController, macOsController);
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
                
            Arduino? discoveredDevice = arduinoController.UpdateDiscovery(arduinoConnections);

            if (discoveredDevice != null)
            {
                arduinoConnections = arduinoController.AddConnection(arduinoConnections, discoveredDevice, definedConfig.BaudRate);

                bool deviceHasMacros = IsDeviceConfigured(definedMacros, discoveredDevice.Id);
                bool deviceHasBeenConfigured = false;
                
                if (!deviceHasMacros)
                {
                    deviceHasBeenConfigured = configurationManager.CreateNewMacrosPreset(ref definedMacros, discoveredDevice.Id);
                    configurationManager.WriteMacros(definedMacros);
                    backupManager.CreateBackup();

                    if (deviceHasBeenConfigured)
                    {
                        // TODO - open settings app for configuring the Macros
                    }
                }
            }

            Thread.Sleep(10);
        }
    }

    /// <summary>
    /// Routes a complete device message to macro execution or identity and invalid-message reporting.
    /// </summary>
    /// <param name="receivedData">A non-null complete message returned by the connection.</param>
    /// <param name="deviceId">The sending device's ID, used for macro matching and identity checks.</param>
    /// <param name="definedMacros">The current macro mappings, including presets added for newly discovered devices.</param>
    /// <param name="macroPadController">Finds the mapping for the device ID and button code.</param>
    /// <param name="macOsController">Executes the selected keyboard or application action.</param>
    /// <remarks>Unknown codes execute no action. Macro-execution errors propagate to the caller.</remarks>
    private static void ProcessMessage(string receivedData, string deviceId, Macro[] definedMacros, MacroPadController macroPadController,
        MacOSController macOsController)
    {
        if (int.TryParse(receivedData, out int buttonCode))
        {
            Macro? macro = macroPadController.FindMacro(
                definedMacros, buttonCode, deviceId);

            if (macro != null)
            {
                macOsController.ExecuteMacro(macro);
            }
        }
        else
        {
            string[] parts = receivedData.Split(':', 2);

            if (parts.Length == 2 &&
                parts[0] == "MACROPAD" &&
                parts[1] == deviceId)
            {
                Console.WriteLine($"- Identity reply received from {deviceId}.");
            }
            else
            {
                Console.WriteLine($"- Invalid message received: {receivedData}.");
            }
        }
    }

    /// <summary>
    /// Checks whether any mapping uses the supplied device ID.
    /// </summary>
    /// <param name="definedMacros">The current mappings, containing no null records.</param>
    /// <param name="deviceId">The ID to compare exactly, including case and spaces.</param>
    /// <returns>True if at least one mapping belongs to the device.</returns>
    /// <remarks>
    /// Does not verify that all five buttons have usable actions. The current loop scans the entire array.
    /// </remarks>
    private static bool IsDeviceConfigured(Macro[] definedMacros, string deviceId)
    {
        bool deviceHasMacros = false;
        bool stopLoop = false;
        
        for (int i = 0; i < definedMacros.Length && !stopLoop; i++)
        {
            if (definedMacros[i].ArduinoDeviceId == deviceId)
            {
                deviceHasMacros = true;
                stopLoop = false;
            }
        }
        
        return deviceHasMacros;
    }
}