//--------------------------------------//
//     Fortuna Alexandru Cristian       //
//--------------------------------------//
//              18.09.2026              //
//--------------------------------------//
//               MacroPad               //
//--------------------------------------//

// Description: Coordinates configuration, connected boards and macro execution.
namespace MacroPad_application;

using System.ComponentModel;
using System.Diagnostics;
using Backup;
using Connections;
using Controllers;
using Models;

class Program
{
    private static volatile bool _isRunning = true;

    static void Main(string[] args)
    {
        Logger logger = new Logger();
        ConfigurationManager configurationManager = new ConfigurationManager();
        BackupManager backupManager = new BackupManager();
        Console.WriteLine("- MacroPad is running ...\n");
        logger.LogInfo("MacroPad application started.");

        Config? config = null;
        Macro[] macros = [];
        bool loaded = configurationManager.ReadAllFiles(ref config, ref macros, out bool defaultsCreated);
        if (!loaded || config == null)
        {
            Console.WriteLine("Configuration could not be loaded. Check the log and configuration files.");
        }
        else if (defaultsCreated)
        {
            Console.WriteLine("Default files were created. Configure your settings before restarting.");
            // TODO: Open Settings for first-time configuration.
        }
        else if (config.BaudRate <= 0)
        {
            logger.LogError("BaudRate must be a positive number. Check config.json.");
        }
        else
        {
            RunApplication(config, macros, configurationManager, backupManager, logger);
        }
    }

    /// <summary>Coordinates serial reading and staged discovery until cancellation.</summary>
    /// <param name="config">The loaded serial configuration.</param>
    /// <param name="macros">The current macro mappings.</param>
    /// <param name="configurationManager">Saves presets for new devices.</param>
    /// <param name="backupManager">Backs up successfully saved presets.</param>
    /// <param name="logger">Reports errors without stopping other devices.</param>
    private static void RunApplication(Config config, Macro[] macros, ConfigurationManager configurationManager,
        BackupManager backupManager, Logger logger)
    {
        ArduinoController arduinoController = new ArduinoController();
        MacroPadController macroPadController = new MacroPadController();
        MacOSController macOsController = new MacOSController();
        ArduinoConnection[] connections = [];
        string[] candidatePorts = [];
        int candidateIndex = 0;
        Stopwatch discoveryTimer = Stopwatch.StartNew();

        _isRunning = true;
        Console.CancelKeyPress += HandleCancel;
        Console.WriteLine("- Searching for devices ...");

        try
        {
            while (_isRunning)
            {
                ReadConnections(connections, macros, macroPadController, macOsController, logger);
                connections = arduinoController.RemoveInactiveConnections(connections);

                bool candidatesFinished = candidateIndex >= candidatePorts.Length;
                if (discoveryTimer.ElapsedMilliseconds >= 3000 &&
                    !arduinoController.IsDiscoveryRunning() && candidatesFinished)
                {
                    candidatePorts = arduinoController.GetNewPortNames(connections);
                    candidateIndex = 0;
                    discoveryTimer.Restart();
                }

                if (!arduinoController.IsDiscoveryRunning() && candidateIndex < candidatePorts.Length)
                {
                    arduinoController.StartDiscovery(candidatePorts[candidateIndex], config.BaudRate);
                    candidateIndex++;
                }

                Arduino? device = arduinoController.UpdateDiscovery(connections);
                if (device != null)
                {
                    connections = arduinoController.AddConnection(connections, device, config.BaudRate);
                    if (arduinoController.IsPortAlreadyOpen(device.Port, connections))
                    {
                        macros = EnsureDevicePreset(macros, device.Id, configurationManager, backupManager, logger);
                    }
                }

                Thread.Sleep(10);
            }
        }
        finally
        {
            // finally also runs if an unexpected error leaves the application loop.
            Console.CancelKeyPress -= HandleCancel;
            arduinoController.ResetDiscovery();
            for (int i = 0; i < connections.Length; i++)
            {
                connections[i].Disconnect();
            }
            Console.WriteLine("\n- Stopping ...");
        }
    }

    /// <summary>Requests that the main loop stop after its current operation.</summary>
    /// <param name="sender">The console event source.</param>
    /// <param name="eventArgs">Cancels immediate termination so finally can close the ports.</param>
    private static void HandleCancel(object? sender, ConsoleCancelEventArgs eventArgs)
    {
        eventArgs.Cancel = true;
        _isRunning = false;
    }

    /// <summary>Processes at most one message per connection without letting an action failure stop other boards.</summary>
    /// <param name="connections">Current serial connections.</param>
    /// <param name="macros">Current macro mappings.</param>
    /// <param name="macroPadController">Finds the configured action.</param>
    /// <param name="macOsController">Executes the action.</param>
    /// <param name="logger">Records action failures with device context.</param>
    private static void ReadConnections(ArduinoConnection[] connections, Macro[] macros,
        MacroPadController macroPadController, MacOSController macOsController, Logger logger)
    {
        for (int i = 0; i < connections.Length; i++)
        {
            string? message = connections[i].ReadMessage();
            if (message != null)
            {
                try
                {
                    ProcessMessage(message, connections[i].Device.Id, macros, macroPadController, macOsController);
                }
                catch (Exception exception) when (exception is InvalidDataException || exception is InvalidOperationException
                                                  || exception is Win32Exception || exception is IOException
                                                  || exception is UnauthorizedAccessException || exception is ArgumentException
                                                  || exception is DllNotFoundException || exception is EntryPointNotFoundException)
                {
                    logger.LogError($"Action failed for device {connections[i].Device.Id}, message '{message}': {exception.Message}");
                }
            }
        }
    }

    /// <summary>Creates and saves the existing five-button preset only when a device has no mappings.</summary>
    /// <param name="macros">The mappings to preserve.</param>
    /// <param name="deviceId">The newly connected device ID.</param>
    /// <param name="configurationManager">Creates and saves the preset.</param>
    /// <param name="backupManager">Backs up the saved files.</param>
    /// <param name="logger">Reports a failed save.</param>
    /// <returns>The enlarged array after a successful save, or the original array on failure.</returns>
    private static Macro[] EnsureDevicePreset(Macro[] macros, string deviceId,
        ConfigurationManager configurationManager, BackupManager backupManager, Logger logger)
    {
        Macro[] result = macros;
        if (!IsDeviceConfigured(macros, deviceId))
        {
            Macro[] updatedMacros = macros;
            configurationManager.CreateNewMacrosPreset(ref updatedMacros, deviceId);
            bool saved = false;
            try
            {
                configurationManager.WriteMacros(updatedMacros);
                saved = true;
            }
            catch (InvalidDataException exception)
            {
                logger.LogError($"Could not save presets for {deviceId}: {exception.Message}");
            }

            if (saved)
            {
                backupManager.CreateBackup();
                // TODO: Open Settings so the user can fill in the empty actions.
                result = updatedMacros;
            }
        }
        return result;
    }

    /// <summary>Routes button codes to macros and reports identity or invalid messages.</summary>
    /// <param name="receivedData">One complete serial message.</param>
    /// <param name="deviceId">The sending device ID.</param>
    /// <param name="definedMacros">Current mappings.</param>
    /// <param name="macroPadController">Finds the matching mapping.</param>
    /// <param name="macOsController">Executes the selected action.</param>
    private static void ProcessMessage(string receivedData, string deviceId, Macro[] definedMacros,
        MacroPadController macroPadController, MacOSController macOsController)
    {
        if (int.TryParse(receivedData, out int buttonCode))
        {
            Macro? macro = macroPadController.FindMacro(definedMacros, buttonCode, deviceId);
            if (macro != null)
            {
                macOsController.ExecuteMacro(macro);
            }
        }
        else
        {
            string[] parts = receivedData.Split(':', 2);
            if (parts.Length == 2 && parts[0] == "MACROPAD" && parts[1] == deviceId)
            {
                Console.WriteLine($"- Identity reply received from {deviceId}.");
            }
            else
            {
                Console.WriteLine($"- Invalid message received: {receivedData}.");
            }
        }
    }

    /// <summary>Checks whether any mapping belongs to a device, stopping at the first match.</summary>
    /// <param name="definedMacros">The validated macro array.</param>
    /// <param name="deviceId">The device ID to match exactly.</param>
    /// <returns>True if a mapping exists; this does not mean all actions are configured.</returns>
    private static bool IsDeviceConfigured(Macro[] definedMacros, string deviceId)
    {
        bool found = false;
        for (int i = 0; i < definedMacros.Length && !found; i++)
        {
            found = definedMacros[i].ArduinoDeviceId == deviceId;
        }
        return found;
    }
}
