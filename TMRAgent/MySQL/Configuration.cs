using System;
using Newtonsoft.Json;

namespace TMRAgent.MySQL
{

    [Serializable]
    public class Configuration
    {
        public string? ConnectionString;
    }

    internal class ConfigurationHandler
    {
        public static ConfigurationHandler Instance = _instance ??= new ConfigurationHandler();
        // ReSharper disable once InconsistentNaming
        private static readonly ConfigurationHandler? _instance;

        private string _configFileName = "db.conf";

        public Configuration Configuration = new();

        public ConfigurationHandler()
        {
            try
            {
                #if DEBUG
                //_configFileName = "db_debug.conf";
                #endif

                Load();
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {ex.Message}\r\n\r\n{ex.StackTrace}", ConsoleUtil.LogLevel.Fatal, ConsoleColor.Red);
            }
        }

        public bool IsConfigurationGood()
        {

            return !string.IsNullOrEmpty(Configuration.ConnectionString);

        }

        private void Load()
        {
            try
            {
                if (System.IO.File.Exists(_configFileName))
                {
                    Configuration =
                        JsonConvert.DeserializeObject<Configuration>(System.IO.File.ReadAllText(_configFileName));
                    ConsoleUtil.WriteToConsole($"Configuration file {_configFileName} loaded", ConsoleUtil.LogLevel.Info);
                }
                else
                {
                    Save();
                }
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {ex.Message}\r\n\r\n{ex.StackTrace}", ConsoleUtil.LogLevel.Fatal, ConsoleColor.Red);
            }
        }

        private void Save()
        {
            try
            {
                System.IO.File.WriteAllText(_configFileName, JsonConvert.SerializeObject(Configuration, Formatting.Indented));
                ConsoleUtil.WriteToConsole($"Configuration file {_configFileName} saved", ConsoleUtil.LogLevel.Info);
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {ex.Message}\r\n\r\n{ex.StackTrace}", ConsoleUtil.LogLevel.Fatal, ConsoleColor.Red);
            }
        }

    }
}
