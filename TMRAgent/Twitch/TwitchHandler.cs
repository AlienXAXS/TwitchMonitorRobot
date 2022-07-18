#nullable enable
using System;
using System.Linq;
using System.Threading;
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

        private TwitchClient? _client;

        private MySQL.Commands.AddModeratorCommand _addModeratorCommand = new MySQL.Commands.AddModeratorCommand();
        private MySQL.Commands.RemoveModeratorCommand _removeModeratorCommand = new MySQL.Commands.RemoveModeratorCommand();
        private MySQL.Commands.TopCommand _topCommand = new MySQL.Commands.TopCommand();
        private MySQL.Commands.UserStatsCommand _userStatsCommand = new MySQL.Commands.UserStatsCommand();

        public Auth Auth = new Auth();

        public void Connect()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(ConfigurationHandler.Instance.Configuration.TwitchChat.Username , ConfigurationHandler.Instance.Configuration.TwitchChat.AuthToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(customClient);
            _client.Initialize(credentials, ConfigurationHandler.Instance.Configuration.TwitchChat.ChannelName);

            // Log
            _client.OnLog += Client_OnLog!;
            _client.OnConnectionError += ClientOnOnConnectionError;
            _client.OnFailureToReceiveJoinConfirmation += ClientOnOnFailureToReceiveJoinConfirmation;
            _client.OnNoPermissionError += ClientOnOnNoPermissionError;

            // Auth Issues
            _client.OnIncorrectLogin += ClientOnOnIncorrectLogin;

            //Connectivity & Channel
            _client.OnDisconnected += Client_OnDisconnected;
            _client.OnConnected += Client_OnConnected;
            _client.OnJoinedChannel += Client_OnJoinedChannel;

            // Stream State
            _client.OnChannelStateChanged += ClientOnOnChannelStateChanged;

            // Messages
            _client.OnMessageReceived += Client_OnMessageReceived;

            // Subscriptions
            _client.OnNewSubscriber += Client_OnNewSubscriber;
            _client.OnReSubscriber += ClientOnOnReSubscriber;
            _client.OnGiftedSubscription += Client_OnGiftedSubscription;

            // Tests
            _client.OnRitualNewChatter += Client_OnRitualNewChatter;

            _client.Connect();
        }

        private void ClientOnOnIncorrectLogin(object? sender, OnIncorrectLoginArgs e)
        {
            if (_client == null) return;

            ConsoleUtil.WriteToConsole($"[TwitchChat] Invalid/Expired Auth Credentials", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
            _client.Disconnect();

            if (!Auth.TestAuth(Auth.AuthType.TwitchChat))
            {
                ConsoleUtil.WriteToConsole("[TwitchChat] Attemtping a token refresh", ConsoleUtil.LogLevel.Info);
                if (Auth.RefreshToken(Auth.AuthType.TwitchChat).GetAwaiter().GetResult())
                {
                    ConsoleUtil.WriteToConsole($"[TwitchChat] Token refresh successful, reconnecting to TwitchChat", ConsoleUtil.LogLevel.Info);
                    Connect();
                }
                else
                {
                    ConsoleUtil.WriteToConsole("[TwitchChat] Unable to refresh TwitchChat Token, Application will now exit.", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
                    Program.QuitAppEvent.Set();
                }
            }
        }

        private void ClientOnOnFailureToReceiveJoinConfirmation(object? sender, OnFailureToReceiveJoinConfirmationArgs e)
        {
            ConsoleUtil.WriteToConsole($"[TwitchClient] Failed to join channel {e.Exception.Channel}", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
        }

        private void ClientOnOnConnectionError(object? sender, OnConnectionErrorArgs e)
        {
            
        }

        private void ClientOnOnNoPermissionError(object? sender, EventArgs e)
        {
            
        }

        private void Client_OnDisconnected(object? sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            ConsoleUtil.WriteToConsole($"[TwitchClient] Disconnection! {e}", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
        }

        private void Client_OnRitualNewChatter(object? sender, OnRitualNewChatterArgs e)
        {
            ConsoleUtil.WriteToConsole($"Possible First Time Chatter: {e.RitualNewChatter.DisplayName}", ConsoleUtil.LogLevel.Info, ConsoleColor.Cyan);
        }

        public TwitchClient? GetTwitchClient()
        {
            return _client;
        }

        public void CheckForStreamUpdate()
        {
            if (TwitchLiveMonitor.Instance.CurrentLiveStreamId != -1)
            {
                if (TwitchLiveMonitor.Instance.LastUpdateTime < DateTime.Now.ToUniversalTime().AddMinutes(-2))
                {
                    MySQL.MySqlHandler.Instance.Streams.ProcessStreamUpdate();
                    TwitchLiveMonitor.Instance.LastUpdateTime = DateTime.Now.ToUniversalTime();
                }
            }
        }

        private void ClientOnOnChannelStateChanged(object? sender, OnChannelStateChangedArgs e)
        {
            CheckForStreamUpdate();
        }

        private void Client_OnLog(object? sender, OnLogArgs e)
        {
            CheckForStreamUpdate();
        }

        private void Client_OnConnected(object? sender, OnConnectedArgs e)
        {
            ConsoleUtil.WriteToConsole($"[Twitch Bot] Connected to Twitch IRC", ConsoleUtil.LogLevel.Info);
        }

        private void Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
        {
            ConsoleUtil.WriteToConsole($"[Twitch Bot] Joined channel {e.Channel}", ConsoleUtil.LogLevel.Info);
            
        }

        private void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            CheckForStreamUpdate();

            if ( e.ChatMessage.Message.StartsWith("!!") )
            {
                ProcessChatCommandMessage(e.ChatMessage);
                return;
            }
            if (e.ChatMessage.Message.StartsWith("!"))
            {
                MySQL.MySqlHandler.Instance.Commands.ProcessCommandMessage(e.ChatMessage.Username, int.Parse(e.ChatMessage.UserId), e.ChatMessage.IsModerator,
                    e.ChatMessage.Message);
            }
            else
            {
                MySQL.MySqlHandler.Instance.Messages.ProcessChatMessage(e.ChatMessage.Username, int.Parse(e.ChatMessage.UserId), e.ChatMessage.IsModerator,
                    e.ChatMessage.Message, e.ChatMessage.Bits);
            }
        }

        private void ProcessChatCommandMessage(ChatMessage chatMessage)
        {
            if (_client == null) return;

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
                        var walls = db.Messages.Count(x => x.Message.ToLower().Contains("wall") && !x.Message.StartsWith("Stats"));
                        _client.SendMessage(chatMessage.Channel, $"Stats: At least {walls} messages have been sent describing how much @Mind1 shoots walls - GO MIND!");
                    }
                    break;

                case "!!stats":
                    _userStatsCommand.Handle(chatMessage, parameters);
                    break;

                case "!!dead":
                    using ( var db = new MySQL.DBConnection.Database())
                    {
                        var deadCmds = db.Commands.Where(x => x.Command.ToLower().Equals("!dead"));
                        _client.SendMessage(chatMessage.Channel, $"Stats: Mind1 has died at least {deadCmds.Count()} times - Ouch! (num of !dead used)");
                    }
                    break;

                case "!!about":
                    using ( var db = new MySQL.DBConnection.Database() )
                    {
                        var totalUsers = db.Users.Count();
                        var totalMessages = db.Messages.Count();
                        _client.SendMessage(chatMessage.Channel, $"I am Twitch Monitor Robot v{Program.Version} - I am logging everything that happens here.  Currently watching {totalUsers:n0} users having sent {totalMessages:n0} chat messages! - Created by AlienX");
                    }
                    break;

                case "!!top":
                    _topCommand.Handle(chatMessage, parameters);
                    break;

                case "!!help":
                    _client.SendMessage(chatMessage.Channel, $"Commands are: !!top [redeem], !!about, !!help (more added soon!)");
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
                _client!.SendMessage(chatMessage.Channel, $"Sorry {chatMessage.Username}, you do not have access to that command");
                return false;
            }
        }

        private void Client_OnNewSubscriber(object? sender, OnNewSubscriberArgs e)
        {
            MySQL.MySqlHandler.Instance.Subscriptions.ProcessSubscription(e.Subscriber.DisplayName, e.Subscriber.UserId, false, e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime);
        }

        private void ClientOnOnReSubscriber(object? sender, OnReSubscriberArgs e)
        {
            MySQL.MySqlHandler.Instance.Subscriptions.ProcessSubscription(e.ReSubscriber.DisplayName, e.ReSubscriber.UserId, true, e.ReSubscriber.SubscriptionPlan == SubscriptionPlan.Prime);
        }

        private void Client_OnGiftedSubscription(object? sender, OnGiftedSubscriptionArgs e)
        {
            MySQL.MySqlHandler.Instance.Subscriptions.ProcessSubscription(e.GiftedSubscription.MsgParamRecipientDisplayName, e.GiftedSubscription.MsgParamRecipientId, false, false, true, e.GiftedSubscription.DisplayName, e.GiftedSubscription.UserId);
        }

        public void ProcessStreamOnline()
        {
            if (_client is not {IsConnected: true}) return;

            _client.SendMessage(ConfigurationHandler.Instance.Configuration.TwitchChat.ChannelName, $"[BOT] Stream Started - Monitoring Chat and Bit Events.");
        }

        public void ProcessStreamOffline()
        {
            if (_client is not {IsConnected: true}) return;
            _client.SendMessage(ConfigurationHandler.Instance.Configuration.TwitchChat.ChannelName, $"[BOT] Stream Ended - Disabling Chat and Bit Event Hooks.");
        }

        public void Dispose()
        {
            if (_client == null) return;

            ConsoleUtil.WriteToConsole("Disposing Twitch IRC Chat Classes...", ConsoleUtil.LogLevel.Info);

            foreach (var channel in _client.JoinedChannels)
            {
                ConsoleUtil.WriteToConsole($"Attemtping to leave channel {channel.Channel}", ConsoleUtil.LogLevel.Info);
                _client.LeaveChannel(channel.Channel);
            }
            
            ConsoleUtil.WriteToConsole("Disconnecting from Twitch IRC", ConsoleUtil.LogLevel.Info);
            _client.Disconnect();
            ConsoleUtil.WriteToConsole("Disconnecting from Twitch IRC - Done", ConsoleUtil.LogLevel.Info);

            var maxTries = 10;
            var currentTry = 1;
            while (_client.IsConnected)
            {
                ConsoleUtil.WriteToConsole($"Attempting to disconnect from Twitch Chat IRC [{currentTry}/{maxTries}]", ConsoleUtil.LogLevel.Info);
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
