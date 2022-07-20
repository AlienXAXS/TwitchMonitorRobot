#nullable enable
using TMRAgent.Twitch.Utility;

namespace TMRAgent.Twitch
{
    public class TwitchHandler
    {
        public static TwitchHandler Instance = _instance ??= new TwitchHandler();
        private static readonly TwitchHandler? _instance;

        public Auth Auth = new();

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
