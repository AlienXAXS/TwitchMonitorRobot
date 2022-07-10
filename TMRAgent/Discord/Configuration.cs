using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TMRAgent.Discord
{
    [Serializable]
    public class Configuration
    {
        public string? WebHookURL { get; set; }
    }

    internal class ConfigurationHandler
    {
        public static ConfigurationHandler Instance = _instance ??= new ConfigurationHandler();
        private static readonly ConfigurationHandler? _instance;

        private string _configFileName = "discord.conf";

        public Configuration Configuration = new Configuration();
        public bool IsEnabled = false;

        public ConfigurationHandler()
        {
            Load();
        }

        private void Load()
        {
            if (System.IO.File.Exists(_configFileName))
            {
                Configuration = JsonConvert.DeserializeObject<Configuration>(System.IO.File.ReadAllText(_configFileName));
                IsEnabled = Configuration.WebHookURL != null;
            }
            else
            {
                System.IO.File.WriteAllText(_configFileName, JsonConvert.SerializeObject(Configuration));
            }
        }
    }
}
