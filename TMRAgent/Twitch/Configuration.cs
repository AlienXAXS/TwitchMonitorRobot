#nullable enable
using System;
using Newtonsoft.Json;

namespace TMRAgent.Twitch
{

    [Serializable]
    public class Configuration
    {
        public string? AppClientId;
        public string? TwitchCallbackUrl;
        public string? ClientSecret;

        public TwitchChatCls TwitchChat = new TwitchChatCls();
        public PubSubCls PubSub = new PubSubCls();

        [Serializable]
        public class TwitchChatCls
        {
            public string? Username;
            public string? AuthToken;
            public string? ChannelName;
            public string? RefreshToken;
            public DateTime? TokenExpiry;
        }

        [Serializable]
        public class PubSubCls
        {
            public string? AuthToken;
            public string? ChannelId;
            public string? RefreshToken;
            public DateTime? TokenExpiry;
        }
    }

    internal class ConfigurationHandler
    {
        public static ConfigurationHandler Instance = _instance ??= new ConfigurationHandler();
        // ReSharper disable once InconsistentNaming
        private static readonly ConfigurationHandler? _instance;

        private string _configFileName = "twitch.conf";

        public Configuration Configuration = new Configuration();

        public ConfigurationHandler()
        {
            try
            {
                #if DEBUG
                //_configFileName = "twitch_debug.conf";
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
            return !(string.IsNullOrEmpty(Configuration.TwitchChat.AuthToken) || string.IsNullOrEmpty(Configuration.TwitchChat.Username) || string.IsNullOrEmpty(Configuration.TwitchChat.ChannelName));
        }

        private void Load()
        {
            try
            {
                if (System.IO.File.Exists(_configFileName))
                {
                    Configuration =
                        JsonConvert.DeserializeObject<Configuration>(System.IO.File.ReadAllText(_configFileName)) ?? new Configuration();
                    ConsoleUtil.WriteToConsole($"Configuration file {_configFileName} loaded", ConsoleUtil.LogLevel.Info);
                }
                else
                {
                    Configuration.TwitchCallbackUrl = "http://localhost:9953/callback";
                    Save();
                }
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {ex.Message}\r\n\r\n{ex.StackTrace}", ConsoleUtil.LogLevel.Fatal, ConsoleColor.Red);
            }
        }

        public void Save()
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
