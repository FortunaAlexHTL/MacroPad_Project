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
using System.Diagnostics;

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
        Macro[] definedMacro = configurationReader.ReadMacros(macrosPath);
        // --------------------------------
        
        Console.WriteLine("- MacroPad is running ...\n");
        
        Console.WriteLine("- Searching for device ...");
        SerialPort[] arduinoConnections = [];
        Arduino[] arduinoObjects = [];
        bool isListening = false;
        bool[] isListeningArduino = [];
        string[] arduinoBuffers = [];
        
        Stopwatch discoveryTimer = Stopwatch.StartNew();
        
        while (true)
        {
            if (!isListening)
            {
                for (int i = 0; i < arduinoConnections.Length; i++)
                {
                    if (arduinoConnections[i] != null!)
                    {
                        arduinoConnections[i].Dispose();
                    }
                }
                
                arduinoObjects = arduinoController.GetArduinos(arduinoConnections, definedConfig);
                arduinoConnections = new SerialPort[arduinoObjects.Length];
                isListeningArduino = new bool[arduinoObjects.Length];
                arduinoBuffers = new string[arduinoObjects.Length];

                for (int i = 0; i < arduinoConnections.Length; i++)
                {
                    try
                    {
                        arduinoConnections[i] = new SerialPort(arduinoObjects[i].Port, definedConfig.BaudRate);
                        arduinoConnections[i].Open();
                        isListeningArduino[i] = true;
                        arduinoBuffers[i] = "";
                        
                        Console.WriteLine($"- Opened {arduinoObjects[i].Name} ...");
                    }
                    catch (Exception exception) when (exception is UnauthorizedAccessException || exception is IOException)
                    {
                        Console.WriteLine($"- Could not open {arduinoObjects[i].Name}: {exception.Message}.\n");
                        
                        if (arduinoConnections[i] != null!)
                        {
                            arduinoConnections[i].Dispose();
                        }
                        
                        isListeningArduino[i] = false;
                    }
                }

                for (int i = 0; i < isListeningArduino.Length; i++)
                {
                    isListening = isListening || isListeningArduino[i];
                }
            }
            
            while (isListening)
            {
                for (int i = 0; i < arduinoConnections.Length; i++)
                {
                    int positionOfEndLine = -1;
                    string? receivedData = null;

                    if (isListeningArduino[i])
                    {
                        try
                        {
                            string incomingText = arduinoConnections[i].ReadExisting();
                            arduinoBuffers[i] += incomingText;
                            positionOfEndLine = arduinoBuffers[i].IndexOf("\n");
                        }
                        catch (Exception exception) when (exception is IOException || exception is InvalidOperationException)
                        {
                            Console.WriteLine($"- Serial read failed: {arduinoObjects[i].Name}, {exception.Message}. Reconnect it.\n");
                            isListeningArduino[i] = false;
                            arduinoConnections[i].Dispose();
                        }
                        catch (TimeoutException)
                        {
                            isListeningArduino[i] = true;
                            // Catch and move to next device
                        }
                    }

                    if (positionOfEndLine == -1)
                    {
                        receivedData = null;
                    }
                    else
                    {
                        receivedData = arduinoBuffers[i].Substring(0, positionOfEndLine).Trim();
                        arduinoBuffers[i] = arduinoBuffers[i].Remove(0, positionOfEndLine + 1);
                    }
                    
                    if (receivedData != null)
                    {
                        if (int.TryParse(receivedData, out int buttonCode))
                        {
                            Macro? macro = macroPadController.FindMacro(
                                definedMacro, buttonCode, arduinoObjects[i].Name);

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
                                parts[1] == arduinoObjects[i].Name)
                            {
                                Console.WriteLine($"- Identity reply received from {arduinoObjects[i].Name}.");
                            }
                            else
                            {
                                Console.WriteLine($"- Invalid message received: {receivedData}.");
                            }
                        }
                    }
                    
                }

                if (discoveryTimer.ElapsedMilliseconds >= 3000)
                {
                    Console.WriteLine("- Discovery timer elapsed.");
                    discoveryTimer.Restart();
                }
                Thread.Sleep(10);
                isListening = false;

                for (int i = 0; i < isListeningArduino.Length; i++)
                {
                    isListening = isListening || isListeningArduino[i];
                }
            }
        }
    }
}
