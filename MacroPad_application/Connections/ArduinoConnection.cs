// Description: Owns one board's serial connection, listening state, and incoming message buffer.
namespace MacroPad_application.Connections;

using System.IO.Ports;
using Models;

public class ArduinoConnection
{
    private Arduino _device;
    private SerialPort _connection;
    private bool _isListening;
    private string _incomingBuffer = "";
    private Logger _logger = new Logger();

    // Creates the port without opening it; discovery already supplies the device information.
    public ArduinoConnection(Arduino arduino, int baudrate)
    {
        _device = arduino;
        _connection = new SerialPort(_device.Port, baudrate);
    }

    /// <summary>Opens this device's port and begins listening with an empty buffer.</summary>
    /// <remarks>Expected open failures are logged and the failed connection is disposed.</remarks>
    public void Open()
    {
        try
        {
            _connection.Open();
            _incomingBuffer = "";
            _isListening = true;
            Console.WriteLine($"- Opened device {_device.Id} ...");
            _logger.LogInfo($"Opened device {_device.Id}.");
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                                          || exception is InvalidOperationException || exception is ArgumentException)
        {
            _logger.LogError($"Could not open device {_device.Id}: {exception.Message}");
            Disconnect();
        }
    }

    /// <summary>Stops listening, clears buffered text and releases the serial port.</summary>
    /// <remarks>Safe to call again during shutdown. Reconnection requires a new instance.</remarks>
    public void Disconnect()
    {
        _isListening = false;
        _incomingBuffer = "";
        try
        {
            _connection.Dispose();
        }
        catch (Exception exception) when (exception is IOException || exception is InvalidOperationException
                                          || exception is UnauthorizedAccessException)
        {
            _logger.LogWarning($"Could not close device {_device.Id}: {exception.Message}");
        }
    }

    /// <summary>Reads available text and returns at most one complete line.</summary>
    /// <returns>A line without CRLF, or null when inactive, incomplete or a read failed.</returns>
    /// <remarks>Remaining lines are buffered. A timeout keeps the connection; other handled read failures disconnect it.</remarks>
    public string? ReadMessage()
    {
        string? message = null;

        // Development only: detect disconnected virtual devices. Remove before shipping.
        if (_isListening && _device.Port.StartsWith("/tmp/") && !File.Exists(_device.Port))
        {
            Disconnect();
        }

        if (_isListening)
        {
            try
            {
                _incomingBuffer += _connection.ReadExisting();
                int endOfLine = _incomingBuffer.IndexOf('\n');
                if (endOfLine != -1)
                {
                    message = _incomingBuffer.Substring(0, endOfLine).TrimEnd('\r');
                    _incomingBuffer = _incomingBuffer.Remove(0, endOfLine + 1);
                }
            }
            catch (TimeoutException exception)
            {
                _logger.LogWarning($"Read timed out for {_device.Id}: {exception.Message}");
            }
            catch (Exception exception) when (exception is IOException || exception is InvalidOperationException
                                              || exception is OverflowException || exception is UnauthorizedAccessException)
            {
                _logger.LogWarning($"Serial read failed for {_device.Id}: {exception.Message}");
                Disconnect();
            }
        }

        return message;
    }

    // These values describe the connection; callers cannot change them.
    public bool Listening
    {
        get => _isListening;
    }

    public Arduino Device
    {
        get => _device;
    }
}
