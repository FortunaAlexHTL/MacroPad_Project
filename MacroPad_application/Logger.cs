// Description: Writes timestamped messages to the current user’s MacroPad log file.
namespace MacroPad_application;

public class Logger
{
    private bool _isLogsFileReadable = true;

    /// <summary>
    /// Writes a message with a local timestamp and the INFO level.
    /// </summary>
    /// <param name="log">The message to record.</param>
    /// <remarks>
    /// File-access errors propagate to the caller.
    /// </remarks>
    public void LogInfo(string log)
    {
        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        WriteLog($"[{timeStamp}] [INFO] {log}");
    }

    /// <summary>
    /// Writes a message with a local timestamp and the WARNING level.
    /// </summary>
    /// <param name="log">The message to record.</param>
    /// <remarks>
    /// File-access errors propagate to the caller.
    /// </remarks>
    public void LogWarning(string log)
    {
        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        WriteLog($"[{timeStamp}] [WARNING] {log}");
    }

    /// <summary>
    /// Writes a message with a local timestamp and the ERROR level.
    /// </summary>
    /// <param name="log">The message to record.</param>
    /// <remarks>
    /// File-access errors propagate to the caller.
    /// </remarks>
    public void LogError(string log)
    {
        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        WriteLog($"[{timeStamp}] [ERROR] {log}");
    }
    
    /// <summary>
    /// Creates the current user’s log directory if needed.
    /// </summary>
    /// <returns>The path to Library/Logs/MacroPad under the user’s home directory.</returns>
    private string GetLogsDirectoryPath()
    {
        string homeDirectory = Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile);
            
        string logsDirectory = Path.Combine(homeDirectory, "Library", "Logs", "MacroPad");
            
        Directory.CreateDirectory(logsDirectory);
            
        return logsDirectory;
    }
    
    /// <summary>
    /// Updates the internal flag by opening the existing log file.
    /// </summary>
    /// <param name="logsPath">The log file to check.</param>
    /// <remarks>
    /// Requests read/write access. Only a missing file sets the flag false; other access errors propagate.
    /// </remarks>
    private void IsLogsFileReadable(string logsPath)
    {
        try
        {
            File.Open(logsPath, FileMode.Open).Close();
            _isLogsFileReadable = true;
        }
        catch (FileNotFoundException)
        {
            _isLogsFileReadable = false;
        }
    }
    
    /// <summary>
    /// Adds a line to logs.txt, creating the file when missing.
    /// </summary>
    /// <param name="logs">The formatted log entry to write.</param>
    /// <remarks>
    /// Reads and rewrites the whole file for each entry. There is no size limit, rotation,
    /// concurrent-writer protection or internal console fallback; file-access errors propagate.
    /// </remarks>
    private void WriteLog(string logs)
    {
        string logPath = Path.Combine(GetLogsDirectoryPath(), "logs.txt");
        IsLogsFileReadable(logPath);

        if (_isLogsFileReadable)
        {
            string[] currentContents = File.ReadAllLines(logPath);
            string[] newContents = new string[currentContents.Length + 1];

            for (int i = 0; i < currentContents.Length; i++)
            {
                newContents[i] = currentContents[i];
            }
                
            newContents[newContents.Length - 1] = logs;
                
            File.WriteAllLines(logPath, newContents);
        }

        if (!_isLogsFileReadable)
        {
            File.Create(logPath).Close();
            string[] contents = {logs};
                
            File.WriteAllLines(logPath, contents);
        }
            
    }
}