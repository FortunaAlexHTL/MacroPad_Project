namespace MacroPad_application;

public class Logger
{
    private bool _isLogsFileReadable = true;

    public void LogInfo(string log)
    {
        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        WriteLog($"[{timeStamp}] [INFO] {log}");
    }

    public void LogWarning(string log)
    {
        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        WriteLog($"[{timeStamp}] [WARNING] {log}");
    }

    public void LogError(string log)
    {
        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        WriteLog($"[{timeStamp}] [ERROR] {log}");
    }
    
    private string GetLogsDirectoryPath()
    {
        string homeDirectory = Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile);
            
        string logsDirectory = Path.Combine(homeDirectory, "Library", "Logs", "MacroPad");
            
        Directory.CreateDirectory(logsDirectory);
            
        return logsDirectory;
    }
    
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