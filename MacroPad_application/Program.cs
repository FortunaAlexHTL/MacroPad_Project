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
        ConfigurationReader configurationReader = new ConfigurationReader();
        MacController macController = new MacController();
        MacroPadController  macroPadController = new MacroPadController();
        
        string configurationDirectoryPath = GetConfigurationDirectoryPath();
        
        string configPath = Path.Combine(configurationDirectoryPath, ConfigFileName);
        Config definedConfig = configurationReader.ReadConfig(configPath);
        
        string macrosPath = Path.Combine(configurationDirectoryPath, definedConfig.MacrosFile);
        Macro[] definedMacro = configurationReader.ReadMacros(macrosPath);
        
        Console.WriteLine("MacroPad is running!\n");
        bool isListening = false;
        Console.WriteLine("Searching for device ");
        SerialPort[] arduino = [];
        
        while (true)
        {
            if (!isListening)
            {
                for (int i = 0; i < arduino.Length; i++)
                {
                    arduino[i].Dispose();
                }
                
                Arduino[] arduinoObjects = GetArduinos(definedConfig);
                arduino = new SerialPort[arduinoObjects.Length];

                try
                {
                    for (int i = 0; i < arduinoObjects.Length; i++)
                    {
                        arduino[i] = new SerialPort(arduinoObjects[i].Port, definedConfig.BaudRate);
                        arduino[i].Open();
                    }
                }
                catch (Exception exception) when (exception is UnauthorizedAccessException || exception is IOException)
                {
                    Console.WriteLine($"Could not open the port: {exception.Message}");
                    isListening = false;
                }
                if (arduino.Length > 0)
                {
                    isListening = true;
                }
                else
                {
                    isListening = false;
                    Console.Write(".");
                    Thread.Sleep(1000);
                }
                
            }

            while (isListening)
            {
                Console.WriteLine();
                string? receivedData = null;

                try
                {
                    receivedData = arduino[0].ReadLine();
                }
                catch (Exception exception) when (
                    exception is IOException ||
                    exception is InvalidOperationException)
                {
                    Console.WriteLine($"Serial read failed: {exception.Message}. Reconnect the Arduino.");
                    isListening = false;
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
                        Console.WriteLine($"Invalid button code: {receivedData}.");
                    }
                }
            }
        }
    }

    static string GetConfigurationDirectoryPath()
    {
        string homeDirectory = Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile);
        
        string configurationDirectory = Path.Combine(
            homeDirectory, "Library", "Application Support", "MacroPad");

        Directory.CreateDirectory(configurationDirectory);
        
        return configurationDirectory;
    }

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
                Console.WriteLine($"\nChecking {portName}...");

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
                    Console.WriteLine($"{portName} did not reply in time.");
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine($"{portName} is unavailable or in use.");
                    
                }
                catch (IOException exception)
                {
                    Console.WriteLine($"Cannot communicate with {portName}: {exception.Message}");
                }
            }
        }

        Arduino[] arduinos = new Arduino[validPortsCount];

        for (int i = 0; i < arduinos.Length; i++)
        {
            arduinos[i] = tempArduinos[i];
        }
        
        return arduinos;
    }
}