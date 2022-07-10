using System;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using System.Linq;

namespace TMRAgent
{
    internal class Program
    {
        private readonly ManualResetEvent _quitAppEvent = new ManualResetEvent(false);

        public static string Version = "0.1.0 Beta";

        static void Main(string[] args)
        {
            new Program().MainMethod();
        }

        public void MainMethod()
        {
            _quitAppEvent.Reset();

            DataConnection.DefaultSettings = new MySQL.DBConnection.MySettings();

            AppDomain.CurrentDomain.ProcessExit += (sender, args) => HandleApplicationExitEvent();
            Console.CancelKeyPress += (sender, args) => HandleApplicationExitEvent();

            ConsoleUtil.WriteToConsole($"Twitch Management Robot v{Version} Starting...", ConsoleUtil.LogLevel.INFO);

            if ( !Twitch.ConfigurationHandler.Instance.IsConfigurationGood() )
            {
                ConsoleUtil.WriteToConsole("Twitch Configuration (twitch.conf) is invalid, needs username and auth token!", ConsoleUtil.LogLevel.ERROR);
                ConsoleUtil.WriteToConsole("Press ENTER to exit", ConsoleUtil.LogLevel.ERROR);
                Console.ReadLine();
                return;
            }

            ConsoleUtil.WriteToConsole("Connecting to Twitch", ConsoleUtil.LogLevel.INFO);
            try
            {
                Twitch.TwitchHandler.Instance.Connect();
            }
            catch (Exception ex)
            {
                Console.ReadLine();
                return;
            }

            ConsoleUtil.WriteToConsole(" -> Success", ConsoleUtil.LogLevel.INFO);

            if (!MySQL.ConfigurationHandler.Instance.IsConfigurationGood())
            {
                ConsoleUtil.WriteToConsole("MySQL Configuration (db.conf) is invalid, needs connection string!", ConsoleUtil.LogLevel.ERROR);
                ConsoleUtil.WriteToConsole("Press ENTER to exit", ConsoleUtil.LogLevel.ERROR);
                Console.ReadLine();
                return;
            }

            ConsoleUtil.WriteToConsole("Connecting to MySQL Database Backend...", ConsoleUtil.LogLevel.INFO);
            MySQL.MySQLHandler.Instance.Connect();
            ConsoleUtil.WriteToConsole(" -> Success", ConsoleUtil.LogLevel.INFO);

            ConsoleUtil.WriteToConsole("Starting Twitch Monitor", ConsoleUtil.LogLevel.INFO);
            try
            {
                Twitch.TwitchLiveMonitor.Instance.Start();
                ConsoleUtil.WriteToConsole(" -> Success", ConsoleUtil.LogLevel.INFO);
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"Fatal Error: {ex.Message}", ConsoleUtil.LogLevel.ERROR);
            }

            ConsoleUtil.WriteToConsole("Application is ready!", ConsoleUtil.LogLevel.INFO);

            _quitAppEvent.WaitOne();

            Twitch.TwitchHandler.Instance.Dispose();
            Twitch.TwitchLiveMonitor.Instance.Dispose();
        }
        private void HandleApplicationExitEvent()
        {
            ConsoleUtil.WriteToConsole(" -> Application Exit Event Invoked... Shutting down!", ConsoleUtil.LogLevel.INFO);
            Task.Delay(1000).GetAwaiter().GetResult();
            _quitAppEvent.Set();
        }
    }
}
