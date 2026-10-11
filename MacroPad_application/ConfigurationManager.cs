// Description: Loads and saves JSON configuration, coordinates recovery, and creates device presets.
namespace MacroPad_application;

using Models;
using Backup;
using System.Text.Json;
    
public class ConfigurationManager
{
    private const string ConfigFileName = "config.json";
    private const string MacroFileName = "macros.json";
    private bool _isMacrosFileReadable = true;
    private bool _isConfigFileReadable = true;
    private Logger _logger = new Logger();
    private BackupManager _backupManager = new BackupManager();
        
    /// <summary>
    /// Loads configuration and macros with at most two recovery attempts.
    /// </summary>
    /// <param name="definedConfig">Receives the configuration when its read succeeds.</param>
    /// <param name="definedMacros">Receives mappings when their read succeeds.</param>
    /// <returns>True when both reads succeed without creating defaults; false on failure or when setup is required.</returns>
    /// <remarks>
    /// May restore backups or copy bundled defaults. Output values can be partially updated on failure.
    /// Some file-access and logging errors still propagate; false does not distinguish setup from failure.
    /// </remarks>
    public bool ReadAllFiles(ref Config? definedConfig, ref Macro[] definedMacros)
    {
        string configurationDirectoryPath = GetConfigurationDirectoryPath();
        bool readSuccessful = false;
        bool readSuccessfulConfig = false;
        bool readSuccessfulMacros = false; 
        bool createSuccessful = false;
        AreConfigurationFilesReadable(configurationDirectoryPath);

        for(int i = 0; i < 2 && !readSuccessful; i++)
        {
            if ((_isConfigFileReadable && _isMacrosFileReadable) || createSuccessful)
            {
                string configPath = Path.Combine(configurationDirectoryPath, ConfigFileName);
                try
                {
                    definedConfig = ReadConfig(configPath);
                    readSuccessfulConfig = definedConfig != null;
                }
                catch (InvalidDataException)
                {
                    readSuccessfulConfig = false;
                }

                string macrosPath = Path.Combine(configurationDirectoryPath, MacroFileName);
                try
                {
                    definedMacros = ReadMacros(macrosPath);
                    readSuccessfulMacros = true;
                }
                catch (InvalidDataException)
                {
                    readSuccessfulMacros = false;
                }
            }
            else
            {
                createSuccessful = CreateFilesIfNoneExist(configurationDirectoryPath);
            }

            if (readSuccessfulConfig && readSuccessfulMacros)
            {
                readSuccessful = true;
            }
        }
            
        if (!readSuccessful || createSuccessful)
        {
            _logger.LogError("Configuration could not be loaded after recovery attempts. Default files were restored.");
            readSuccessful = false;
        }
            
        return readSuccessful;
    }
        
    /// <summary>
    /// Appends five empty keyboard mappings for the supplied device ID.
    /// </summary>
    /// <param name="existingMacros">Replaced with a new array containing existing mappings and five presets.</param>
    /// <param name="deviceId">The ID to store in each new mapping.</param>
    /// <returns>True after the presets are appended.</returns>
    /// <remarks>
    /// Uses button codes 10 through 50. Does not validate IDs, check duplicates, or save the array to disk.
    /// </remarks>
    public bool CreateNewMacrosPreset(ref Macro[] existingMacros, string deviceId)
    {
        Macro[] tempMacros = new Macro[5];
        Macro[] macros = new Macro[tempMacros.Length + existingMacros.Length];

        for (int i = 0; i < tempMacros.Length; i++)
        {
            int baseButtonCode = 10;
                
            tempMacros[i] = new Macro(deviceId, 
                (i+1)*baseButtonCode, 
                "Keyboard", 
                [], 
                null);
        }

        for (int i = 0; i < existingMacros.Length; i++)
        {
            macros[i] = existingMacros[i];
        }
            
        int counter = 0;
            
        for (int i = existingMacros.Length; i < macros.Length; i++)
        {
            macros[i] = tempMacros[counter];
            counter++;
        }
            
        existingMacros = macros;
            
        return true;
    }

    /// <summary>
    /// Serializes mappings as indented JSON and overwrites the user macro file.
    /// </summary>
    /// <param name="macros">The mappings to save; individual action fields are not validated.</param>
    /// <remarks>
    /// Writes directly to the destination, so an interrupted write can damage the previous file.
    /// Null input, missing-file and JSON errors become InvalidDataException; other I/O, permission,
    /// directory-creation and logging errors can propagate.
    /// </remarks>
    public void WriteMacros(Macro[] macros)
    {
        string configurationDirectoryPath = GetConfigurationDirectoryPath(); 
        string macrosPath = Path.Combine(configurationDirectoryPath, MacroFileName);
            
        try
        {
            if (macros == null)
            {
                throw new InvalidDataException();
            }
                
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string content = JsonSerializer.Serialize(macros, options);
            File.WriteAllText(macrosPath, content);
        }
        catch (Exception exception) when (exception is FileNotFoundException  || exception is InvalidDataException
                                                                              || exception is JsonException)
        {
            _logger.LogWarning("Macro file could not be written.");
            throw new InvalidDataException("Macro configuration could not be loaded on this attempt.");
        }
    }
        
    /// <summary>
    /// Creates the current user’s configuration directory if needed.
    /// </summary>
    /// <returns>The path to Library/Application Support/MacroPad under the user’s home directory.</returns>
    /// <remarks>
    /// Does not create configuration files. Directory-creation errors propagate.
    /// </remarks>
    public string GetConfigurationDirectoryPath()
    {
        string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        string configurationDirectory = Path.Combine(homeDirectory, "Library", "Application Support", "MacroPad");

        Directory.CreateDirectory(configurationDirectory);
        
        return configurationDirectory;
    }
        
    // Helper Methods
    // --------------
    
    /// <summary>
    /// Reads macro records and checks their device IDs before returning them.
    /// </summary>
    /// <param name="path">The file path; relative paths use the current working directory.</param>
    /// <returns>The macro array, which may be empty but contains no null records or invalid IDs.</returns>
    /// <exception cref="InvalidDataException">Reading or validation failed; recovery was attempted.</exception>
    /// <remarks>
    /// Expected read failures trigger backup restoration and then notify the caller to retry reading.
    /// Restored data is not returned until the caller reads it again. Other action fields are not validated.
    /// </remarks>
    private Macro[] ReadMacros(string path)
    {
        try
        {
            string fileContents = File.ReadAllText(path);
            Macro[]? macros = JsonSerializer.Deserialize<Macro[]>(fileContents);

            if (macros == null)
            {
                throw new InvalidDataException("The macro file must contain an array.");
            }

            for (int i = 0; i < macros.Length; i++)
            {
                if (macros[i] == null || !Arduino.IsValidId(macros[i].ArduinoDeviceId))
                {
                    throw new InvalidDataException($"Macro {i + 1} requires ArduinoDeviceId with exactly six printable ASCII characters; 000000 is reserved.");
                }
            }

            _isMacrosFileReadable = true;
            return macros;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                                          || exception is InvalidDataException || exception is JsonException)
        {
            _isMacrosFileReadable = false;
            string message = $"Macro file could not be read: {exception.Message} Backup restoration will be tried.";
            try
            {
                _logger.LogWarning(message);
            }
            catch (Exception logException) when (logException is IOException || logException is UnauthorizedAccessException)
            {
                Console.WriteLine($"{message} Logging also failed: {logException.Message}");
            }

            _backupManager.BackupRestauration("macros", ref _isConfigFileReadable, ref _isMacrosFileReadable);
            throw new InvalidDataException("Macro configuration could not be loaded on this attempt.", exception);
        }
    }

    /// <summary>
    /// Reads the baud-rate record, attempting backup recovery for missing or malformed data.
    /// </summary>
    /// <param name="path">The JSON file to read.</param>
    /// <returns>The deserialized configuration record when reading succeeds.</returns>
    /// <remarks>
    /// Missing files, malformed JSON and a null record trigger restoration and then InvalidDataException
    /// so the caller can retry. BaudRate is not validated. Other access and logging errors propagate.
    /// </remarks>
    private Config? ReadConfig(string path)
    {
        Config? config = null;

        try
        {
            string fileContents = File.ReadAllText(path);
            config = JsonSerializer.Deserialize<Config>(fileContents);

            if (config == null)
            {
                throw new InvalidDataException();
            }
        }
        catch (Exception exception) when (exception is FileNotFoundException || exception is InvalidDataException
                                                                             || exception is JsonException)
        {
            _logger.LogError($"Config file could not be read, Backup restauration will be tried, if that fails a default file will be created.");
            _backupManager.BackupRestauration("config", ref _isConfigFileReadable, ref _isMacrosFileReadable);
            throw new InvalidDataException("Configuration file could not be loaded on this attempt.");
        }

        return config;
    }
        
    /// <summary>
    /// Copies bundled defaults for files whose readable flags are false.
    /// </summary>
    /// <param name="configurationDirectoryPath">The destination configuration directory.</param>
    /// <returns>True when at least one default was copied and both readable flags are true.</returns>
    /// <remarks>
    /// Uses ConfigurationFiles beside the executable. Copying overwrites the selected destination;
    /// despite the method name, an existing invalid file can be replaced. Copy failures propagate.
    /// </remarks>
    private bool CreateFilesIfNoneExist(string configurationDirectoryPath)
    {
        bool createSuccess = false;
            
        if (!_isConfigFileReadable)
        {
            string defaultConfigPath = Path.Combine(AppContext.BaseDirectory, "ConfigurationFiles", "config.json");
            string configPath = Path.Combine(configurationDirectoryPath, ConfigFileName);
            File.Copy(defaultConfigPath, configPath, true);
            _isConfigFileReadable = true;
            createSuccess = true;
        }
            
        if (!_isMacrosFileReadable)
        {
            string defaultMacrosPath = Path.Combine(AppContext.BaseDirectory, "ConfigurationFiles", "macros.json");
            string marcrosPath = Path.Combine(configurationDirectoryPath, MacroFileName);
            File.Copy(defaultMacrosPath, marcrosPath, true);
            _isMacrosFileReadable = true;
            createSuccess = true;
        }
            
        if  (!_isConfigFileReadable || !_isMacrosFileReadable)
        {
            createSuccess = false;
        }
            
        return createSuccess;
    }
        
    /// <summary>
    /// Checks whether both configuration files can be opened and attempts restoration when missing.
    /// </summary>
    /// <param name="configurationDirectoryPath">The directory containing config.json and macros.json.</param>
    /// <remarks>
    /// Updates the readable flags. File.Open currently requests read/write access and does not validate JSON.
    /// Access errors other than FileNotFoundException, and logging failures, can propagate.
    /// </remarks>
    private void AreConfigurationFilesReadable(string configurationDirectoryPath)
    {
        try
        {
            string configPath = Path.Combine(configurationDirectoryPath, ConfigFileName);
            File.Open(configPath, FileMode.Open).Close();
            _isConfigFileReadable = true;
        }
        catch (FileNotFoundException)
        {
            _logger.LogError("Configuration file could not be found.");
            _backupManager.BackupRestauration("config", ref _isConfigFileReadable, ref _isMacrosFileReadable);
        }

        try
        {
            string macroPath = Path.Combine(configurationDirectoryPath, MacroFileName);
            File.Open(macroPath, FileMode.Open).Close();
            _isMacrosFileReadable  = true;
        }
        catch (FileNotFoundException)
        {
            _logger.LogError("Macro file could not be found.");
            _backupManager.BackupRestauration("macros", ref _isConfigFileReadable, ref _isMacrosFileReadable);
        }
    }
}