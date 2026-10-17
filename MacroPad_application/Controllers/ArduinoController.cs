// Description: Discovers serial boards, assigns unconfigured IDs, and manages connection arrays.

namespace MacroPad_application.Controllers;

using System.IO.Ports;
using System.Diagnostics;
using System.Security.Cryptography;

using Connections;
using Models;

public class ArduinoController
{
    private const int StartupDelayMilliseconds = 2000;
    private const int ReplyTimeoutMilliseconds = 2000;
    private Logger _logger = new();
    private DiscoveryStage _discoveryStage = DiscoveryStage.Idle;
    private SerialPort? _candidatePort = null;
    private string _assignedId = "";
    private Stopwatch _discoveryTime = new();
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
            isValid = isValid && arduinoConnections[i].Listening;

            if (isValid)
            {
                result = arduinoConnections[i].Device.Port == portName;
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
        string[] hardwarePorts = [];
        string[] developmentPorts = [];
        try
        {
            hardwarePorts = SerialPort.GetPortNames();
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            _logger.LogWarning($"Could not scan serial ports: {exception.Message}");
        }

        try
        {
            // Development-only simulated ports. Remove before shipping.
            developmentPorts = Directory.GetFiles("/tmp", "cu.usbmodem*");
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            _logger.LogWarning($"Could not scan simulated ports: {exception.Message}");
        }

        string[] totalPortNames = hardwarePorts.Concat(developmentPorts).Distinct().ToArray();
        string[] tempPortNames = new string[totalPortNames.Length];
        int counter = 0;

        for (int i = 0; i < totalPortNames.Length; i++)
        {
            bool isCandidate = totalPortNames[i].StartsWith("/dev/cu.usbmodem") ||
                               totalPortNames[i].StartsWith("/tmp/cu.usbmodem");
            if (isCandidate && !IsPortAlreadyOpen(totalPortNames[i], arduinoConnections))
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

    /// <summary>Opens a candidate port and starts its two-second startup wait.</summary>
    /// <param name="portName">The serial path to probe.</param>
    /// <param name="baudRate">The configured serial speed.</param>
    /// <remarks>Does nothing while another candidate is being processed.</remarks>
    public void StartDiscovery(string portName, int baudRate)
    {
        if (!IsDiscoveryRunning())
        {
            try
            {
                _candidatePort = new SerialPort(portName, baudRate);
                _candidatePort.WriteTimeout = ReplyTimeoutMilliseconds;
                _candidatePort.NewLine = "\n";
                _candidatePort.Open();
                _discoveryTime.Restart();
                _discoveryStage = DiscoveryStage.WaitingForStartup;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                                              || exception is InvalidOperationException || exception is ArgumentException)
            {
                _logger.LogWarning($"Cannot open discovery port {portName}: {exception.Message}");
                ResetDiscovery();
            }
        }
    }

    /// <summary>Advances the existing handshake without sleeping through its waiting stages.</summary>
    /// <param name="arduinoConnections">Connections whose IDs cannot be assigned to another board.</param>
    /// <returns>A confirmed device, or null while waiting or after failure.</returns>
    /// <remarks>Processes one complete reply per call. All handshake reads and writes share the same error handling.</remarks>
    public Arduino? UpdateDiscovery(ArduinoConnection[] arduinoConnections)
    {
        Arduino? discoveredDevice = null;
        if (_candidatePort != null)
        {
            try
            {
                switch (_discoveryStage)
                {
                    case DiscoveryStage.WaitingForStartup:
                        if (_discoveryTime.ElapsedMilliseconds >= StartupDelayMilliseconds)
                        {
                            _candidatePort.WriteLine("WHO_ARE_YOU?");
                            _discoveryStage = DiscoveryStage.WaitingForIdentity;
                            _discoveryTime.Restart();
                        }
                        break;
                    case DiscoveryStage.WaitingForIdentity:
                        string? identity = ReadDiscoveryMessage();
                        if (identity != null)
                        {
                            discoveredDevice = ProcessIdentity(identity, arduinoConnections);
                        }
                        break;
                    case DiscoveryStage.WaitingForIdConfirmation:
                        string? confirmation = ReadDiscoveryMessage();
                        if (confirmation != null)
                        {
                            string[] parts = confirmation.Split(':', 2);
                            if (parts.Length == 2 && parts[0] == "OK" && parts[1] == _assignedId)
                            {
                                discoveredDevice = CompleteDiscovery(parts[1]);
                            }
                        }
                        break;
                }

                bool waitingForReply = _discoveryStage == DiscoveryStage.WaitingForIdentity ||
                                       _discoveryStage == DiscoveryStage.WaitingForIdConfirmation;
                if (discoveredDevice == null && _candidatePort != null && waitingForReply &&
                    _discoveryTime.ElapsedMilliseconds >= ReplyTimeoutMilliseconds)
                {
                    _logger.LogWarning($"Discovery timed out on {_candidatePort.PortName} during {_discoveryStage}.");
                    ResetDiscovery();
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                                              || exception is InvalidOperationException || exception is TimeoutException
                                              || exception is OverflowException)
            {
                _logger.LogWarning($"Discovery failed on {_candidatePort?.PortName}: {exception.Message}");
                ResetDiscovery();
            }
        }
        return discoveredDevice;
    }

    /// <summary>Appends available candidate text and extracts one complete reply.</summary>
    /// <returns>The next line without CRLF, or null when the line is incomplete.</returns>
    /// <remarks>The caller handles serial errors. Spaces in IDs are preserved.</remarks>
    private string? ReadDiscoveryMessage()
    {
        string? message = null;
        if (_candidatePort != null)
        {
            _discoveryBuffer += _candidatePort.ReadExisting();
            int endOfLine = _discoveryBuffer.IndexOf('\n');
            if (endOfLine != -1)
            {
                message = _discoveryBuffer.Substring(0, endOfLine).TrimEnd('\r');
                _discoveryBuffer = _discoveryBuffer.Remove(0, endOfLine + 1);
            }
        }
        return message;
    }

    /// <summary>Accepts an assigned identity or sends the existing ID-assignment command.</summary>
    /// <param name="message">A complete identity-stage line.</param>
    /// <param name="connections">Current connections checked for generated-ID collisions.</param>
    /// <returns>A device with an existing valid ID, or null while assignment is pending.</returns>
    private Arduino? ProcessIdentity(string message, ArduinoConnection[] connections)
    {
        Arduino? discoveredDevice = null;
        string[] parts = message.Split(':', 2);
        if (parts.Length == 2 && parts[0] == "MACROPAD" && _candidatePort != null)
        {
            if (Arduino.IsValidId(parts[1]))
            {
                discoveredDevice = CompleteDiscovery(parts[1]);
            }
            else
            {
                bool available = false;
                while (!available)
                {
                    _assignedId = GenerateDeviceId();
                    available = Arduino.IsValidId(_assignedId) && CheckIdAvailability(connections, _assignedId);
                }

                _candidatePort.WriteLine($"SET_ID:{_assignedId}");
                _discoveryStage = DiscoveryStage.WaitingForIdConfirmation;
                _discoveryTime.Restart();
            }
        }
        return discoveredDevice;
    }

    /// <summary>Captures the confirmed device before releasing its discovery port.</summary>
    /// <param name="id">The validated or confirmed device ID.</param>
    /// <returns>The discovered device record.</returns>
    private Arduino CompleteDiscovery(string id)
    {
        if (_candidatePort == null)
        {
            throw new InvalidOperationException("Discovery has no candidate port.");
        }

        Arduino device = new Arduino(_candidatePort.PortName, id);
        ResetDiscovery();
        return device;
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

        for (int i = 0; i < arduinoConnections.Length && available; i++)
        {
            if (arduinoConnections[i].Device.Id == id)
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

    /// <summary>Clears discovery state and releases the candidate port, including during shutdown.</summary>
    /// <remarks>Expected close errors are logged after the state has been cleared.</remarks>
    public void ResetDiscovery()
    {
        SerialPort? port = _candidatePort;
        _candidatePort = null;
        _discoveryStage = DiscoveryStage.Idle;
        _discoveryBuffer = "";
        _assignedId = "";
        _discoveryTime.Reset();

        try
        {
            if (port != null)
            {
                port.Dispose();
            }
        }
        catch (Exception exception) when (exception is IOException || exception is InvalidOperationException
                                          || exception is UnauthorizedAccessException)
        {
            _logger.LogWarning($"Could not close discovery port: {exception.Message}");
        }
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
        bool canConnect = true;
        if (baudRate <= 0)
        {
            _logger.LogError("Cannot open a connection with a non-positive baud rate.");
            canConnect = false;
        }
        else if (!Arduino.IsValidId(discoveredDevice.Id))
        {
            Console.WriteLine($"- Cannot connect {discoveredDevice.Port}: invalid or unconfigured device ID.");
            canConnect = false;
        }

        for (int i = 0; i < existingConnections.Length && canConnect; i++)
        {
            if (existingConnections[i].Listening &&
                existingConnections[i].Device.Id == discoveredDevice.Id &&
                existingConnections[i].Device.Port != discoveredDevice.Port)
            {
                Console.WriteLine($"- Duplicate device ID {discoveredDevice.Id} on {discoveredDevice.Port}. Assign different IDs before using both boards.");
                canConnect = false;
            }
        }

        if (canConnect && !IsPortAlreadyOpen(discoveredDevice.Port, existingConnections))
        {
            ArduinoConnection discoveredConnection = new ArduinoConnection(discoveredDevice, baudRate);
            discoveredConnection.Open();
            if (discoveredConnection.Listening)
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
            if (existingConnections[i].Listening)
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
                if (existingConnections[i].Listening)
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
