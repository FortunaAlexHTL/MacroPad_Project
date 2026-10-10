using System.IO.Ports;
using MacroPad_application.Models;

namespace MacroPad_application.Controllers;

public class ArduinoController
{
    private const int MaxTryingAttempts = 5;

    /// <summary>
    /// Searches macOS USB modem ports for boards replying to the MacroPad identification command.
    /// </summary>
    /// <param name="arduinoConnection">Existing connections whose listening ports are skipped.</param>
    /// <param name="baudRate">The baud rate used to contact each candidate.</param>
    /// <returns>An array of discovered port paths and device names, or an empty array if none match.</returns>
    /// <remarks>
    /// Opens a temporary port for each attempt, waits for startup, and reads one identity reply.
    /// Timeouts and malformed replies retry up to five times; access, I/O, and disposed-port errors
    /// stop attempts for that port. Each temporary connection is disposed before continuing.
    /// This method blocks during probing and waits two seconds after scanning all candidates.
    /// A button message arriving before the identity reply can cause a board to be missed.
    /// Returned names are not guaranteed to be unique, and returned ports are not left open.
    /// </remarks>
    public Arduino[] GetArduinos(ArduinoConnection[] arduinoConnection, int baudRate)
    {
        string[] portNames = SerialPort.GetPortNames();
        Arduino[] tempArduinos = new Arduino[portNames.Length];
        int validPortsCount = 0;
        
        for (int i = 0; i < portNames.Length; i++)
        {
            string portName = portNames[i];
            
            if (portName.StartsWith("/dev/cu.usbmodem") && !IsPortAlreadyOpen(portName, arduinoConnection))
            {
                Console.Write($"\n- Checking: {portName} ");

                bool stopLoop = false;
                for (int q = 0; q < MaxTryingAttempts && !stopLoop; q++)
                {
                    try
                    {
                        Console.Write(".");
                        using SerialPort arduino = new SerialPort(portName, baudRate);
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
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine($"\n- {portName} did not reply in time.\n");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine($"\n- {portName} is unavailable or in use.\n");
                        stopLoop = true;
                    
                    }
                    catch (Exception exception) when (exception is IOException || exception is ObjectDisposedException)
                    {
                        Console.WriteLine($"\n- Cannot communicate with {portName}: {exception.Message}\n");
                        stopLoop = true;
                    }
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

    /// <summary>
    /// Creates a connection object for each discovered device and attempts to open it.
    /// </summary>
    /// <param name="discoveredDevices">Device records returned by discovery.</param>
    /// <param name="baudRate">The baud rate for the listening connections.</param>
    /// <returns>One connection per device, including inactive objects whose handled opening attempt failed.</returns>
    public ArduinoConnection[] OpenConnections(Arduino[] discoveredDevices, int baudRate)
    {
        ArduinoConnection[] arduinoConnections = new ArduinoConnection[discoveredDevices.Length];

        for (int i = 0; i < discoveredDevices.Length; i++)
        {
            arduinoConnections[i] = new ArduinoConnection(discoveredDevices[i], baudRate);
            arduinoConnections[i].Open();
        }

        return arduinoConnections;
    }

    /// <summary>
    /// Disconnects every non-null connection in the supplied array.
    /// </summary>
    /// <param name="receivedArduinoConnections">Existing objects to disconnect; the array itself is unchanged.</param>
    /// <remarks>Disconnected objects are disposed and should be replaced before connecting again.</remarks>
    public void DisconnectConnections(ArduinoConnection[] receivedArduinoConnections)
    {
        for (int i = 0; i < receivedArduinoConnections.Length; i++)
        {
            if (receivedArduinoConnections[i] != null)
            {
                receivedArduinoConnections[i].Disconnect();
            }
        }
    }

    /// <summary>
    /// Checks whether at least one connection is marked as listening.
    /// </summary>
    /// <param name="arduinoConnections">Initialized connection objects to inspect.</param>
    /// <returns>True if any connection is listening; false for an empty array or all inactive connections.</returns>
    /// <remarks>Reads application state only; it does not probe whether hardware is still attached.</remarks>
    public bool IsAnyDeviceListening(ArduinoConnection[] arduinoConnections)
    {
        bool result = false;

        for (int i = 0; i < arduinoConnections.Length; i++)
        {
            result = result || arduinoConnections[i].GetListeningStatus();
        }

        return result;
    }

    /// <summary>
    /// Checks whether a listening connection already uses the given port path.
    /// </summary>
    /// <param name="portName">The serial port path to compare.</param>
    /// <param name="arduinoConnections">Existing connections; null entries are skipped.</param>
    /// <returns>True when a listening connection has the same port path.</returns>
    /// <remarks>Uses the connection's listening flag rather than querying the operating system.</remarks>
    public bool IsPortAlreadyOpen(string portName, ArduinoConnection[] arduinoConnections)
    {
        bool result = false;

        for (int i = 0; i < arduinoConnections.Length && !result; i++)
        {
            bool isValid = arduinoConnections[i] != null;
            isValid = isValid && arduinoConnections[i].GetListeningStatus();
            
            if (isValid)
            {
                result = arduinoConnections[i].GetDevice().Port == portName;
            }
        }
        
        return result;
    }

    /// <summary>
    /// Lists USB modem port candidates not already used by a listening connection.
    /// </summary>
    /// <param name="arduinoConnections">Connections whose listening port paths should be excluded.</param>
    /// <returns>Available candidate paths beginning with /dev/cu.usbmodem, possibly an empty array.</returns>
    /// <remarks>Does not open ports or perform the handshake; candidates are not yet confirmed MacroPads.</remarks>
    public string[] GetNewPortNames(ArduinoConnection[] arduinoConnections)
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