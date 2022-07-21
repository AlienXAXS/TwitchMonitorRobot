using System;
using System.Linq;
using System.Threading.Tasks;
using TMRAgent.MySQL.Commands;
using TMRAgent.Twitch.Utility;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace TMRAgent.Twitch.Chat
{
    public class ChatHandler
    {
        private readonly AddModeratorCommand _addModeratorCommand = new();
        private readonly RemoveModeratorCommand _removeModeratorCommand = new();
        private readonly UserManager _userManagerCommand = new();
        private readonly TopCommand _topCommand = new();
        private readonly UserStatsCommand _userStatsCommand = new();

        private TwitchClient? _client;
        public TwitchClient? GetTwitchClient()
        {
            return _client;
        }

        public void Connect()
        {
            Task.Run(ConnectTask);
        }

        private Task ConnectTask()
        {
            // Validate OAuth Token
            try
            {
                TwitchHandler.Instance.Auth.Validate(Auth.AuthType.TwitchChat, true);
            }
            catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"[TwitchChat] Unable to connect to TwitchChat.  OAuth Validation failed. Error: {ex.Message}", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
                return Task.CompletedTask;
            }

            
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                ReconnectionPolicy = new ReconnectionPolicy(0, 0),
            };

            WebSocketClient customClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(customClient);

            ConnectionCredentials credentials = new ConnectionCredentials(ConfigurationHandler.Instance.Configuration.TwitchChat.Username!, ConfigurationHandler.Instance.Configuration.TwitchChat.AuthToken!);
            _client.Initialize(credentials, ConfigurationHandler.Instance.Configuration.TwitchChat.ChannelName!);

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

            return Task.CompletedTask;
        }

        private void SetCredentials()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(ConfigurationHandler.Instance.Configuration.TwitchChat.Username!, ConfigurationHandler.Instance.Configuration.TwitchChat.AuthToken!);
            _client?.SetConnectionCredentials(credentials);
        }

        private void ClientOnOnIncorrectLogin(object? sender, OnIncorrectLoginArgs e)
        {
            if (_client == null) return;

            ConsoleUtil.WriteToConsole($"[TwitchChat] Invalid/Expired Auth Credentials", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
            _client.Disconnect();

            if (!TwitchHandler.Instance.Auth.TestAuth(Auth.AuthType.TwitchChat))
            {
                ConsoleUtil.WriteToConsole("[TwitchChat] Attemtping a token refresh", ConsoleUtil.LogLevel.Info);
                if (TwitchHandler.Instance.Auth.RefreshToken(Auth.AuthType.TwitchChat).GetAwaiter().GetResult())
                {
                    ConsoleUtil.WriteToConsole($"[TwitchChat] Token refresh successful, reconnecting to TwitchChat", ConsoleUtil.LogLevel.Info);
                    SetCredentials();
                    _client?.Connect();
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
            ConsoleUtil.WriteToConsole($"[TwitchClient] TwitchChat Client Connection Error!", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
            if (!Program.ExitRequested)
            {
                SetCredentials();
                _client?.Connect();
            }
        }

        private void ClientOnOnNoPermissionError(object? sender, EventArgs e)
        {

        }

        private void Client_OnDisconnected(object? sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            ConsoleUtil.WriteToConsole($"[TwitchClient] TwitchChat Client has disconnected!", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
            if (!Program.ExitRequested)
            {
                SetCredentials();
                _client?.Connect();
            }
        }

        private void Client_OnRitualNewChatter(object? sender, OnRitualNewChatterArgs e)
        {
            ConsoleUtil.WriteToConsole($"Possible First Time Chatter: {e.RitualNewChatter.DisplayName}", ConsoleUtil.LogLevel.Info, ConsoleColor.Cyan);
        }

        private void ClientOnOnChannelStateChanged(object? sender, OnChannelStateChangedArgs e)
        {
            MySQL.MySqlHandler.Instance.Streams.CheckForStreamUpdate();
        }

        private void Client_OnLog(object? sender, OnLogArgs e)
        {
            MySQL.MySqlHandler.Instance.Streams.CheckForStreamUpdate();
        }

        private void Client_OnConnected(object? sender, OnConnectedArgs e)
        {
            ConsoleUtil.WriteToConsole($"[Twitch Bot] Connected to Twitch IRC", ConsoleUtil.LogLevel.Info);
        }

        private void Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
        {
            ConsoleUtil.WriteToConsole($"[Twitch Bot] Joined channel {e.Channel}", ConsoleUtil.LogLevel.Info);

        }
        public void ProcessStreamOnline()
        {
            if (_client is not { IsConnected: true }) return;

            _client.SendMessage(ConfigurationHandler.Instance.Configuration.TwitchChat.ChannelName!, $"[BOT] Stream Started - Monitoring Chat and Bit Events.");
        }

        public void ProcessStreamOffline()
        {
            if (_client is not { IsConnected: true }) return;
            _client.SendMessage(ConfigurationHandler.Instance.Configuration.TwitchChat.ChannelName!, $"[BOT] Stream Ended - Disabling Chat and Bit Event Hooks.");
        }

        private void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            MySQL.MySqlHandler.Instance.Streams.CheckForStreamUpdate();

            if (e.ChatMessage.Message.StartsWith("!!"))
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

        private void ProcessChatCommandMessage(ChatMessage chatMessage)
        {
            if (_client == null) return;

            var parameters = chatMessage.Message.Trim().Split(' ');

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

                case "!!shutdown":
                    if (!IsUserModeratorOrBroadcaster(chatMessage)) return;
                    _client.SendMessage(chatMessage.Channel, "TMR Shutting down now, Byeeee!");
                    Program.QuitAppEvent.Set();
                    break;

                case "!!manage_user":
                    if (!IsUserModeratorOrBroadcaster(chatMessage)) return;
                    _userManagerCommand.Handle(chatMessage, parameters);
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
                    using (var db = new MySQL.DBConnection.Database())
                    {
                        var deadCmds = db.Commands.Where(x => x.Command.ToLower().Equals("!dead"));
                        _client.SendMessage(chatMessage.Channel, $"Stats: Mind1 has died at least {deadCmds.Count()} times - Ouch! (num of !dead used)");
                    }
                    break;

                case "!!about":
                    using (var db = new MySQL.DBConnection.Database())
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
                    _client.SendMessage(chatMessage.Channel, $"Commands are: !!top [redeem], !!about, !!stats, !!dead, !!walls.  (Mods Only: !!add_mod_action, !!remove_mod_action, !!manage_user, !!shutdown)");
                    break;
            }
        }

        private bool IsUserModeratorOrBroadcaster(ChatMessage chatMessage)
        {
            if (chatMessage.IsModerator || chatMessage.IsBroadcaster)
            {
                return true;
            }
            else
            {
                _client!.SendMessage(chatMessage.Channel, $"Sorry {chatMessage.Username}, you do not have access to that command");
                return false;
            }
        }

        public void Dispose()
        {
            _client?.Disconnect();
        }
    }
}
