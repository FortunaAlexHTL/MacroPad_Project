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
    /// Expected directory, validation and copy failures are logged. The logger handles file-access failures.
    /// </remarks>
    public void CreateBackup()
    {
        try
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

            if (!IsBackupValid(currentConfigPath, "config"))
            {
                throw new InvalidDataException("Config file is not valid for backup.");
            }

            if (!IsBackupValid(currentMacrosPath, "macros"))
            {
                throw new InvalidDataException("Macro file is not valid for backup.");
            }

            File.Copy(currentConfigPath, backupConfigPath);
            File.Copy(currentMacrosPath, backupMacrosPath);
        }
        catch (Exception exception) when (exception is InvalidDataException || exception is IOException
                                          || exception is UnauthorizedAccessException)
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

            if (!IsBackupValid(latestBackupPath, fileType))
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

            _logger.LogInfo($"{fileType} backup restored successfully.");
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                                          || exception is InvalidDataException || exception is JsonException)
        {
            _logger.LogError($"{fileType} backup could not be restored: {exception.Message}");
        }

    }

    /// <summary>Checks the existing JSON and ID rules before copying a backup.</summary>
    /// <param name="path">The file to validate.</param>
    /// <param name="fileType">Either config or macros.</param>
    /// <returns>True for valid contents; false for malformed JSON, null records or invalid IDs.</returns>
    /// <remarks>File-access errors propagate to the backup operation's handler. Other action fields are not checked.</remarks>
    private bool IsBackupValid(string path, string fileType)
    {
        string contents = File.ReadAllText(path);
        bool valid = false;
        try
        {
            if (fileType == "config")
            {
                valid = JsonSerializer.Deserialize<Config>(contents) != null;
            }
            else
            {
                Macro[]? macros = JsonSerializer.Deserialize<Macro[]>(contents);
                if (macros != null)
                {
                    valid = true;
                    for (int i = 0; i < macros.Length && valid; i++)
                    {
                        valid = macros[i] != null && Arduino.IsValidId(macros[i].ArduinoDeviceId);
                    }
                }
            }
        }
        catch (JsonException)
        {
            valid = false;
        }
        return valid;
    }
}
