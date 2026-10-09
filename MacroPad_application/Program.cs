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
    private const string ConfigFileName = "config.json";
    static void Main()
    {
        // Starting up controllers and configuration reader
        ConfigurationReader configurationReader = new ConfigurationReader();
        MacController macController = new MacController();
        MacroPadController  macroPadController = new MacroPadController();
        // ------------------------------------------------
        
        // Getting configuration and macros
        string configurationDirectoryPath = GetConfigurationDirectoryPath();
        
        string configPath = Path.Combine(configurationDirectoryPath, ConfigFileName);
        Config definedConfig = configurationReader.ReadConfig(configPath);
        
        string macrosPath = Path.Combine(configurationDirectoryPath, definedConfig.MacrosFile);
        Macro[] definedMacro = configurationReader.ReadMacros(macrosPath);
        // --------------------------------
        
        Console.WriteLine("- MacroPad is running!\n");
        bool isListening = false;
        
        Console.WriteLine("- Searching for device ...");
        SerialPort[] arduinoConnections = [];
        Arduino[] arduinoObjects = [];
        
        while (true)
        {
            if (!isListening)
            {
                for (int i = 0; i < arduinoConnections.Length; i++)
                {
                    if (arduinoConnections[i] != null)
                    {
                        arduinoConnections[i].Dispose();
                    }
                }
                
                arduinoObjects = GetArduinos(definedConfig);
                arduinoConnections = new SerialPort[arduinoObjects.Length];

                for (int i = 0; i < arduinoConnections.Length; i++)
                {
                    try
                    {
                        arduinoConnections[i] = new SerialPort(arduinoObjects[i].Port, definedConfig.BaudRate);
                        arduinoConnections[i].Open();
                            
                        Console.WriteLine($"- Opened {arduinoObjects[i].Name} ...");
                    }
                    catch (Exception exception) when (exception is UnauthorizedAccessException || exception is IOException)
                    {
                        Console.WriteLine($"- Could not open {arduinoObjects[i].Name}: {exception.Message}.\n");
                        
                        if (arduinoConnections[i] != null)
                        {
                            arduinoConnections[i].Dispose();
                        }
                    }
                    
                    if (arduinoConnections.Length > 0 && arduinoConnections[i].IsOpen)
                    {
                        isListening = true;
                    }
                    else
                    {
                        isListening = false;
                    }
                }
            }
            
            while (isListening)
            {
                string? receivedData = null;

                for (int i = 0; i < arduinoConnections.Length; i++)
                {
                    try
                    {
                        arduinoConnections[i].ReadTimeout = 500;
                        receivedData = arduinoConnections[i].ReadLine().Trim();
                    }
                    catch (Exception exception) when (exception is IOException || exception is InvalidOperationException)
                    {
                        Console.WriteLine($"- Serial read failed: {arduinoObjects[i].Name}, {exception.Message}. Reconnect it.\n");
                        isListening = false;
                    }
                    catch (TimeoutException)
                    {
                        // Catch and move to next device
                    }

                    if (receivedData != null)
                    {
                        if (int.TryParse(receivedData, out int buttonCode))
                        {
                            Macro? macro = macroPadController.FindMacro(
                                definedMacro, buttonCode);

                            if (macro != null)
                            {
                                macController.ExecuteMacro(macro);
                            }
                        }
                        else
                        {
                            Console.WriteLine($"- Invalid button code: {receivedData}.\n");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Creates the current user's MacroPad configuration directory if it does not exist.
    /// </summary>
    /// <returns>The path to Library/Application Support/MacroPad under the user's home directory.</returns>
    /// <remarks>Creates the directory only; configuration files must already be supplied separately.</remarks>
    static string GetConfigurationDirectoryPath()
    {
        string homeDirectory = Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile);
        
        string configurationDirectory = Path.Combine(
            homeDirectory, "Library", "Application Support", "MacroPad");

        Directory.CreateDirectory(configurationDirectory);
        
        return configurationDirectory;
    }

    /// <summary>
    /// Searches macOS USB modem ports for boards replying to the MacroPad identification command.
    /// </summary>
    /// <param name="definedConfig">Configuration supplying the baud rate used to contact each candidate.</param>
    /// <returns>An array of discovered port paths and device names, or an empty array if none match.</returns>
    /// <remarks>
    /// Waits for board startup and reads one reply per candidate, checking its prefix and fields.
    /// Reports timeout, access, and I/O failures and disposes each discovery connection before continuing.
    /// A button message arriving before the identity reply can cause a board to be missed.
    /// Returned names are not guaranteed to be unique, and returned ports are not left open.
    /// </remarks>
    static Arduino[] GetArduinos(Config definedConfig)
    {
        string[] portNames = SerialPort.GetPortNames();
        Arduino[] tempArduinos = new Arduino[portNames.Length];
        int validPortsCount = 0;
        
        for (int i = 0; i < portNames.Length; i++)
        {
            string portName = portNames[i];
            
            if (portName.StartsWith("/dev/cu.usbmodem"))
            {
                Console.WriteLine($"- Checking: {portName} ...");

                try
                {
                    using SerialPort arduino = new SerialPort(portName, definedConfig.BaudRate);
                    arduino.ReadTimeout = 2000;
                    arduino.WriteTimeout = 2000;
                    arduino.NewLine = "\n";

                    arduino.Open();

                    // Opening the port can restart the Mega.
                    Thread.Sleep(2000);

                    arduino.WriteLine("WHO_ARE_YOU?");

                    string[] response = arduino.ReadLine().Trim().Split(':');

                    if (response.Length == 2 && response[0] == "MACROPAD" && !string.IsNullOrWhiteSpace(response[1]))
                    {
                        tempArduinos[validPortsCount] = new(portName, response[1]);
                        validPortsCount++;
                    }
                    
                    arduino.Close();
                }
                catch (TimeoutException)
                {
                    Console.WriteLine($"- {portName} did not reply in time.\n");
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine($"- {portName} is unavailable or in use.\n");
                    
                }
                catch (IOException exception)
                {
                    Console.WriteLine($"- Cannot communicate with {portName}: {exception.Message}.\n");
                }
            }
        }

        Arduino[] arduinos = new Arduino[validPortsCount];
        
        for (int i = 0; i < arduinos.Length; i++)
        {
            arduinos[i] = tempArduinos[i];
        }
        
        Thread.Sleep(2000);
        return arduinos;
    }
}
