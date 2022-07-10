using System;
using System.Linq;
using System.Threading;
using LinqToDB;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
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
        private MySQL.Commands.UserStatsCommand _userStatsCommand = new MySQL.Commands.UserStatsCommand();

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

            // Log
            client.OnLog += Client_OnLog;

            //Connectivity & Channel
            client.OnDisconnected += Client_OnDisconnected;
            client.OnConnected += Client_OnConnected;
            client.OnJoinedChannel += Client_OnJoinedChannel;

            // Stream State
            client.OnChannelStateChanged += ClientOnOnChannelStateChanged;

            // Messages
            client.OnMessageReceived += Client_OnMessageReceived;
#if !DEBUG
            // Subscriptions
            client.OnNewSubscriber += Client_OnNewSubscriber;
            client.OnReSubscriber += ClientOnOnReSubscriber;
            client.OnGiftedSubscription += Client_OnGiftedSubscription;

            // Tests
            client.OnRitualNewChatter += Client_OnRitualNewChatter;
#endif

            client.Connect();
        }

        private void Client_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            ConsoleUtil.WriteToConsole($"[TwitchClient] Disconnection! {e}", ConsoleUtil.LogLevel.ERROR, ConsoleColor.Red);
        }

        private void Client_OnRitualNewChatter(object sender, OnRitualNewChatterArgs e)
        {
            ConsoleUtil.WriteToConsole($"Possible First Time Chatter: {e.RitualNewChatter.DisplayName}", ConsoleUtil.LogLevel.INFO, ConsoleColor.Cyan);
        }

        public TwitchClient GetTwitchClient()
        {
            return client;
        }

        public void CheckForStreamUpdate()
        {
            if (TwitchLiveMonitor.Instance.CurrentLiveStreamId != -1)
            {
                if (TwitchLiveMonitor.Instance.LastUpdateTime < DateTime.Now.ToUniversalTime().AddMinutes(-2))
                {
                    MySQL.MySQLHandler.Instance.Streams.ProcessStreamUpdate();
                    TwitchLiveMonitor.Instance.LastUpdateTime = DateTime.Now.ToUniversalTime();
                }
            }
        }

        private void ClientOnOnChannelStateChanged(object? sender, OnChannelStateChangedArgs e)
        {
            CheckForStreamUpdate();
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            CheckForStreamUpdate();
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
            CheckForStreamUpdate();

            if ( e.ChatMessage.Message.StartsWith("!!") )
            {
                ProcessChatCommandMessage(e.ChatMessage);
                return;
            }
            if (e.ChatMessage.Message.StartsWith("!"))
            {
                MySQL.MySQLHandler.Instance.Commands.ProcessCommandMessage(e.ChatMessage.Username, int.Parse(e.ChatMessage.UserId), e.ChatMessage.IsModerator,
                    e.ChatMessage.Message);
            }
            else
            {
                MySQL.MySQLHandler.Instance.Messages.ProcessChatMessage(e.ChatMessage.Username, int.Parse(e.ChatMessage.UserId), e.ChatMessage.IsModerator,
                    e.ChatMessage.Message, e.ChatMessage.Bits);
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

                case "!!stats":
                    _userStatsCommand.Handle(chatMessage, parameters);
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

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            MySQL.MySQLHandler.Instance.Subscriptions.ProcessSubscription(e.Subscriber.DisplayName, e.Subscriber.UserId, false, e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime);
        }

        private void ClientOnOnReSubscriber(object? sender, OnReSubscriberArgs e)
        {
            MySQL.MySQLHandler.Instance.Subscriptions.ProcessSubscription(e.ReSubscriber.DisplayName, e.ReSubscriber.UserId, true, e.ReSubscriber.SubscriptionPlan == SubscriptionPlan.Prime);
        }

        private void Client_OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
        {
            MySQL.MySQLHandler.Instance.Subscriptions.ProcessSubscription(e.GiftedSubscription.MsgParamRecipientDisplayName, e.GiftedSubscription.MsgParamRecipientId, false, false, true, e.GiftedSubscription.DisplayName, e.GiftedSubscription.UserId);
        }

        public void ProcessStreamOnline()
        {
            if (!client.IsConnected) return;
            client.SendMessage(ConfigurationHandler.Instance.Configuration.ChannelName, $"[BOT] Stream Started - Monitoring Chat and Bit Events.");
        }

        public void ProcessStreamOffline()
        {
            if (!client.IsConnected) return;
            client.SendMessage(ConfigurationHandler.Instance.Configuration.ChannelName, $"[BOT] Stream Ended - Disabling Chat and Bit Event Hooks.");
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
