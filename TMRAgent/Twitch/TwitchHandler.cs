using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LinqToDB;
using LinqToDB.Common;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace TMRAgent.Twitch
{
    public class TwitchHandler
    {
        public static TwitchHandler Instance = _instance ??= new TwitchHandler();
        private static readonly TwitchHandler? _instance;

        TwitchClient client;

        public void Connect()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(ConfigurationHandler.Instance.Configuration.Username , ConfigurationHandler.Instance.Configuration.AuthToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            client.Initialize(credentials, ConfigurationHandler.Instance.Configuration.ChannelName);

            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnWhisperReceived += Client_OnWhisperReceived;
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnConnected += Client_OnConnected;
            client.OnChannelStateChanged += ClientOnOnChannelStateChanged;
            client.OnReSubscriber += ClientOnOnReSubscriber;
            client.OnModeratorsReceived += ClientOnOnModeratorsReceived;
 

            client.Connect();
        }

        private void ClientOnOnModeratorsReceived(object? sender, OnModeratorsReceivedArgs e)
        {
            
        }

        private void ClientOnOnReSubscriber(object? sender, OnReSubscriberArgs e)
        {
            
        }

        private void ClientOnOnChannelStateChanged(object? sender, OnChannelStateChangedArgs e)
        {
            
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            //Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            ConsoleUtil.WriteToConsole($"[Twitch Bot] Connected to Twitch IRC", ConsoleUtil.LogLevel.INFO);
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            ConsoleUtil.WriteToConsole($"[Twitch Bot] Joined channel {e.Channel}", ConsoleUtil.LogLevel.INFO);
            
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {

            if (e.ChatMessage.Message.ToLower().Equals("!tmr"))
            {
                client.SendMessage(e.ChatMessage.Channel, $"I am Twitch Message Robot.  Database is {MySQL.MySQLHandler.Instance.DatabaseVersion}, I am running on {Environment.OSVersion}");
                return;
            }

            if (e.ChatMessage.Message.StartsWith("!"))
            {
                ConsoleUtil.WriteToConsole(
                    $"Processing Command Message from {e.ChatMessage.Username} [{e.ChatMessage.Message}]",
                    ConsoleUtil.LogLevel.INFO);

                MySQL.MySQLHandler.Instance.ProcessCommandMessage(e.ChatMessage.Username, e.ChatMessage.IsModerator,
                    e.ChatMessage.Message);
            }
            else
            {
                ConsoleUtil.WriteToConsole(
                    $"Processing Chat Message from {e.ChatMessage.Username} [{e.ChatMessage.Message}]",
                    ConsoleUtil.LogLevel.INFO);

                MySQL.MySQLHandler.Instance.ProcessChatMessage(e.ChatMessage.Username, e.ChatMessage.IsModerator,
                    e.ChatMessage.Message);
            }
        }

        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {

        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {

        }

        public void Dispose()
        {
            ConsoleUtil.WriteToConsole("Disposing Twitch IRC Chat Classes...", ConsoleUtil.LogLevel.INFO);

            foreach (var channel in client.JoinedChannels)
            {
                ConsoleUtil.WriteToConsole($"Attemtping to leave channel {channel.Channel}", ConsoleUtil.LogLevel.INFO);
                client.LeaveChannel(channel.Channel);
            }
            
            ConsoleUtil.WriteToConsole("Disconnecting from Twitch IRC", ConsoleUtil.LogLevel.INFO);
            client.Disconnect();
            ConsoleUtil.WriteToConsole("Disconnecting from Twitch IRC - Done", ConsoleUtil.LogLevel.INFO);

            var maxTries = 10;
            var currentTry = 1;
            while (client.IsConnected)
            {
                ConsoleUtil.WriteToConsole($"Attempting to disconnect from Twitch Chat IRC [{currentTry}/{maxTries}]", ConsoleUtil.LogLevel.INFO);
                Thread.Sleep(1000);
                ++currentTry;

                if (currentTry > maxTries)
                {
                    return;
                }
            }
        }
    }
}
