#nullable enable
using System;
using System.Linq;
using System.Threading;
using TMRAgent.MySQL.Commands;
using TMRAgent.Twitch.Utility;
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

        public Auth Auth = new Auth();

        public Chat.ChatHandler ChatHandler = new();
        public Events.LivestreamMonitorService LivestreamMonitorService = new();
        public Events.PubSubHandler PubSubHandler = new();

        public void Dispose()
        {
            ChatHandler.Dispose();
            LivestreamMonitorService.Dispose();
            PubSubHandler.Dispose();
        }
    }
}
