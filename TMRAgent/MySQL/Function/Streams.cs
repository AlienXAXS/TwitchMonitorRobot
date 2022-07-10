using LinqToDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMRAgent.MySQL.Function
{
    internal class Streams
    {
        internal void ProcessStreamOnline(DateTime dateTime)
        {
            try
            {
                using (var db = new DBConnection.Database())
                {
                    var currentStream = db.Streams.DefaultIfEmpty(null).FirstOrDefault(x => x.End.Equals(null) && (x.LastSeen.Between(DateTime.Now.ToUniversalTime().AddMinutes(-30), DateTime.Now.ToUniversalTime()) || x.Start.Equals(dateTime)));
                    if (currentStream != null)
                    {
                        ConsoleUtil.WriteToConsole($"[StreamEvent] Found an existing row in the Database for this ongoing stream, using StreamID {currentStream.Id} (Stream Started At {currentStream.Start}).", ConsoleUtil.LogLevel.INFO, ConsoleColor.Yellow);
                        Twitch.TwitchLiveMonitor.Instance.CurrentLiveStreamId = currentStream.Id;
                    }
                    else
                    {
                        Twitch.TwitchLiveMonitor.Instance.CurrentLiveStreamId = (int)db.Streams
                            .Value(p => p.Start, dateTime)
                            .Value(p => p.LastSeen, DateTime.Now.ToUniversalTime())
                            .InsertWithInt32Identity();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to add current stream as being Online\r\n\r\n{ex.Message}");
            }
        }

        internal void ProcessStreamUpdate()
        {
            if (Twitch.TwitchLiveMonitor.Instance.CurrentLiveStreamId == -1) return;

            using (var db = new DBConnection.Database())
            {
                db.Streams.Where(p => p.Id == Twitch.TwitchLiveMonitor.Instance.CurrentLiveStreamId)
                    .Set(p => p.LastSeen, DateTime.Now.ToUniversalTime())
                    .Update();
            }
        }

        internal void ProcessStreamOffline(DateTime dateTime, int Viewers)
        {
            try
            {
                if (Twitch.TwitchLiveMonitor.Instance.CurrentLiveStreamId == -1)
                {
                    throw new Exception($"Current stream does not have a known ID, unable to set it offline!");
                }

                using (var db = new DBConnection.Database())
                {
                    db.Streams
                        .Where(p => p.Id == Twitch.TwitchLiveMonitor.Instance.CurrentLiveStreamId)
                        .Set(p => p.End, dateTime)
                        .Set(p => p.Viewers, Viewers)
                        .Update();
                }

                Twitch.TwitchLiveMonitor.Instance.CurrentLiveStreamId = -1;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to add current stream as being Offline\r\n\r\n{ex.Message}");
            }
        }
    }
}
