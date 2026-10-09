// Description: Reads general configuration and macro records from JSON files.

using System.Text.Json;
using MacroPad_application.Models;

namespace MacroPad_application
{
    public class ConfigurationReader
    {
        /// <summary>
        /// Reads a JSON array of macro definitions and converts it into macro records.
        /// </summary>
        /// <param name="path">The file path; relative paths use the current working directory.</param>
        /// <returns>The deserialized macro array, which can be empty.</returns>
        /// <exception cref="InvalidDataException">The JSON deserializes to null.</exception>
        /// <exception cref="JsonException">The JSON cannot be converted into a macro array.</exception>
        /// <remarks>
        /// File-reading errors propagate to the caller. Individual records and their values
        /// are not validated by this method.
        /// </remarks>
        public Macro[] ReadMacros(string path)
        {
            string fileContents = File.ReadAllText(path);
            Macro[]? macros = JsonSerializer.Deserialize<Macro[]>(fileContents);

            if (macros == null)
            {
                throw new InvalidDataException("The macro file must contain an array.");
            }

            return macros;
        }

        /// <summary>
        /// Reads serial connection settings and the macro-file location from a JSON file.
        /// </summary>
        /// <param name="path">The file path; relative paths use the current working directory.</param>
        /// <returns>The deserialized configuration record.</returns>
        /// <exception cref="InvalidDataException">The JSON deserializes to null.</exception>
        /// <exception cref="JsonException">The JSON cannot be converted into a configuration record.</exception>
        /// <remarks>
        /// File-reading errors propagate to the caller. Configuration values are not validated.
        /// The caller must resolve MacrosFile relative to the configuration file's directory.
        /// </remarks>
        public Config ReadConfig(string path)
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
        public string GetConfigurationDirectoryPath()
        {
            string homeDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);
        
            string configurationDirectory = Path.Combine(
                homeDirectory, "Library", "Application Support", "MacroPad");

            Directory.CreateDirectory(configurationDirectory);
        
            return configurationDirectory;
        }
    }
}
