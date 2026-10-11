// Description: Discovers serial boards, assigns unconfigured IDs, and manages connection arrays.

namespace MacroPad_application.Controllers;

using System.IO.Ports;
using System.Diagnostics;
using System.Security.Cryptography;

using Connections;
using Models;

public class ArduinoController
{
    private DiscoveryStage _discoveryStage = DiscoveryStage.Idle;
    private SerialPort? _candidatePort = null;
    private string _assignedId = "";
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
    /// <returns>Candidate paths beginning with /dev/cu.usbmodem or the development-only /tmp/cu.usbmodem prefix.</returns>
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

    /// <summary>
    /// Opens one candidate port and begins its startup wait when discovery is idle.
    /// </summary>
    /// <param name="portName">The serial port path to probe.</param>
    /// <param name="baudRate">The serial speed, which must match the firmware.</param>
    /// <remarks>
    /// Repeated calls during discovery do nothing. Opening is synchronous.
    /// Handled access and port failures reset discovery; call UpdateDiscovery to advance the handshake.
    /// </remarks>
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
                Console.WriteLine($"\n- Cannot reliably open {portName}: {exception.Message}");
                ResetDiscovery();
            }
            catch (Exception exception) when (exception is IOException || exception is ObjectDisposedException)
            {
                Console.WriteLine($"\n- Cannot communicate with {portName}: {exception.Message}\n");
                ResetDiscovery();
            }
        }
    }

    /// <summary>
    /// Advances startup, identity probing and optional ID assignment for the current candidate.
    /// </summary>
    /// <param name="arduinoConnections">Existing connections used to check generated IDs for collisions.</param>
    /// <returns>An identified device after a valid MACROPAD reply or matching assignment confirmation; otherwise null.</returns>
    /// <remarks>
    /// Waits two seconds for startup and allows two seconds for each reply stage. Processes at most
    /// one buffered line per call. Invalid IDs trigger generation and SET_ID; successful discovery closes
    /// the probe port before the caller opens a listening connection. The SET_ID write is currently
    /// outside the write-error handlers, and generated IDs are not checked against saved offline devices.
    /// </remarks>
    public Arduino? UpdateDiscovery(ArduinoConnection[] arduinoConnections)
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
                identityMessage = _discoveryBuffer.Substring(0, positionOfEndLine).TrimEnd('\r');
                _discoveryBuffer = _discoveryBuffer.Remove(0, positionOfEndLine + 1);
            }

            if (identityMessage != null)
            {
                string[] response = identityMessage.Split(':', 2);
                
                if (response.Length == 2 && response[0] == "MACROPAD" &&
                    Arduino.IsValidId(response[1]))
                {
                    discoveredDevice = new Arduino(_candidatePort.PortName, response[1]);
                    ResetDiscovery();
                }
                else if (response.Length == 2 && response[0] == "MACROPAD")
                {
                    bool available = false;
                    
                    while (!available)
                    {
                        _assignedId = GenerateDeviceId();
                        available = CheckIdAvailability(arduinoConnections, _assignedId);
                    }
                    
                    _candidatePort.WriteLine($"SET_ID:{_assignedId}");
                    _discoveryStage = DiscoveryStage.WaitingForIdConfirmation;
                    _discoveryTime.Restart();
                }
            }
        }
        else if (_discoveryStage == DiscoveryStage.WaitingForIdConfirmation && _candidatePort != null)
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
                identityMessage = _discoveryBuffer.Substring(0, positionOfEndLine).TrimEnd('\r');
                _discoveryBuffer = _discoveryBuffer.Remove(0, positionOfEndLine + 1);
            }

            if (identityMessage != null)
            {
                string[] response = identityMessage.Split(':', 2);

                if (response.Length == 2 && response[0] == "OK" && response[1] == _assignedId)
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

        if (_discoveryStage == DiscoveryStage.WaitingForIdConfirmation && _candidatePort != null &&
            _discoveryTime.ElapsedMilliseconds >= 2000)
        {
            Console.WriteLine($"{_candidatePort.PortName} did not provide a valid assignment reply in time.");
            ResetDiscovery();
        }
        
        return discoveredDevice;
    }

    /// <summary>
    /// Checks whether the supplied connections contain an ID.
    /// </summary>
    /// <param name="arduinoConnections">The connections to search, containing no null entries.</param>
    /// <param name="id">The proposed ID to compare exactly.</param>
    /// <returns>True when none of the supplied device records use the ID.</returns>
    /// <remarks>
    /// Checks all supplied records, regardless of listening status; does not inspect saved macro mappings.
    /// </remarks>
    private bool CheckIdAvailability(ArduinoConnection[] arduinoConnections, string id)
    {
        bool available = true;
        
        for (int i = 0; i < arduinoConnections.Length; i++)
        {
            if (arduinoConnections[i].GetDevice().Id == id)
            {
                available = false;
            }
        }
        
        return available;
    }
    
    /// <summary>
    /// Generates six characters using uppercase letters and decimal digits.
    /// </summary>
    /// <returns>A randomly generated ID; uniqueness and the reserved value 000000 are not checked here.</returns>
    /// <remarks>
    /// RandomNumberGenerator selects each character independently. The caller checks connection collisions.
    /// </remarks>
    private string GenerateDeviceId()
    {
        const string characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] id = new char[6];

        for (int i = 0; i < id.Length; i++)
        {
            id[i] = characters[RandomNumberGenerator.GetInt32(characters.Length)];
        }
        
        return new string(id);
    }
    
    /// <summary>
    /// Checks whether a candidate is being processed.
    /// </summary>
    /// <returns>True in any discovery stage other than Idle.</returns>
    public bool IsDiscoveryRunning()
    {
        return _discoveryStage != DiscoveryStage.Idle;
    }
    
    /// <summary>
    /// Returns discovery to idle, disposes the candidate port and clears buffered data and timing.
    /// </summary>
    /// <remarks>
    /// Disposal errors propagate. The assigned-ID field is retained until a later assignment replaces it.
    /// </remarks>
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
    
    /// <summary>
    /// Opens and appends a discovered device when its ID and port are acceptable.
    /// </summary>
    /// <param name="existingConnections">Existing non-null connection objects to preserve.</param>
    /// <param name="discoveredDevice">The identified port and device ID.</param>
    /// <param name="baudRate">The listening port’s baud rate.</param>
    /// <returns>A new array after a successful addition; otherwise the original array.</returns>
    /// <remarks>
    /// Rejects invalid IDs, duplicate active IDs on another port, and already-listening ports.
    /// Opens a fresh SerialPort rather than transferring the discovery port.
    /// </remarks>
    public ArduinoConnection[] AddConnection(ArduinoConnection[] existingConnections, Arduino discoveredDevice, int baudRate)
    {
        if (!Arduino.IsValidId(discoveredDevice.Id))
        {
            Console.WriteLine($"- Cannot connect {discoveredDevice.Port}: invalid or unconfigured device ID.");
            return existingConnections;
        }

        for (int i = 0; i < existingConnections.Length; i++)
        {
            if (existingConnections[i].GetListeningStatus() &&
                existingConnections[i].GetDevice().Id == discoveredDevice.Id &&
                existingConnections[i].GetDevice().Port != discoveredDevice.Port)
            {
                Console.WriteLine($"- Duplicate device ID {discoveredDevice.Id} on {discoveredDevice.Port}. Assign different IDs before using both boards.");
                return existingConnections;
            }
        }

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

    /// <summary>
    /// Removes entries whose listening flag is false, preserving the remaining order.
    /// </summary>
    /// <param name="existingConnections">The non-null connection objects to filter.</param>
    /// <returns>The original array if all entries are active; otherwise a new array of active connections.</returns>
    /// <remarks>
    /// Does not probe hardware or dispose removed objects; disconnection must already have cleaned them up.
    /// </remarks>
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