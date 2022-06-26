﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using LinqToDB;
using LinqToDB.Common;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
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

        private TwitchClient client;

        private MySQL.Commands.AddModeratorCommand _addModeratorCommand = new MySQL.Commands.AddModeratorCommand();
        private MySQL.Commands.RemoveModeratorCommand _removeModeratorCommand = new MySQL.Commands.RemoveModeratorCommand();
        private MySQL.Commands.TopCommand _topCommand = new MySQL.Commands.TopCommand();

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
            client.OnRitualNewChatter += Client_OnRitualNewChatter;
            client.OnGiftedSubscription += Client_OnGiftedSubscription;

            client.Connect();
        }

        private void Client_OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
        {
            ConsoleUtil.WriteToConsole($"[GiftSubEvent] {e.GiftedSubscription.DisplayName} gifted {e.GiftedSubscription.MsgParamRecipientUserName} a sub!", ConsoleUtil.LogLevel.INFO, ConsoleColor.Cyan);
        }

        private void Client_OnRitualNewChatter(object sender, OnRitualNewChatterArgs e)
        {
            ConsoleUtil.WriteToConsole($"Possible First Time Chatter: {e.RitualNewChatter.DisplayName}", ConsoleUtil.LogLevel.INFO, ConsoleColor.Cyan);
        }

        public TwitchClient GetTwitchClient()
        {
            return client;
        }

        private void ClientOnOnModeratorsReceived(object? sender, OnModeratorsReceivedArgs e)
        {
            
        }

        private void ClientOnOnReSubscriber(object? sender, OnReSubscriberArgs e)
        {
            ConsoleUtil.WriteToConsole($"[SubDetector] User {e.ReSubscriber.DisplayName} Resubbed!", ConsoleUtil.LogLevel.INFO, ConsoleColor.Cyan);
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

            if ( e.ChatMessage.Message.StartsWith("!!") )
            {
                ProcessChatCommandMessage(e.ChatMessage);
                return;
            }
            if (e.ChatMessage.Message.StartsWith("!"))
            {
                ConsoleUtil.WriteToConsole(
                    $"Processing Command Message from {e.ChatMessage.Username} [{e.ChatMessage.Message}]",
                    ConsoleUtil.LogLevel.INFO);

                MySQL.MySQLHandler.Instance.Commands.ProcessCommandMessage(e.ChatMessage.Username, int.Parse(e.ChatMessage.UserId), e.ChatMessage.IsModerator,
                    e.ChatMessage.Message);
            }
            else
            {

                ConsoleUtil.WriteToConsole(
                    $"Processing Chat Message from {e.ChatMessage.Username} [{e.ChatMessage.Message}]",
                    ConsoleUtil.LogLevel.INFO);

                MySQL.MySQLHandler.Instance.Messages.ProcessChatMessage(e.ChatMessage.Username, int.Parse(e.ChatMessage.UserId), e.ChatMessage.IsModerator,
                    e.ChatMessage.Message);

                if (e.ChatMessage.Bits > 0)
                {
                    ConsoleUtil.WriteToConsole($"User {e.ChatMessage.Username} sent {e.ChatMessage.Bits} bits with their message", ConsoleUtil.LogLevel.INFO, ConsoleColor.Green);
                }
            }
        }

        private void ProcessChatCommandMessage(ChatMessage chatMessage)
        {
            var parameters = chatMessage.Message.Split(' ', 2);

            switch (parameters[0].ToLower())
            {
                case "!!add_mod_action":
                    if (!IsUserModeratorOrBroadcaster(chatMessage)) return;
                    _addModeratorCommand.Handle(chatMessage, parameters);
                    break;

                case "!!remove_mod_action":
                    if (!IsUserModeratorOrBroadcaster(chatMessage)) return;
                    _removeModeratorCommand.Handle(chatMessage, parameters);
                    break;

                case "!!walls":
                    using (var db = new MySQL.DBConnection.Database())
                    {
                        var walls = db.Messages.Where(x => x.Message.ToLower().Contains("wall") && !x.Message.StartsWith("Stats")).Count();
                        client.SendMessage(chatMessage.Channel, $"Stats: At least {walls} messages have been sent describing how much @Mind1 shoots walls - GO MIND!");
                    }
                    break;

                case "!!dead":
                    using ( var db = new MySQL.DBConnection.Database())
                    {
                        var deadCmds = db.Commands.Where(x => x.Command.ToLower().Equals("!dead"));
                        client.SendMessage(chatMessage.Channel, $"Stats: Mind1 has died at least {deadCmds.Count()} times - Ouch! (num of !dead used)");
                    }
                    break;

                case "!!about":
                    using ( var db = new MySQL.DBConnection.Database() )
                    {
                        var TotalUsers = db.Users.Count();
                        var TotalMessages = db.Messages.Count();
                        client.SendMessage(chatMessage.Channel, $"I am Twitch Monitor Robot v{Program.Version} - I am logging everything that happens here.  Currently watching {TotalUsers:n0} users having sent {TotalMessages:n0} chat messages! - Created by AlienX");
                    }
                    break;

                case "!!top":
                    _topCommand.Handle(chatMessage, parameters);
                    break;

                case "!!help":
                    client.SendMessage(chatMessage.Channel, $"Commands are: !!top [redeem], !!about, !!help (more added soon!)");
                    break;
            }
        }

        private bool IsUserModeratorOrBroadcaster(ChatMessage chatMessage)
        {
            if (chatMessage.IsModerator || chatMessage.IsBroadcaster)
            {
                return true;
            } else
            {
                client.SendMessage(chatMessage.Channel, $"Sorry {chatMessage.Username}, you do not have access to that command");
                return false;
            }
        }

        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            var rnd = new Random(DateTime.Now.Millisecond);

            if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
            {
                ConsoleUtil.WriteToConsole($"[SubDetector] User {e.Subscriber.DisplayName} New Prime Sub!", ConsoleUtil.LogLevel.INFO, ConsoleColor.Cyan);

                if (rnd.Next(1, 2) == 1)
                {
                    client.SendMessage(e.Channel, $"@{e.Subscriber.DisplayName} Thanks for the prime sub!!!");
                }
                else
                {
                    client.SendMessage(e.Channel, $"!handsup @{e.Subscriber.DisplayName} - Thanks for the prime sub!!!");
                }

            }
            else
            {
                ConsoleUtil.WriteToConsole($"[SubDetector] User {e.Subscriber.DisplayName} New Sub!", ConsoleUtil.LogLevel.INFO, ConsoleColor.Cyan);
                if (rnd.Next(1, 2) == 1)
                {
                    client.SendMessage(e.Channel, $"@{e.Subscriber.DisplayName} Thanks for the sub!!!");
                } else
                {
                    client.SendMessage(e.Channel, $"!handsup @{e.Subscriber.DisplayName} - Thanks for the sub!!!");
                }
            }
                
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
