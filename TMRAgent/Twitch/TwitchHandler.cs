#nullable enable
using TMRAgent.Twitch.Utility;

namespace TMRAgent.Twitch
{
    public class TwitchHandler
    {
        public static TwitchHandler Instance = _instance ??= new TwitchHandler();
        private static readonly TwitchHandler? _instance;

        public Auth Auth = new();

        public Chat.ChatHandler ChatService = new();
        public Events.LivestreamMonitorService LivestreamMonitorService = new();
        public Events.PubSubHandler PubSubService = new();

        public void Dispose()
        {
            ChatService.Dispose();
            LivestreamMonitorService.Dispose();
            PubSubService.Dispose();
        }
    }
}
