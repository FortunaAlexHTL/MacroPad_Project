using System.Text.Json;
using MacroPad_application.Models;

namespace MacroPad_application
{
    public class ConfigurationReader
    {
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
    }
}