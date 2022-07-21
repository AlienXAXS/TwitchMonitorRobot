using LinqToDB;
using System;
using System.Linq;
using LinqToDB.Data;

namespace TMRAgent.MySQL.Function
{
    internal class Streams
    {

        public void CheckForStreamUpdate()
        {
            if (Twitch.TwitchHandler.Instance.LivestreamMonitorService.CurrentLiveStreamId != -1)
            {
                if (Twitch.TwitchHandler.Instance.LivestreamMonitorService.LastUpdateTime < DateTime.Now.ToUniversalTime().AddMinutes(-2))
                {
                    ProcessStreamUpdate();
                    Twitch.TwitchHandler.Instance.LivestreamMonitorService.LastUpdateTime = DateTime.Now.ToUniversalTime();
                }
            }
        }

        internal void ProcessStreamOnline(DateTime dateTime)
        {
            try
            {
                using (var db = new DBConnection.Database())
                {
                    var currentStreamDbEntry = db.Streams.DefaultIfEmpty(null).Where(x =>
                        x.LastSeen.Between(DateTime.Now.ToUniversalTime().AddMinutes(-120),
                            DateTime.Now.ToUniversalTime()) || x.Start.Equals(dateTime));
                    var currentStream = currentStreamDbEntry.ToList().OrderBy(x => x.LastSeen).FirstOrDefault();
                    if (currentStream != null)
                    {
                        ConsoleUtil.WriteToConsole($"[StreamEvent] Found an existing row in the Database for this ongoing stream, using StreamID {currentStream.Id} (Stream Started At {currentStream.Start}).", ConsoleUtil.LogLevel.Info, ConsoleColor.Yellow);
                        Twitch.TwitchHandler.Instance.LivestreamMonitorService.CurrentLiveStreamId = currentStream.Id;
                        db.Query<dynamic>($"UPDATE `streams` SET `end` = NULL WHERE `streams`.`id` = {currentStream.Id};");
                    }
                    else
                    {
                        Twitch.TwitchHandler.Instance.LivestreamMonitorService.CurrentLiveStreamId = (int)db.Streams
                            .Value(p => p.Start, dateTime)
                            .Value(p => p.LastSeen, DateTime.Now.ToUniversalTime())
                            .InsertWithInt32Identity()!;

                        Twitch.TwitchHandler.Instance.ChatService.ProcessStreamOnline();
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
            if (Twitch.TwitchHandler.Instance.LivestreamMonitorService.CurrentLiveStreamId == -1) return;

            using (var db = new DBConnection.Database())
            {
                db.Streams.Where(p => p.Id == Twitch.TwitchHandler.Instance.LivestreamMonitorService.CurrentLiveStreamId)
                    .Set(p => p.LastSeen, DateTime.Now.ToUniversalTime())
                    .Update();
            }
        }

        internal void ProcessStreamOffline(DateTime dateTime, int? Viewers)
        {
            try
            {
                if (Twitch.TwitchHandler.Instance.LivestreamMonitorService.CurrentLiveStreamId == -1)
                {
                    return;
                }

                using (var db = new DBConnection.Database())
                {
                    db.Streams
                        .Where(p => p.Id == Twitch.TwitchHandler.Instance.LivestreamMonitorService.CurrentLiveStreamId)
                        .Set(p => p.End, dateTime)
                        .Set(p => p.Viewers, Viewers)
                        .Update();
                }

                Twitch.TwitchHandler.Instance.LivestreamMonitorService.CurrentLiveStreamId = -1;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to add current stream as being Offline\r\n\r\n{ex.Message}");
            }
        }
    }
}
