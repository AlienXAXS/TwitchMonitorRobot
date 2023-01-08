#nullable enable
using System;
using TMRAgent.Twitch.Utility;
using System.Linq;
using LinqToDB;

namespace TMRAgent.Twitch
{
    public class TwitchHandler
    {
        public static TwitchHandler Instance = _instance ??= new TwitchHandler();
        private static readonly TwitchHandler? _instance;

        public Auth Auth = new();

        public Chat.ChatHandler ChatService = new();
        public Events.PubSubHandler PubSubService = new();

        public int? CurrentLiveStreamId = null;
        public DateTime? LastUpdateTime = null;

        public void CheckForExistingStream()
        {
            if (CurrentLiveStreamId != null) return;

            using (var db = new MySQL.DBConnection.Database())
            {
                var currentStreamDbEntry = db.Streams.DefaultIfEmpty(null).Where(x =>
                    x.LastSeen.Between(DateTime.Now.ToUniversalTime().AddMinutes(-120),
                        DateTime.Now.ToUniversalTime()) || x.Start.Equals(DateTime.Now.ToUniversalTime()));
                var currentStream = currentStreamDbEntry.ToList().OrderBy(x => x.LastSeen).FirstOrDefault();
                if (currentStream != null)
                {
#if !DEBUG
                    ChatService.SendMessage($"[TMR] v{Program.Version} Started! | Found a stream that started at {currentStream.Start}.  Logging messages and other stream events!");
#endif
                    Util.Log($"Previous stream found. Started At: {currentStream.Start}, Last Seen: {currentStream.LastSeen}", Util.LogLevel.Info);
                    CurrentLiveStreamId = currentStream.Id;
                    LastUpdateTime = DateTime.Now.ToUniversalTime();
                }
                else
                {
                    Util.Log($"No previous stream found, waiting for a stream to start.", Util.LogLevel.Info);
                }
            }
        }

        public void Dispose()
        {
            ChatService.Dispose();
            PubSubService.Dispose();
        }
    }
}
