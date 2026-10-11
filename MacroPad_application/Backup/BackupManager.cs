// Description: Creates timestamped configuration backups and restores validated backup files.
namespace MacroPad_application.Backup;
using System.Text.Json;
using Models;

public class BackupManager
{
    private const string ConfigFileName = "config.json";
    private const string MacroFileName = "macros.json";
    private Logger _logger = new Logger();
    
    /// <summary>
    /// Validates both current files and copies them to timestamped backups.
    /// </summary>
    /// <remarks>
    /// Copies config and macros separately, so a failure can leave only one backup.
    /// Validation checks JSON structure and macro IDs, not all configuration values.
    /// Expected validation and copy failures are logged; directory-creation and logging errors can propagate.
    /// </remarks>
    public void CreateBackup()
    {
        string timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
        string configurationDirectoryPath = GetConfigurationDirectoryPath(); 
        string backupDirectoryPath = GetBackupDirectoryPath();
        
        string configFileName = $"config_{timeStamp}.json"; 
        string macrosFileName = $"macros_{timeStamp}.json";
        
        string currentConfigPath = Path.Combine(configurationDirectoryPath, ConfigFileName); 
        string currentMacrosPath = Path.Combine(configurationDirectoryPath, MacroFileName);
        
        string backupConfigPath = Path.Combine(backupDirectoryPath, configFileName); 
        string backupMacrosPath = Path.Combine(backupDirectoryPath, macrosFileName);

        try
        {
            string errorMessage = "";
                
            errorMessage += VerifyFileForBackup(currentConfigPath, "config"); 
                
            errorMessage += VerifyFileForBackup(currentMacrosPath, "macros"); 

            if (errorMessage == "ConfigMacro" )
                throw new InvalidDataException("Files are not in the correct format.");

            if (errorMessage == "Config")
                throw new InvalidDataException("Config file is not in the correct format.");
                
            if (errorMessage == "Macro")
                throw new InvalidDataException("Macro file is not in the correct format.");
                
            File.Copy(currentConfigPath, backupConfigPath);
            File.Copy(currentMacrosPath, backupMacrosPath);
        }
        catch (Exception exception) when (exception is FileNotFoundException || exception is InvalidDataException 
                                                                             || exception is IOException || exception is UnauthorizedAccessException
                                                                             || exception is DirectoryNotFoundException)
        {
            _logger.LogError($"Backup could not be created: {exception.Message}");
        }
    }
    
    /// <summary>
    /// Creates the current user’s backup directory if needed.
    /// </summary>
    /// <returns>The Backups directory inside Library/Application Support/MacroPad.</returns>
    /// <remarks>
    /// Directory-creation errors propagate.
    /// </remarks>
    private string GetBackupDirectoryPath()
    {
        string homeDirectory = Environment.GetFolderPath(
            Environment.SpecialFolder.UserProfile);
            
        string backupDirectory = Path.Combine(homeDirectory, "Library", "Application Support", "MacroPad",  "Backups");
            
        Directory.CreateDirectory(backupDirectory);
            
        return backupDirectory;
    }
    
    /// <summary>
    /// Creates the current user’s configuration directory if needed.
    /// </summary>
    /// <returns>The path to Library/Application Support/MacroPad.</returns>
    /// <remarks>
    /// Directory-creation errors propagate.
    /// </remarks>
    private string GetConfigurationDirectoryPath()
    {
        string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        string configurationDirectory = Path.Combine(homeDirectory, "Library", "Application Support", "MacroPad");

        Directory.CreateDirectory(configurationDirectory);
        
        return configurationDirectory;
    }
    
    /// <summary>
    /// Restores the newest backup for the requested file after checking its contents.
    /// </summary>
    /// <param name="fileType">Either config or macros.</param>
    /// <param name="isConfigFileReadable">Updated only when restoring config.</param>
    /// <param name="isMacrosFileReadable">Updated only when restoring macros.</param>
    /// <remarks>
    /// Marks the requested file unreadable until validation and copying succeed.
    /// Expected file-access failures are logged; older backups are not tried automatically.
    /// </remarks>
    public void BackupRestauration(string fileType, ref bool isConfigFileReadable, ref bool isMacrosFileReadable)
    {
        if (fileType != "config" && fileType != "macros")
        {
            throw new ArgumentException("File type must be config or macros.", nameof(fileType));
        }

        if (fileType == "config")
        {
            isConfigFileReadable = false;
        }
        else
        {
            isMacrosFileReadable = false;
        }

        try
        {
            string backupDirectoryPath = GetBackupDirectoryPath();
            string configurationDirectory = GetConfigurationDirectoryPath();
            string? latestBackupPath = Directory.GetFiles(backupDirectoryPath, $"{fileType}_*.json")
                .OrderByDescending(file => File.GetLastWriteTime(file))
                .FirstOrDefault();

            if (latestBackupPath == null)
            {
                throw new FileNotFoundException($"No {fileType} backup exists.");
            }

            if (VerifyFileForBackup(latestBackupPath, fileType) != "")
            {
                throw new InvalidDataException($"The newest {fileType} backup is invalid.");
            }

            string fileName = fileType == "config" ? ConfigFileName : MacroFileName;
            string destinationPath = Path.Combine(configurationDirectory, fileName);
            File.Copy(latestBackupPath, destinationPath, true);

            if (fileType == "config")
            {
                isConfigFileReadable = true;
            }
            else
            {
                isMacrosFileReadable = true;
            }
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                                          || exception is InvalidDataException || exception is JsonException)
        {
            string message = $"{fileType} backup could not be restored: {exception.Message}";
            try
            {
                _logger.LogError(message);
            }
            catch (Exception logException) when (logException is IOException || logException is UnauthorizedAccessException)
            {
                Console.WriteLine($"{message} Logging also failed: {logException.Message}");
            }

            return;
        }

        try
        {
            _logger.LogInfo($"{fileType} backup restored successfully.");
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            Console.WriteLine($"{fileType} backup restored successfully, but logging failed: {exception.Message}");
        }
    }

    /// <summary>
    /// Checks the JSON record or macro array before backup or restoration.
    /// </summary>
    /// <param name="currentFilePath">The file whose contents are checked.</param>
    /// <param name="fileType">Use config or macros to select validation.</param>
    /// <returns>Config or Macro for invalid contents; an empty string for accepted contents or an unrecognized file type.</returns>
    /// <remarks>
    /// Rejects null data, null macro entries and invalid device IDs. Other fields are not validated.
    /// File-access errors propagate to the caller.
    /// </remarks>
    private string VerifyFileForBackup(string currentFilePath, string fileType)
    {
        string errorMessage = "";
            
        if (fileType == "config")
        {
            try
            {
                string fileContents = File.ReadAllText(currentFilePath);
                Config? config = JsonSerializer.Deserialize<Config>(fileContents);

                if (config == null)
                {
                    throw new InvalidDataException();
                }
            }
            catch (Exception exception) when (exception is JsonException || exception is InvalidDataException)
            {
                errorMessage += "Config";
            }
                
        }
        else if (fileType == "macros")
        {
            try
            {
                string fileContents = File.ReadAllText(currentFilePath);
                Macro[]? macro = JsonSerializer.Deserialize<Macro[]>(fileContents);

                if (macro == null)
                {
                    throw new InvalidDataException();
                }

                for (int i = 0; i < macro.Length; i++)
                {
                    if (macro[i] == null || !Arduino.IsValidId(macro[i].ArduinoDeviceId))
                    {
                        throw new InvalidDataException($"Macro {i + 1} has an invalid device ID.");
                    }
                }
            }
            catch (Exception exception) when (exception is JsonException || exception is InvalidDataException)
            {
                errorMessage += "Macro";
            }
        }
            
        return errorMessage;
    }
}