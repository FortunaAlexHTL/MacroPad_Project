// Description: Reads general configuration and macro records from JSON files.

using System.Text.Json;
using MacroPad_application.Models;

namespace MacroPad_application
{
    public class ConfigurationReader
    {
        private const string ConfigFileName = "config.json";
        private const string MacroFileName = "macros.json";
        private bool _isMacrosFileReadable = true;
        private bool _isConfigFileReadable = true;
        
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
            Macro[]? macros = [];

            try
            {
                string fileContents = File.ReadAllText(path);
                macros = JsonSerializer.Deserialize<Macro[]>(fileContents);

                if (macros == null)
                {
                    throw new InvalidDataException("The macro file must contain an array.");
                }

                for (int i = 0; i < macros.Length; i++)
                {
                    if (macros[i] == null || !Arduino.IsValidId(macros[i].ArduinoDeviceId))
                    {
                        throw new InvalidDataException($"Macro {i + 1} requires ArduinoDeviceId with exactly six printable ASCII characters (000000 is reserved). Older ArduinoDeviceName mappings must be updated to the board's assigned ID; the file has not been overwritten.");
                    }
                }
            }
            catch (FileNotFoundException)
            {
                _isMacrosFileReadable = false;
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
        private Config ReadConfig(string path)
        {
            string fileContents = File.ReadAllText(path);
            Config? config = JsonSerializer.Deserialize<Config>(fileContents);

            if (config == null)
            {
                throw new InvalidDataException("Config file is invalid");
            }
            
            return config;
        }
        
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

        private bool CreateFilesIfNoneExist(string configurationDirectoryPath)
        {
            bool createSuccess = false;
            
            if (!_isConfigFileReadable)
            {
                string defaultConfigPath = Path.Combine(AppContext.BaseDirectory, "Configuration", "config.json");
                string configPath = Path.Combine(configurationDirectoryPath, ConfigFileName);
                File.Copy(defaultConfigPath, configPath);
                _isConfigFileReadable = true;
                createSuccess = true;
            }
            
            if (!_isMacrosFileReadable)
            {
                string defaultMacrosPath = Path.Combine(AppContext.BaseDirectory, "Configuration", "macros.json");
                string marcrosPath = Path.Combine(configurationDirectoryPath, MacroFileName);
                File.Copy(defaultMacrosPath, marcrosPath);
                _isMacrosFileReadable = true;
                createSuccess = true;
            }
            
            if  (!_isConfigFileReadable || !_isMacrosFileReadable)
            {
                createSuccess = false;
            }
            
            return createSuccess;
        }

        public bool ReadAllFiles(ref Config? definedConfig, ref Macro[] definedMacros)
        {
            string configurationDirectoryPath = GetConfigurationDirectoryPath();
            bool readSuccessful = false;
            bool createSuccessful = false;
            AreFilesReadable(configurationDirectoryPath);

            while (!readSuccessful)
            {
                if ((_isConfigFileReadable && _isMacrosFileReadable) || createSuccessful)
                {
                    string configPath = Path.Combine(configurationDirectoryPath, ConfigFileName);
                    definedConfig = ReadConfig(configPath);

                    string macrosPath = Path.Combine(configurationDirectoryPath, MacroFileName);
                    try
                    {
                        definedMacros = ReadMacros(macrosPath);
                    }
                    catch (InvalidDataException exception)
                    {
                        Console.WriteLine($"- Cannot load macros: {exception.Message}");
                        return false;
                    }

                    readSuccessful = true;
                }
                else
                {
                    Console.WriteLine("- Files were corrupted or missing, creating new files with default template");
                    createSuccessful = CreateFilesIfNoneExist(configurationDirectoryPath);
                }
            }
            
            return readSuccessful;
        } 
        private void AreFilesReadable(string configurationDirectoryPath)
        {
            try
            {
                string configPath = Path.Combine(configurationDirectoryPath, ConfigFileName);
                File.Open(configPath, FileMode.Open).Close();
                _isConfigFileReadable = true;
            }
            catch (FileNotFoundException)
            {
                _isConfigFileReadable = false;
            }

            try
            {
                string macroPath = Path.Combine(configurationDirectoryPath, MacroFileName);
                File.Open(macroPath, FileMode.Open).Close();
                _isMacrosFileReadable  = true;
            }
            catch (FileNotFoundException)
            {
                _isMacrosFileReadable = false;
            }
        }
    }
}