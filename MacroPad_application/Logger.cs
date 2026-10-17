// Description: Writes timestamped messages to the current user's MacroPad log file.
namespace MacroPad_application;

public class Logger
{
    private const int MaximumLogLines = 1000;

    /// <summary>Writes an informational message with a local timestamp.</summary>
    /// <param name="log">The message to record.</param>
    public void LogInfo(string log)
    {
        WriteLog("INFO", log);
    }

    /// <summary>Writes a warning with a local timestamp.</summary>
    /// <param name="log">The message to record.</param>
    public void LogWarning(string log)
    {
        WriteLog("WARNING", log);
    }

    /// <summary>Writes an error with a local timestamp.</summary>
    /// <param name="log">The message to record.</param>
    public void LogError(string log)
    {
        WriteLog("ERROR", log);
    }

    /// <summary>Creates the current user's log directory if needed.</summary>
    /// <returns>The path to Library/Logs/MacroPad.</returns>
    private string GetLogsDirectoryPath()
    {
        string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string logsDirectory = Path.Combine(homeDirectory, "Library", "Logs", "MacroPad");
        Directory.CreateDirectory(logsDirectory);
        return logsDirectory;
    }

    /// <summary>Writes one entry, restarting the log after 1000 existing lines.</summary>
    /// <param name="level">The severity label.</param>
    /// <param name="message">The message to record.</param>
    /// <remarks>Expected file-access failures fall back to the console rather than interrupting the caller.</remarks>
    private void WriteLog(string level, string message)
    {
        string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

        try
        {
            string logPath = Path.Combine(GetLogsDirectoryPath(), "logs.txt");
            string[] currentContents;
            try
            {
                currentContents = File.ReadAllLines(logPath);
            }
            catch (FileNotFoundException)
            {
                currentContents = [];
            }

            if (currentContents.Length >= MaximumLogLines)
            {
                File.WriteAllText(logPath, entry + Environment.NewLine);
            }
            else
            {
                File.AppendAllText(logPath, entry + Environment.NewLine);
            }
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            Console.WriteLine($"{entry} (Could not write log: {exception.Message})");
        }
    }
}
