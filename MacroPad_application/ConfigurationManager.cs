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
    private Logger _logger = new();
    private BackupManager _backupManager = new();

    /// <summary>Loads both files, with one retry after backup or default recovery.</summary>
    /// <param name="definedConfig">Updated when both files have been read successfully.</param>
    /// <param name="definedMacros">Updated when both files have been read successfully.</param>
    /// <param name="creationHappened">True if any bundled default was copied, even if later loading fails.</param>
    /// <returns>True when both files load. The caller must also check creationHappened for required setup.</returns>
    public bool ReadAllFiles(ref Config? definedConfig, ref Macro[] definedMacros, out bool creationHappened)
    {
        creationHappened = false;
        bool loaded = false;

        try
        {
            string directory = GetConfigurationDirectoryPath();
            for (int attempt = 0; attempt < 2 && !loaded; attempt++)
            {
                Config? config = TryReadConfig(Path.Combine(directory, ConfigFileName));
                Macro[]? macros = TryReadMacros(Path.Combine(directory, MacroFileName));

                if (config != null && macros != null)
                {
                    definedConfig = config;
                    definedMacros = macros;
                    loaded = true;
                }
                else if (attempt == 0)
                {
                    CopyDefaultFiles(directory, ref creationHappened);
                }
            }

            if (!loaded)
            {
                _logger.LogError("Configuration could not be loaded after recovery attempts.");
            }
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            _logger.LogError($"Configuration recovery failed: {exception.Message}");
        }

        return loaded;
    }

    /// <summary>Reads config while allowing the macro read to proceed if config needs recovery.</summary>
    /// <param name="path">The configuration file to read.</param>
    /// <returns>The loaded record, or null when this attempt failed.</returns>
    private Config? TryReadConfig(string path)
    {
        Config? config = null;
        try
        {
            config = ReadConfig(path);
        }
        catch (InvalidDataException)
        {
            // Recovery was attempted by the reader; null tells the caller to retry.
        }
        return config;
    }

    /// <summary>Reads macros while allowing a failed attempt to be retried after recovery.</summary>
    /// <param name="path">The macro file to read.</param>
    /// <returns>The loaded array, or null when this attempt failed.</returns>
    private Macro[]? TryReadMacros(string path)
    {
        Macro[]? macros = null;
        try
        {
            macros = ReadMacros(path);
        }
        catch (InvalidDataException)
        {
            // Recovery was attempted by the reader; null tells the caller to retry.
        }
        return macros;
    }

    /// <summary>Appends empty keyboard mappings for buttons 10 through 50 without saving them.</summary>
    /// <param name="existingMacros">Replaced with the enlarged array.</param>
    /// <param name="deviceId">The ID to put in each preset.</param>
    /// <returns>True after appending the presets.</returns>
    /// <remarks>The caller checks whether this device already has mappings.</remarks>
    public bool CreateNewMacrosPreset(ref Macro[] existingMacros, string deviceId)
    {
        const int numberOfButtons = 5;
        Macro[] macros = new Macro[existingMacros.Length + numberOfButtons];

        for (int i = 0; i < existingMacros.Length; i++)
        {
            macros[i] = existingMacros[i];
        }

        for (int i = 0; i < numberOfButtons; i++)
        {
            macros[existingMacros.Length + i] = new Macro(deviceId, (i + 1) * 10, "Keyboard", [], null);
        }

        existingMacros = macros;
        return true;
    }

    /// <summary>Serializes mappings as indented JSON into the user's macro file.</summary>
    /// <param name="macros">The mappings to save.</param>
    /// <exception cref="InvalidDataException">Serialization or file access failed.</exception>
    /// <remarks>Writes directly to the destination; protection against interrupted writes remains unfinished.</remarks>
    public void WriteMacros(Macro[] macros)
    {
        try
        {
            if (macros == null)
            {
                throw new InvalidDataException("The macro array cannot be null.");
            }

            string macrosPath = Path.Combine(GetConfigurationDirectoryPath(), MacroFileName);
            JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };
            string content = JsonSerializer.Serialize(macros, options);
            File.WriteAllText(macrosPath, content);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                                          || exception is InvalidDataException || exception is JsonException)
        {
            throw new InvalidDataException($"Macro file could not be saved: {exception.Message}", exception);
        }
    }

    /// <summary>Creates the current user's configuration directory if needed.</summary>
    /// <returns>The path to Library/Application Support/MacroPad.</returns>
    public string GetConfigurationDirectoryPath()
    {
        string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string directory = Path.Combine(homeDirectory, "Library", "Application Support", "MacroPad");
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>Reads macros and checks for null records and invalid device IDs.</summary>
    /// <param name="path">The macro file to read.</param>
    /// <returns>The macro array, which may be empty.</returns>
    /// <exception cref="InvalidDataException">Reading failed; backup restoration was attempted.</exception>
    /// <remarks>Other action fields are not validated here.</remarks>
    private Macro[] ReadMacros(string path)
    {
        Macro[]? macros = null;
        try
        {
            macros = JsonSerializer.Deserialize<Macro[]>(File.ReadAllText(path));
            if (macros == null)
            {
                throw new InvalidDataException("The macro file must contain an array.");
            }

            for (int i = 0; i < macros.Length; i++)
            {
                if (macros[i] == null || !Arduino.IsValidId(macros[i].ArduinoDeviceId))
                {
                    throw new InvalidDataException($"Macro {i + 1} needs a six-character printable ASCII ID other than 000000.");
                }
            }

            _isMacrosFileReadable = true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                                          || exception is InvalidDataException || exception is JsonException)
        {
            _isMacrosFileReadable = false;
            _logger.LogWarning($"Macro file could not be read: {exception.Message} Trying its backup.");
            _backupManager.BackupRestauration("macros", ref _isConfigFileReadable, ref _isMacrosFileReadable);
            throw new InvalidDataException("Macro configuration could not be loaded on this attempt.", exception);
        }

        return macros;
    }

    /// <summary>Reads the baud-rate record and attempts backup restoration after a failed read.</summary>
    /// <param name="path">The configuration file to read.</param>
    /// <returns>The deserialized configuration.</returns>
    /// <exception cref="InvalidDataException">Reading failed; backup restoration was attempted.</exception>
    private Config ReadConfig(string path)
    {
        Config? config = null;
        try
        {
            config = JsonSerializer.Deserialize<Config>(File.ReadAllText(path));
            if (config == null)
            {
                throw new InvalidDataException("The configuration file must contain an object.");
            }

            _isConfigFileReadable = true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
                                          || exception is InvalidDataException || exception is JsonException)
        {
            _isConfigFileReadable = false;
            _logger.LogWarning($"Config file could not be read: {exception.Message} Trying its backup.");
            _backupManager.BackupRestauration("config", ref _isConfigFileReadable, ref _isMacrosFileReadable);
            throw new InvalidDataException("Configuration could not be loaded on this attempt.", exception);
        }

        return config;
    }

    /// <summary>Copies defaults only for files that could not be read or restored.</summary>
    /// <param name="directory">The destination directory.</param>
    /// <param name="creationHappened">Set after each successful copy so partial recovery is recorded.</param>
    /// <remarks>The next read validates copied contents. Copy failures propagate to ReadAllFiles.</remarks>
    private void CopyDefaultFiles(string directory, ref bool creationHappened)
    {
        if (!_isConfigFileReadable)
        {
            string templatePath = Path.Combine(AppContext.BaseDirectory, "ConfigurationFiles", ConfigFileName);
            File.Copy(templatePath, Path.Combine(directory, ConfigFileName), true);
            creationHappened = true;
        }

        if (!_isMacrosFileReadable)
        {
            string templatePath = Path.Combine(AppContext.BaseDirectory, "ConfigurationFiles", MacroFileName);
            File.Copy(templatePath, Path.Combine(directory, MacroFileName), true);
            creationHappened = true;
        }
    }
}