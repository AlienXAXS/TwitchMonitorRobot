using System;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;

namespace TMRAgent
{
    internal class Program
    {
        public static ManualResetEvent QuitAppEvent = new ManualResetEvent(false);
        public static bool ExitRequested = false;

        public static string Version = "0.1.3 Beta";

        static void Main(string[] args)
        {
            new Program().MainMethod();
        }

        public void MainMethod()
        {
            DataConnection.DefaultSettings = new MySQL.DBConnection.MySettings();
            AppDomain.CurrentDomain.ProcessExit += (sender, args) => HandleApplicationExitEvent();
            Console.CancelKeyPress += (sender, args) => HandleApplicationExitEvent();
            QuitAppEvent.Reset();

            ConsoleUtil.WriteToConsole($"Twitch Management Robot v{Version} Starting...", ConsoleUtil.LogLevel.Info);

            CheckConfigurationValidity();

            try
            {
                Twitch.TwitchHandler.Instance.Auth.Validate();
            }
            catch (Exception exception)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {exception.Message}", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
                ShutdownApp();
                return;
            }

            ConnectTwitchChat();

            SetupMySqlBackend();

            StartMonitoringTwitch();

            ConsoleUtil.WriteToConsole("Application is ready!", ConsoleUtil.LogLevel.Info);

            QuitAppEvent.WaitOne();

            ShutdownApp();
        }

        private void ShutdownApp()
        {
            ExitRequested = true;
            Twitch.TwitchHandler.Instance.Dispose();
        }

        private void StartMonitoringTwitch()
        {
            ConsoleUtil.WriteToConsole("Starting Twitch Monitor", ConsoleUtil.LogLevel.Info);
            try
            {
                Twitch.TwitchHandler.Instance.LivestreamMonitorService.Start();
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

        

        private void ConnectTwitchChat()
        {
            ConsoleUtil.WriteToConsole("Connecting to Twitch", ConsoleUtil.LogLevel.Info);
            try
            {
                Twitch.TwitchHandler.Instance.ChatHandler.Connect();
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
