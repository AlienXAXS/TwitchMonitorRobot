#nullable enable
using System;
using Newtonsoft.Json;

namespace TMRAgent.Discord
{
    [Serializable]
    public class Configuration
    {
        public string? WebHookUrl { get; set; }
    }

    internal class ConfigurationHandler
    {
        public static ConfigurationHandler Instance = _instance ??= new ConfigurationHandler();
        // ReSharper disable once InconsistentNaming
        private static readonly ConfigurationHandler? _instance;

        private string _configFileName = "discord.conf";

        public Configuration? Configuration = new();
        public bool IsEnabled = false;

        public ConfigurationHandler()
        {
            Load();
        }

        private void Load()
        {
            if (System.IO.File.Exists(_configFileName))
            {
                try
                {
                    var jsonData = System.IO.File.ReadAllText(_configFileName);
                    Configuration = JsonConvert.DeserializeObject<Configuration>(jsonData);
                    IsEnabled = Configuration?.WebHookUrl != null;
                }
                catch (Exception ex)
                {
                    Util.Log($"Fatal error while reading {_configFileName}: {ex.Message}\r\n\r\n{ex.StackTrace}", Util.LogLevel.Error, ConsoleColor.Red);
                    return;
                }
            }
            else
            {
                try
                {
                    System.IO.File.WriteAllText(_configFileName, JsonConvert.SerializeObject(Configuration));
                }
                catch (Exception ex)
                {
                    Util.Log($"Fatal error while writing {_configFileName}: {ex.Message}\r\n\r\n{ex.StackTrace}", Util.LogLevel.Error, ConsoleColor.Red);
                }
            }
        }
    }
}
