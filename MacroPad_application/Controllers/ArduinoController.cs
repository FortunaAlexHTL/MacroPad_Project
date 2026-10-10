namespace MacroPad_application.Controllers;

using System.IO.Ports;
using System.Diagnostics;

using Connections;
using Models;

public class ArduinoController
{
    private DiscoveryStage _discoveryStage = DiscoveryStage.Idle;
    private SerialPort? _candidatePort = null;
    private Stopwatch _discoveryTime = new Stopwatch();
    private string _discoveryBuffer = "";
    
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
        string[] totalPortNames = SerialPort.GetPortNames()
            .Concat(Directory.GetFiles("/tmp", "cu.usbmodem*")) // TODO - Remove block before successful shipping .pkg
            .Distinct()
            .ToArray();
        string[] tempPortNames = new string[totalPortNames.Length];
        int counter = 0;

        for (int i = 0; i < totalPortNames.Length; i++)                // TODO | Remove condition down before successful shipping .pkg
        {
            if ((totalPortNames[i].StartsWith("/dev/cu.usbmodem") || totalPortNames[i].StartsWith("/tmp/cu.usbmodem")) && !IsPortAlreadyOpen(totalPortNames[i], arduinoConnections))
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

    public void StartDiscovery(string portName, int baudRate)
    {
        if (_discoveryStage == DiscoveryStage.Idle)
        {
            try
            {
                _candidatePort = new SerialPort(portName, baudRate);
                _candidatePort.WriteTimeout = 2000;
                _candidatePort.NewLine = "\n";
                _candidatePort.Open();
                _discoveryTime.Restart();
                _discoveryStage = DiscoveryStage.WaitingForStartup;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"\n- {portName} is unavailable or in use.\n");
                ResetDiscovery();
            }
            catch (InvalidOperationException exception)
            {
                Console.WriteLine($"\n- Cannot reliably open {_candidatePort.PortName}: {exception.Message}");
                ResetDiscovery();
            }
            catch (Exception exception) when (exception is IOException || exception is ObjectDisposedException)
            {
                Console.WriteLine($"\n- Cannot communicate with {portName}: {exception.Message}\n");
                ResetDiscovery();
            }
        }
    }

    public Arduino? UpdateDiscovery()
    {
        Arduino? discoveredDevice = null;
        
        if (_discoveryStage == DiscoveryStage.WaitingForStartup && _candidatePort != null && _discoveryTime.ElapsedMilliseconds >= 2000)
        {
            try
            {
                _candidatePort.WriteLine("WHO_ARE_YOU?");
                _discoveryTime.Restart();
                _discoveryStage = DiscoveryStage.WaitingForIdentity;
            }
            catch (InvalidOperationException exception)
            {
                Console.WriteLine($"\n- Cannot send identification request to {_candidatePort.PortName}: the serial port is no longer open. {exception.Message}");
                ResetDiscovery();
            }
            catch(TimeoutException)
            {
                Console.WriteLine($"\n- Timed out sending identification request to {_candidatePort.PortName}.");
                ResetDiscovery();
            }
            catch (Exception exception) when (exception is IOException || exception is ObjectDisposedException)
            {
                Console.WriteLine($"\n- Cannot communicate with {_candidatePort.PortName}: {exception.Message}\n");
                ResetDiscovery();
            }
        }
        else if (_discoveryStage == DiscoveryStage.WaitingForIdentity && _candidatePort != null)
        {
            int positionOfEndLine = -1;
            string? identityMessage = null;
            
            try
            {
                string response = _candidatePort.ReadExisting();
                _discoveryBuffer += response;
                positionOfEndLine = _discoveryBuffer.IndexOf("\n");
            }
            catch (InvalidOperationException exception)
            {
                Console.WriteLine($"\n- Cannot read identification request to {_candidatePort.PortName}: the serial port is no longer open. {exception.Message}");
                ResetDiscovery();
            }
            catch (OverflowException exception)
            {
                Console.WriteLine($"\n- Could not read identification reply from {_candidatePort.PortName}: {exception.Message}");
                ResetDiscovery();
            }
            catch (Exception exception) when (exception is IOException || exception is ObjectDisposedException)
            {
                Console.WriteLine($"\n- Cannot communicate with {_candidatePort.PortName}: {exception.Message}\n");
                ResetDiscovery();
            }

            if (positionOfEndLine != -1)
            {
                identityMessage = _discoveryBuffer.Substring(0, positionOfEndLine).Trim();
                _discoveryBuffer = _discoveryBuffer.Remove(0, positionOfEndLine + 1);
            }

            if (identityMessage != null)
            {
                string[] response = identityMessage.Split(':');
                
                if (response.Length == 2 && response[0] == "MACROPAD" &&
                    !string.IsNullOrWhiteSpace(response[1]))
                {
                    discoveredDevice = new Arduino(_candidatePort.PortName, response[1]);
                    ResetDiscovery();
                }
            }
        }

        if (_discoveryStage == DiscoveryStage.WaitingForIdentity && _candidatePort != null &&
            _discoveryTime.ElapsedMilliseconds >= 2000)
        {
            Console.WriteLine($"{_candidatePort.PortName} did not provide a valid identification reply in time.");
            ResetDiscovery();
        }
        
        return discoveredDevice;
    }

    public bool IsDiscoveryRunning()
    {
        return _discoveryStage != DiscoveryStage.Idle;
    }
    
    private void ResetDiscovery()
    {
        _discoveryStage = DiscoveryStage.Idle;
        if (_candidatePort != null)
        {
            _candidatePort.Dispose();
        }

        _discoveryBuffer = "";
        _candidatePort = null;
        _discoveryTime.Reset();
    }
    
    public ArduinoConnection[] AddConnection(ArduinoConnection[] existingConnections, Arduino discoveredDevice, int baudRate)
    {
        bool deviceIsAlreadyListening = IsPortAlreadyOpen(discoveredDevice.Port, existingConnections);

        if (!deviceIsAlreadyListening)
        {
            ArduinoConnection discoveredConnection = new ArduinoConnection(discoveredDevice,  baudRate);
            discoveredConnection.Open();

            if (discoveredConnection.GetListeningStatus())
            {
                ArduinoConnection[] newConnections = new ArduinoConnection[existingConnections.Length + 1];
                
                for (int i = 0; i < existingConnections.Length; i++)
                {
                    newConnections[i] = existingConnections[i];
                }
                
                newConnections[newConnections.Length - 1] = discoveredConnection;
                existingConnections = newConnections;
            }
        }

        return existingConnections;
    }

    public ArduinoConnection[] RemoveInactiveConnections(ArduinoConnection[] existingConnections)
    {
        int activeConnectionsCount = 0;
        
        for (int i = 0; i < existingConnections.Length; i++)
        {
            if (existingConnections[i].GetListeningStatus())
            {
                activeConnectionsCount++;
            }
        }

        if (activeConnectionsCount != existingConnections.Length)
        {
            ArduinoConnection[] activeConnections = new ArduinoConnection[activeConnectionsCount];
            int activeConnectionsIndex = 0;

            for (int i = 0; i < existingConnections.Length; i++)
            {
                if (existingConnections[i].GetListeningStatus())
                {
                    activeConnections[activeConnectionsIndex] = existingConnections[i];
                    activeConnectionsIndex++;
                }
            }
            
            existingConnections = activeConnections;
        }

        return existingConnections;
    }
}