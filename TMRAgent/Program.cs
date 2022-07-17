using System;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;

namespace TMRAgent
{
    internal class Program
    {
        public static ManualResetEvent QuitAppEvent = new ManualResetEvent(false);

        public static string Version = "0.1.1 Beta";

        static void Main(string[] args)
        {
            new Program().MainMethod();
        }

        public void MainMethod()
        {
            QuitAppEvent.Reset();
            
            DataConnection.DefaultSettings = new MySQL.DBConnection.MySettings();

            AppDomain.CurrentDomain.ProcessExit += (sender, args) => HandleApplicationExitEvent();
            Console.CancelKeyPress += (sender, args) => HandleApplicationExitEvent();

            ConsoleUtil.WriteToConsole($"Twitch Management Robot v{Version} Starting...", ConsoleUtil.LogLevel.Info);

            CheckConfigurationValidity();

            try
            {
                CheckOAuthTokens();
            }
            catch (Exception exception)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {exception.Message}", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
                ShutdownApp();
            }

            return;
            ConnectTwitchChat();

            SetupMySqlBackend();

            StartMonitoringTwitch();

            ConsoleUtil.WriteToConsole("Application is ready!", ConsoleUtil.LogLevel.Info);

            QuitAppEvent.WaitOne();

            ShutdownApp();
        }

        private void ShutdownApp()
        {
            Twitch.TwitchHandler.Instance.Dispose();
            Twitch.TwitchLiveMonitor.Instance.Dispose();
        }

        private void StartMonitoringTwitch()
        {
            ConsoleUtil.WriteToConsole("Starting Twitch Monitor", ConsoleUtil.LogLevel.Info);
            try
            {
                Twitch.TwitchLiveMonitor.Instance.Start();
                ConsoleUtil.WriteToConsole(" -> Success", ConsoleUtil.LogLevel.Info);
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {ex.Message}", ConsoleUtil.LogLevel.Error);
            }
        }

        private void SetupMySqlBackend()
        {
            ConsoleUtil.WriteToConsole("Connecting to MySQL Database Backend...", ConsoleUtil.LogLevel.Info);
            MySQL.MySqlHandler.Instance.Connect();
            ConsoleUtil.WriteToConsole(" -> Success", ConsoleUtil.LogLevel.Info);
        }

        private void CheckConfigurationValidity()
        {
            if (!Twitch.ConfigurationHandler.Instance.IsConfigurationGood())
            {
                ConsoleUtil.WriteToConsole("Twitch Configuration (twitch.conf) is invalid, needs username and auth token!", ConsoleUtil.LogLevel.Error);
                ConsoleUtil.WriteToConsole("Press ENTER to exit", ConsoleUtil.LogLevel.Error);
                Console.ReadLine();
                return;
            }

            if (!MySQL.ConfigurationHandler.Instance.IsConfigurationGood())
            {
                ConsoleUtil.WriteToConsole("MySQL Configuration (db.conf) is invalid, needs connection string!", ConsoleUtil.LogLevel.Error);
                ConsoleUtil.WriteToConsole("Press ENTER to exit", ConsoleUtil.LogLevel.Error);
                Console.ReadLine();
                return;
            }
        }

        private void CheckOAuthTokens()
        {
            ConsoleUtil.WriteToConsole("[OAuthChecker] Checking OAuth Tokens Validity", ConsoleUtil.LogLevel.Info);
            var twitchChatOAuth = Twitch.TwitchHandler.Instance.Auth.TestAuth(Twitch.Auth.AuthType.TwitchChat);
            var pubSubOAuth = Twitch.TwitchHandler.Instance.Auth.TestAuth(Twitch.Auth.AuthType.PubSub);

            if (!twitchChatOAuth)
            {
                ConsoleUtil.WriteToConsole("[OAuthChecker] Failed to validate TwitchChat OAuth Token, Attempting refresh.", 
                    ConsoleUtil.LogLevel.Warn, 
                    ConsoleColor.Yellow);

                if (!Twitch.TwitchHandler.Instance.Auth.RefreshToken(Twitch.Auth.AuthType.TwitchChat)
                        .GetAwaiter().GetResult())
                {
                    throw new Exception("Unable to refresh Auth Token for TwitchChat, Application cannot continue!");
                }
            }
            else
            {
                ConsoleUtil.WriteToConsole($"[OAuthChecker] Successfully validated TwitchChat OAuth Tokens, Expiry: {Twitch.ConfigurationHandler.Instance.Configuration.TwitchChat.TokenExpiry}/UTC", ConsoleUtil.LogLevel.Info);
            }

            if (!pubSubOAuth)
            {
                ConsoleUtil.WriteToConsole("[OAuthChecker] Failed to validate TwitchPubSub OAuth Token, Attempting refresh.", ConsoleUtil.LogLevel.Warn, ConsoleColor.Yellow);
                if (!Twitch.TwitchHandler.Instance.Auth.RefreshToken(Twitch.Auth.AuthType.PubSub)
                    .GetAwaiter().GetResult())
                {
                    throw new Exception("Unable to refresh Auth Token for PubSub, Application cannot continue!");
                }
            }
            else
            {
                ConsoleUtil.WriteToConsole($"[OAuthChecker] Successfully validated TwitchChat OAuth Tokens, Expiry: {Twitch.ConfigurationHandler.Instance.Configuration.TwitchChat.TokenExpiry}/UTC", ConsoleUtil.LogLevel.Info);
            }
        }

        private void ConnectTwitchChat()
        {
            ConsoleUtil.WriteToConsole("Connecting to Twitch", ConsoleUtil.LogLevel.Info);
            try
            {
                Twitch.TwitchHandler.Instance.Connect();
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"[TwitchChat] Error connecting: {ex.Message}", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
                return;
            }
            ConsoleUtil.WriteToConsole(" -> Success", ConsoleUtil.LogLevel.Info);
        }

        private void HandleApplicationExitEvent()
        {
            ConsoleUtil.WriteToConsole(" -> Application Exit Event Invoked... Shutting down!", ConsoleUtil.LogLevel.Info);
            Task.Delay(1000).GetAwaiter().GetResult();
            QuitAppEvent.Set();
        }
    }
}
