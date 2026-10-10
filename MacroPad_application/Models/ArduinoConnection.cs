namespace MacroPad_application.Models;
using System.IO.Ports;

public class ArduinoConnection
{
    private Arduino _device;
    private SerialPort _connection;
    private bool _isListening;
    private string _incomingBuffer = "";

    // Stores discovery information and creates the serial port without opening it.
    public ArduinoConnection(Arduino arduino, int baudrate)
    {
        _device = arduino;
        _connection = new SerialPort(_device.Port, baudrate);
    }

    /// <summary>
    /// Opens this device's serial port and starts listening with an empty message buffer.
    /// </summary>
    /// <remarks>
    /// Logs access and I/O failures and disconnects the failed connection. Other errors propagate.
    /// Call on a newly constructed instance; replace instances that have been disconnected.
    /// </remarks>
    public void Open()
    {
        try
        {
            _connection.Open();
            _incomingBuffer = "";
            _isListening = true;

            Console.WriteLine($"\n- Opened {_device.Name} ...");
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException || exception is IOException)
        {
            Disconnect();
            Console.WriteLine($"- Could not open {_device.Name}: {exception.Message}.\n");
        }
    }

    /// <summary>
    /// Stops listening, discards buffered text, and disposes the serial port.
    /// </summary>
    /// <remarks>Create a new connection object to reconnect this device.</remarks>
    public void Disconnect()
    {
        _isListening = false;
        _incomingBuffer = "";
        _connection.Dispose();
    }

    /// <summary>
    /// Appends available serial text and extracts at most one complete newline-terminated message.
    /// </summary>
    /// <returns>
    /// The trimmed message, including an empty string for a blank line; null if inactive,
    /// no complete line is available, or a handled read error occurs.
    /// </returns>
    /// <remarks>
    /// Retains incomplete text and additional lines for later calls. I/O, invalid-operation,
    /// and overflow failures are logged and disconnect this device; a timeout leaves its state unchanged.
    /// The overflow handling covers the observed ReadExisting failure during unplugging.
    /// Does not interpret messages or execute macros. The incoming buffer currently has no size limit.
    /// </remarks>
    public string? ReadMessage()
    {
        int positionOfEndLine = -1;
        string? receivedData = null;

        if (_isListening)
        {
            try
            {
                string incomingText = _connection.ReadExisting();
                _incomingBuffer += incomingText;
                positionOfEndLine = _incomingBuffer.IndexOf("\n");
            }
            catch (Exception exception) when
                (exception is IOException || exception is InvalidOperationException
                                          || exception is OverflowException)
            {
                Console.WriteLine($"- Serial read failed: {_device.Name}, {exception.Message}. Reconnect it.\n");
                Disconnect();
            }
            catch (TimeoutException)
            {
                // Catch and move to next device
            }

            if (positionOfEndLine == -1)
            {
                receivedData = null;
            }
            else
            {
                receivedData = _incomingBuffer.Substring(0, positionOfEndLine).Trim();
                _incomingBuffer = _incomingBuffer.Remove(0, positionOfEndLine + 1);
            }
        }
        else
        {
            receivedData = null;
        }

        return receivedData;
    }

    // Getter Methods
    /// <summary>
    /// Gets whether this connection is marked as listening by the application.
    /// </summary>
    /// <returns>True after a successful open until the connection is disconnected.</returns>
    /// <remarks>Does not probe hardware; unplugging is detected when a read reports a failure.</remarks>
    public bool GetListeningStatus()
    {
        return _isListening;
    }

    /// <summary>
    /// Gets the discovery information associated with this connection.
    /// </summary>
    /// <returns>The device record containing the port path and reported name.</returns>
    public Arduino GetDevice()
    {
        return _device;
    }
}
