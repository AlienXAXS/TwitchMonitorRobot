using System;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;

namespace TMRAgent
{
    internal class Program
    {
        public static bool ExitRequested = false;

        public static string Version = "0.1.4 Beta";

        private readonly object _syncObject = new();

        private static readonly ManualResetEvent _manualResetEvent = new(false);

        static void Main(string[] args)
        {
            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                Task.Run(StopApp);
                _manualResetEvent.WaitOne();
            };

            AssemblyLoadContext.Default.Unloading += context =>
            {
                Task.Run(StopApp);
                _manualResetEvent.WaitOne();
            };

            Task.Run(StartApp);

            _manualResetEvent.WaitOne();
        }

        public static void InvokeApplicationExit()
        {
            ConsoleUtil.WriteToConsole(" -> Application Exit Event Invoked... Shutting down!", ConsoleUtil.LogLevel.Info);
            ExitRequested = true;
            Task.Run(StopApp);
            _manualResetEvent.WaitOne();
        }

        static void StopApp()
        {
            if (ExitRequested) return;

            ExitRequested = true;
            Twitch.TwitchHandler.Instance.Dispose();
            _manualResetEvent.Set();
            Environment.Exit(0);
        }

        static async Task StartApp()
        {
            DataConnection.DefaultSettings = new MySQL.DBConnection.MySettings();

            ConsoleUtil.WriteToConsole($"Twitch Management Robot v{Version} Starting...", ConsoleUtil.LogLevel.Info);

            await Task.Run(() =>
            {
                new Program().CheckConfigurationValidity();
                new Program().ConnectTwitchChat();
                new Program().SetupMySqlBackend();
                new Program().StartMonitoringTwitch();
            });

            ConsoleUtil.WriteToConsole("Application is ready!", ConsoleUtil.LogLevel.Info);
        }

        private void StartMonitoringTwitch()
        {
            ConsoleUtil.WriteToConsole("Starting Twitch Monitor", ConsoleUtil.LogLevel.Info);
            try
            {
                Twitch.TwitchHandler.Instance.PubSubService.Start();
                ConsoleUtil.WriteToConsole(" -> Success", ConsoleUtil.LogLevel.Info);
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {ex.Message}", ConsoleUtil.LogLevel.Error);
            }

            ConsoleUtil.WriteToConsole("Checking for an existing stream", ConsoleUtil.LogLevel.Info);
            Twitch.TwitchHandler.Instance.CheckForExistingStream();
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
                Twitch.TwitchHandler.Instance.ChatService.Connect();
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"[TwitchChat] Error connecting: {ex.Message}", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
                return;
            }
            ConsoleUtil.WriteToConsole(" -> Success", ConsoleUtil.LogLevel.Info);
        }

        
    }
}
