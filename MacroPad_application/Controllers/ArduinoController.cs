using System.IO.Ports;
using MacroPad_application.Models;

namespace MacroPad_application.Controllers;

public class ArduinoController
{
    private const int MaxTryingAttempts = 5;

    /// <summary>
    /// Searches macOS USB modem ports for boards replying to the MacroPad identification command.
    /// </summary>
    /// <param name="arduinoConnections"></param>
    /// <param name="definedConfig">Configuration supplying the baud rate used to contact each candidate.</param>
    /// <returns>An array of discovered port paths and device names, or an empty array if none match.</returns>
    /// <remarks>
    /// Waits for board startup and reads one reply per candidate, checking its prefix and fields.
    /// Reports timeout, access, and I/O failures and disposes each discovery connection before continuing.
    /// A button message arriving before the identity reply can cause a board to be missed.
    /// Returned names are not guaranteed to be unique, and returned ports are not left open.
    /// </remarks>
    public Arduino[] GetArduinos(SerialPort[] arduinoConnections, Config definedConfig)
    {
        string[] portNames = SerialPort.GetPortNames();
        Arduino[] tempArduinos = new Arduino[portNames.Length];
        int validPortsCount = 0;
        
        for (int i = 0; i < portNames.Length; i++)
        {
            string portName = portNames[i];
            
            if (portName.StartsWith("/dev/cu.usbmodem") && !IsPortAlreadyOpen(portName, arduinoConnections))
            {
                Console.Write($"\n- Checking: {portName} ");

                try
                {
                    bool stopLoop = false;
                    for (int q = 0; q < MaxTryingAttempts && !stopLoop; q++)
                    {
                        Console.Write(".");
                        using SerialPort arduino = new SerialPort(portName, definedConfig.BaudRate);
                        arduino.ReadTimeout = 2000;
                        arduino.WriteTimeout = 2000;
                        arduino.NewLine = "\n";

                        arduino.Open();

                        // Opening the port can restart the Mega.
                        Thread.Sleep(2000);

                        arduino.WriteLine("WHO_ARE_YOU?");

                        string[] response = arduino.ReadLine().Trim().Split(':');

                        if (response.Length == 2 && response[0] == "MACROPAD" &&
                            !string.IsNullOrWhiteSpace(response[1]))
                        {
                            tempArduinos[validPortsCount] = new(portName, response[1]);
                            validPortsCount++;
                            stopLoop = true;
                        }

                        arduino.Close();
                    }
                    
                    Console.WriteLine();
                }
                catch (TimeoutException)
                {
                    Console.WriteLine($"- {portName} did not reply in time.\n");
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine($"- {portName} is unavailable or in use.\n");
                    
                }
                catch (Exception exception) when (exception is IOException || exception is ObjectDisposedException)
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

    public bool IsPortAlreadyOpen(string portName, SerialPort[] arduinoConnections)
    {
        bool result = false;

        for (int i = 0; i < arduinoConnections.Length && !result; i++)
        {
            bool isValid = arduinoConnections[i] != null;
            isValid = isValid && arduinoConnections[i].IsOpen;
            
            if (isValid)
            {
                result = arduinoConnections[i].PortName == portName;
            }
        }
        
        return result;
    }

    public string[] GetNewPortNames(SerialPort[] arduinoConnections)
    {
        string[] totalPortNames = SerialPort.GetPortNames();
        string[] tempPortNames = new string[totalPortNames.Length];
        int counter = 0;

        for (int i = 0; i < totalPortNames.Length; i++)
        {
            if (totalPortNames[i].StartsWith("/dev/cu.usbmodem") && !IsPortAlreadyOpen(totalPortNames[i], arduinoConnections))
            {
                tempPortNames[counter] = totalPortNames[i];
                counter++;
            }
        }

        string[] portNames = new string[counter];

        for (int i = 0; i < portNames.Length; i++)
        {
            portNames[i] = tempPortNames[i];
        }
        
        return portNames;
    }
}