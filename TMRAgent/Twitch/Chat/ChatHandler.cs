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
        private readonly StreamCommands _streamCommands = new();
        private readonly AlbionOnlineLookup _albionOnlineLookup = new();

        private readonly Object Lockable = new();

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
                Util.Log(
                    $"[TwitchChat] Unable to connect to TwitchChat.  OAuth Validation failed. Error: {ex.Message}",
                    Util.LogLevel.Error, ConsoleColor.Red);
                return Task.CompletedTask;
            }

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                ReconnectionPolicy = null // Disable reconnection
            };

            WebSocketClient customClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(customClient)
            {
                AutoReListenOnException = false,
            };

            ConnectionCredentials credentials = new ConnectionCredentials(ConfigurationHandler.Instance.Configuration.TwitchChat.Username!, ConfigurationHandler.Instance.Configuration.TwitchChat.AuthToken!);
            _client.Initialize(credentials);

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
            _client.OnWhisperReceived += ClientOnOnWhisperReceived;

            // Subscriptions
            _client.OnNewSubscriber += Client_OnNewSubscriber;
            _client.OnReSubscriber += ClientOnOnReSubscriber;
            _client.OnGiftedSubscription += Client_OnGiftedSubscription;

            _client.Connect();

            return Task.CompletedTask;
        }

        private bool CheckAuth()
        {
            return TwitchHandler.Instance.Auth.TestAuth(Auth.AuthType.TwitchChat);
        }

        private void ClientOnOnWhisperReceived(object? sender, OnWhisperReceivedArgs e)
        {
            if (e.WhisperMessage.Message.ToLower().Equals("ping"))
            {
                if (e.WhisperMessage.Username.ToLower().Equals("mind1"))
                {
                    _client?.SendWhisper(e.WhisperMessage.Username, "Hi Dad, HOW DARE you ping me!");
                    _client?.SendWhisper(e.WhisperMessage.Username, "[TMR] Oh yeah... Pong!");
                }
                else
                {
                    _client?.SendWhisper(e.WhisperMessage.Username, "[TMR] Pong!");
                }
            }
        }

        private void SetCredentials()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(ConfigurationHandler.Instance.Configuration.TwitchChat.Username!, ConfigurationHandler.Instance.Configuration.TwitchChat.AuthToken!);
            _client?.SetConnectionCredentials(credentials);
        }

        private void ClientOnOnIncorrectLogin(object? sender, OnIncorrectLoginArgs e)
        {
            if (_client == null) return;
            if (Program.ExitRequested) return;

            Util.Log($"[TwitchChat] Invalid/Expired Auth Credentials", Util.LogLevel.Error, ConsoleColor.Red);
            
            if (_client.IsConnected) _client.Disconnect();

            if (!TwitchHandler.Instance.Auth.TestAuth(Auth.AuthType.TwitchChat))
            {
                Util.Log("[TwitchChat] Attemtping a token refresh", Util.LogLevel.Info);
                if (TwitchHandler.Instance.Auth.RefreshToken(Auth.AuthType.TwitchChat).GetAwaiter().GetResult())
                {
                    Util.Log($"[TwitchChat] Token refresh successful, reconnecting to TwitchChat", Util.LogLevel.Info);
                    SetCredentials();
                    _client?.Connect();
                }
                else
                {
                    Util.Log("[TwitchChat] Unable to refresh TwitchChat Token, Application will now exit.", Util.LogLevel.Error, ConsoleColor.Red);
                    Program.InvokeApplicationExit();
                }
            }
        }

        private void ClientOnOnFailureToReceiveJoinConfirmation(object? sender, OnFailureToReceiveJoinConfirmationArgs e)
        {
            Util.Log($"[TwitchClient] Failed to join channel {e.Exception.Channel}", Util.LogLevel.Error, ConsoleColor.Red);
        }

        private void ClientOnOnConnectionError(object? sender, OnConnectionErrorArgs e)
        {
            Util.Log($"[TwitchClient] TwitchChat Client Connection Error! -> {e.Error.Message}", Util.LogLevel.Error, ConsoleColor.Red);
            if (!Program.ExitRequested)
            {
                if (!CheckAuth())
                    SetCredentials();

                _client?.Reconnect();
            }
        }

        private void ClientOnOnNoPermissionError(object? sender, EventArgs e)
        {
            
        }

        private void Client_OnDisconnected(object? sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Util.Log($"[TwitchClient] TwitchChat Client has disconnected!", Util.LogLevel.Error, ConsoleColor.Red);
            if (!Program.ExitRequested)
            {
                if (!CheckAuth())
                    SetCredentials();

                _client?.Connect();
            }
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
            Util.Log($"[Twitch Bot] Connected to Twitch IRC", Util.LogLevel.Info);
            if (_client != null && !_client.JoinedChannels.Any(x =>
                    x.Channel.Equals(ConfigurationHandler.Instance.Configuration.TwitchChat.ChannelName!)))
            {
                _client?.JoinChannel(ConfigurationHandler.Instance.Configuration.TwitchChat.ChannelName!);
            }
        }

        private void Client_OnJoinedChannel(object? sender, OnJoinedChannelArgs e)
        {
            Util.Log($"[Twitch Bot] Joined channel {e.Channel}", Util.LogLevel.Info);

            Util.Log("Checking for an existing stream", Util.LogLevel.Info);
            TwitchHandler.Instance.CheckForExistingStream();
        }

        public void ProcessStreamOnline()
        {
            if (_client is {IsConnected: false}) return;
            _client?.SendMessage(ConfigurationHandler.Instance.Configuration.TwitchChat.ChannelName!, $"[TMR-Agent-Bot] Stream Started - Monitoring Chat and Bit Events.");
        }

        public void ProcessStreamOffline()
        {
            if (_client is not { IsConnected: true }) return;
            _client.SendMessage(ConfigurationHandler.Instance.Configuration.TwitchChat.ChannelName!, $"[TMR-Agent-Bot] Stream Offline, Cleaning up bot cache and flushing database tables to disk.");
        }

        private void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            MySQL.MySqlHandler.Instance.Streams.CheckForStreamUpdate();

            if (ProcessChatCommandMessage(e.ChatMessage))
            {
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

        private bool ProcessChatCommandMessage(ChatMessage chatMessage)
        {
            if (_client == null) return false;

            var parameters = chatMessage.Message.Trim().Split(' ');

            switch (parameters[0].ToLower())
            {
                case "!!stream_online":
                    if (!IsUserModeratorOrBroadcaster(chatMessage)) return true;
                    _streamCommands.HandleForceStreamOnline(chatMessage, parameters);
                    return true;

                case "!!add_mod_action":
                    if (!IsUserModeratorOrBroadcaster(chatMessage)) return true;
                    _addModeratorCommand.Handle(chatMessage, parameters);
                    return true;

                case "!!remove_mod_action":
                    if (!IsUserModeratorOrBroadcaster(chatMessage)) return true;
                    _removeModeratorCommand.Handle(chatMessage, parameters);
                    return true;

                case "!!shutdown":
                    if (!IsUserModeratorOrBroadcaster(chatMessage)) return true;
                    _client.SendMessage(chatMessage.Channel, "[BOT] TMR Requested a shutdown.");
                    Program.InvokeApplicationExit();
                    return true;

                case "!!manage_user":
                    if (!IsUserModeratorOrBroadcaster(chatMessage)) return true;
                    _userManagerCommand.Handle(chatMessage, parameters);

                    return true;

                case "!!walls":
                    using (var db = new MySQL.DBConnection.Database())
                    {
                        var walls = db.Messages.Count(x => x.Message.ToLower().Contains("wall") && !x.Message.StartsWith("Stats"));
                        _client.SendMessage(chatMessage.Channel, $"Stats: At least {walls} messages have been sent describing how much @Mind1 shoots walls - GO MIND!");
                    }
                    return true;

                case "!!stats":
                    _userStatsCommand.Handle(chatMessage, parameters);
                    return true;

                case "!!dead":
                    using (var db = new MySQL.DBConnection.Database())
                    {
                        var deadCmds = db.Commands.Where(x => x.Command.ToLower().Equals("!dead"));
                        _client.SendMessage(chatMessage.Channel, $"Stats: Mind1 has died at least {deadCmds.Count()} times - Ouch! (num of !dead used)");
                    }
                    return true;

                case "!!about":
                    using (var db = new MySQL.DBConnection.Database())
                    {
                        var totalUsers = db.Users.Count();
                        var totalMessages = db.Messages.Count();
                        _client.SendMessage(chatMessage.Channel, $"I am Twitch Monitor Robot v{Program.Version} - I am logging everything that happens here.  Currently watching {totalUsers:n0} users having sent {totalMessages:n0} chat messages! - Created by AlienX");
                    }
                    return true;

                case "!!top":
                    _topCommand.Handle(chatMessage, parameters);
                    return true;

                case "!!help":
                    _client.SendMessage(chatMessage.Channel, $"Commands are: !!top [redeem], !!about, !!stats, !!dead, !!walls, !aol, !aor  (Mods Only: !!add_mod_action, !!remove_mod_action, !!manage_user, !!shutdown)");
                    return true;

                case "!!aol":
                case "!!aor":
                case "!aol":
                case "!aor":
                    _albionOnlineLookup.ProcessCommandMessage(chatMessage, parameters);
                    return true;
            }

            return false;
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
            if (_client == null) return;

            if ( _client.IsConnected )
                _client.Disconnect();
        }

        public void SendMessage(string message)
        {
            _client?.SendMessage(ConfigurationHandler.Instance.Configuration.TwitchChat.ChannelName, message);
        }

        public void SendMessage(string channel, string message)
        {
            _client?.SendMessage(channel, message);
        }
    }
}
