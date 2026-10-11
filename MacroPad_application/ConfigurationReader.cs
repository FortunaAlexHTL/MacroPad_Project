namespace MacroPad_application
{
    using Models;
    using System.Text.Json;
    
    public class ConfigurationReader
    {
        private const string ConfigFileName = "config.json";
        private const string MacroFileName = "macros.json";
        private bool _isMacrosFileReadable = true;
        private bool _isConfigFileReadable = true;
        private Logger _logger = new Logger();
        
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
                    catch (InvalidDataException exception)
                    {
                        readSuccessfulConfig = false;
                    }

                    string macrosPath = Path.Combine(configurationDirectoryPath, MacroFileName);
                    try
                    {
                        definedMacros = ReadMacros(macrosPath);
                        readSuccessfulMacros = true;
                    }
                    catch (InvalidDataException exception)
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
            
            if (!readSuccessful)
            {
                _logger.LogError("Configuration could not be loaded after recovery attempts. Default files were restored.");
                return false;
            }
            
            return readSuccessful;
        } 
        
        /// <summary>
        /// Reads a JSON array of macro definitions and converts it into macro records.
        /// </summary>
        /// <param name="path">The file path; relative paths use the current working directory.</param>
        /// <returns>The deserialized macro array, which can be empty.</returns>
        /// <exception cref="InvalidDataException">The JSON deserializes to null.</exception>
        /// <exception cref="JsonException">The JSON cannot be converted into a macro array.</exception>
        /// <remarks>
        /// File-reading errors propagate to the caller. Device IDs are checked; other macro values
        /// are not yet validated by this method.
        /// </remarks>
        private Macro[] ReadMacros(string path)
        {

            Macro[]? macros = null;
            
            try
            {
                string fileContents = File.ReadAllText(path);
                macros = JsonSerializer.Deserialize<Macro[]>(fileContents);

                if (macros == null)
                {
                    throw new InvalidDataException();
                }

                for (int i = 0; i < macros.Length; i++)
                {
                    if (macros[i] == null || !Arduino.IsValidId(macros[i].ArduinoDeviceId))
                    {
                        _logger.LogError($"Macro ID {macros[i].ArduinoDeviceId} is not valid");
                        //TODO - Implement assignment of newly generated ID - with call of settings for Macros setup.
                    }
                }
            }
            catch (Exception exception) when (exception is FileNotFoundException  || exception is InvalidDataException
                                              || exception is JsonException)
            {
                _logger.LogWarning("Macro file could not be read. Backup restauration will be tried, if that fails a default file will be created.");
                BackupRestauration("macros");
                throw new InvalidDataException("Macro configuration could not be loaded on this attempt.");
            }
            
            return macros!;
        }

        /// <summary>
        /// Reads the baud rate from a JSON file.
        /// </summary>
        /// <param name="path">The file path; relative paths use the current working directory.</param>
        /// <returns>The deserialized configuration record.</returns>
        /// <exception cref="InvalidDataException">The JSON deserializes to null.</exception>
        /// <exception cref="JsonException">The JSON cannot be converted into a configuration record.</exception>
        /// <remarks>
        /// File-reading errors propagate to the caller. Configuration values are not validated.
        /// Macros are loaded separately from macros.json in the configuration directory.
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
                BackupRestauration("config");
                throw new InvalidDataException("Configuration file could not be loaded on this attempt.");
            }

            return config;
        }
        
        // Helper Methods
        // --------------
        
        /// <summary>
        /// Creates the current user's MacroPad configuration directory if it does not exist.
        /// </summary>
        /// <returns>The path to Library/Application Support/MacroPad under the user's home directory.</returns>
        /// <remarks>Creates the directory only; configuration files must already be supplied separately.</remarks>
        private string GetConfigurationDirectoryPath()
        {
            string homeDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);
        
            string configurationDirectory = Path.Combine(
                homeDirectory, "Library", "Application Support", "MacroPad");

            Directory.CreateDirectory(configurationDirectory);
        
            return configurationDirectory;
        }

        private string GetBackupDirectoryPath()
        {
            string homeDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);
            
            string backupDirectory = Path.Combine(homeDirectory, "Library", "Application Support", "MacroPad",  "Backups");
            
            Directory.CreateDirectory(backupDirectory);
            
            return backupDirectory;
        }

        public void CreateBackup()
        {
            string timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
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
            catch (Exception exception) when (exception is FileNotFoundException || exception is InvalidDataException)
            {
                _logger.LogError($"Backup could not be created: {exception.Message}");
            }
        }
        
        private void BackupRestauration(string fileType)
        {
            string backupDirectoryPath = GetBackupDirectoryPath(); 
            string configurationDirectory = GetConfigurationDirectoryPath();
            
            if (fileType == "config")
            {
                try
                {
                    string? latestConfigBackupPath = Directory.GetFiles(backupDirectoryPath, "config_*.json")
                        .OrderByDescending(file => File.GetLastWriteTime(file))
                        .FirstOrDefault();
                    string configPath = Path.Combine(configurationDirectory, ConfigFileName);

                    if (latestConfigBackupPath != null)
                    {
                        File.Copy(latestConfigBackupPath, configPath,  true);
                        _logger.LogInfo("Config file backup restored successfully.");
                        _isConfigFileReadable = true;
                    }
                    else
                    {
                        throw new FileNotFoundException();
                    }
                }
                catch (FileNotFoundException exception)
                {
                    _logger.LogError($"Config file backup could not be restored: {exception.Message}");
                    _isConfigFileReadable = false;
                }
            }
            else if (fileType == "macros")
            {
                try
                {
                    string? latestMacroBackupPath = Directory.GetFiles(backupDirectoryPath, "macros_*.json")
                        .OrderByDescending(file => File.GetLastWriteTime(file))
                        .FirstOrDefault();
                    string macroPath = Path.Combine(configurationDirectory, MacroFileName);

                    if (latestMacroBackupPath != null)
                    {
                        File.Copy(latestMacroBackupPath, macroPath, true);
                        _logger.LogInfo("Macro file backup restored successfully.");
                        _isMacrosFileReadable = true;
                    }
                    else
                    {
                        throw new FileNotFoundException();
                    }
                }
                catch (FileNotFoundException exception)
                {
                    _logger.LogError($"Macro file backup could not be restored: {exception.Message}");
                    _isMacrosFileReadable = false;
                }
            }
        }

        private string VerifyFileForBackup(string currentFilePath, string fileType)
        {
            string errorMessage = "";
            
            if (fileType == "config")
            {
                try
                {
                    string fileContents = File.ReadAllText(currentFilePath);
                    Config? config = JsonSerializer.Deserialize<Config>(fileContents);
                }
                catch (JsonException)
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
                }
                catch (JsonException)
                {
                    errorMessage += "Macro";
                }
            }
            
            return errorMessage;
        }
        
        private bool CreateFilesIfNoneExist(string configurationDirectoryPath)
        {
            bool createSuccess = false;
            
            if (!_isConfigFileReadable)
            {
                string defaultConfigPath = Path.Combine(AppContext.BaseDirectory, "Configuration", "config.json");
                string configPath = Path.Combine(configurationDirectoryPath, ConfigFileName);
                File.Copy(defaultConfigPath, configPath, true);
                _isConfigFileReadable = true;
                createSuccess = true;
            }
            
            if (!_isMacrosFileReadable)
            {
                string defaultMacrosPath = Path.Combine(AppContext.BaseDirectory, "Configuration", "macros.json");
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
                BackupRestauration("config");
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
                BackupRestauration("macros");
            }
        }
    }
}