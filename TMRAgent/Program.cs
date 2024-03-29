﻿using System;
using System.Net.Mime;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;
using TMRAgent.Twitch;
using TwitchLib.Client.Models;
using TwitchLib.Client;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

using TMRAgent.Twitch.Chat;
using TMRAgent.Twitch.Utility;

namespace TMRAgent
{
    internal class Program
    {
        public static bool ExitRequested = false;

        public static string Version = "3.0";

        private readonly object _syncObject = new();

        private static readonly ManualResetEvent _manualResetEvent = new(false);

        static void Main(string[] args)
        {
            var luaScriptDiscovery = new LuaEngine.LuaHandler();
            //luaScriptDiscovery.DiscoverAndRunScripts();

            // Validate OAuth Token
            try
            {
                TwitchHandler.Instance.Auth.Validate(Auth.AuthType.PubSub, true);
            }
            catch (Exception ex)
            {
                Util.Log(
                    $"[TwitchChat] Unable to connect to TwitchChat.  OAuth Validation failed. Error: {ex.Message}",
                    Util.LogLevel.Error, ConsoleColor.Red);
            }

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                ReconnectionPolicy = null // Disable reconnection
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
            {
                Task.Run(StopApp);
                _manualResetEvent.WaitOne();
            };

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
            Util.Log(" -> Application Exit Event Invoked... Shutting down!", Util.LogLevel.Info);
            Task.Run(StopApp);
            _manualResetEvent.WaitOne();
            ExitRequested = true;
        }

        static void StopApp()
        {
            if (ExitRequested) return;
            Util.Log($"Application exit requested, starting shutdown sequence", Util.LogLevel.Info);

            ExitRequested = true;
            Twitch.TwitchHandler.Instance.Dispose();
            _manualResetEvent.Set();
            Environment.Exit(0);
        }

        static async Task StartApp()
        {
            DataConnection.DefaultSettings = new MySQL.DBConnection.MySettings();

            Util.Log($"Twitch Management Robot v{Version} Starting...", Util.LogLevel.Info);

            await Task.Run(() =>
            {
                new Program().CheckConfigurationValidity();
                new Program().ConnectTwitchChat();
                new Program().SetupMySqlBackend();
                new Program().StartMonitoringTwitch();
            });

            Util.Log("Application is ready!", Util.LogLevel.Info);
        }

        private void StartMonitoringTwitch()
        {
            Util.Log("Starting Twitch Monitor", Util.LogLevel.Info);
            try
            {
                Twitch.TwitchHandler.Instance.PubSubService.Start();
                Util.Log(" -> Success", Util.LogLevel.Info);
            }
            catch (Exception ex)
            {
                Util.Log($"Fatal Error: {ex.Message}", Util.LogLevel.Error);
            }
        }

        private void SetupMySqlBackend()
        {
            Util.Log("Connecting to MySQL Database Backend...", Util.LogLevel.Info);
            MySQL.MySqlHandler.Instance.Connect();
            Util.Log(" -> Success", Util.LogLevel.Info);
        }

        private void CheckConfigurationValidity()
        {
            if (!Twitch.ConfigurationHandler.Instance.IsConfigurationGood())
            {
                Util.Log("Twitch Configuration (twitch.conf) is invalid, needs username and auth token!", Util.LogLevel.Error);
                Util.Log("Press ENTER to exit", Util.LogLevel.Error);
                Console.ReadLine();
                return;
            }

            if (!MySQL.ConfigurationHandler.Instance.IsConfigurationGood())
            {
                Util.Log("MySQL Configuration (db.conf) is invalid, needs connection string!", Util.LogLevel.Error);
                Util.Log("Press ENTER to exit", Util.LogLevel.Error);
                Console.ReadLine();
                return;
            }
        }

        private void ConnectTwitchChat()
        {
            Util.Log("Connecting to Twitch", Util.LogLevel.Info);
            try
            {
                Twitch.TwitchHandler.Instance.ChatService.Connect();
            }
            catch (Exception ex)
            {
                Util.Log($"[TwitchChat] Error connecting: {ex.Message}", Util.LogLevel.Error, ConsoleColor.Red);
                return;
            }
            Util.Log(" -> Success", Util.LogLevel.Info);
        }

        
    }
}
