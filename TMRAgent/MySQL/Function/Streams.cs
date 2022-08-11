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
            if (Twitch.TwitchHandler.Instance.CurrentLiveStreamId != null && Twitch.TwitchHandler.Instance.LastUpdateTime != null)
            {
                if (Twitch.TwitchHandler.Instance.LastUpdateTime < DateTime.Now.ToUniversalTime().AddMinutes(-2))
                {
                    ProcessStreamUpdate();
                    Twitch.TwitchHandler.Instance.LastUpdateTime = DateTime.Now.ToUniversalTime();
                }
            }
        }

        internal void ProcessStreamOnline(DateTime dateTime, bool force = false)
        {
            try
            {
                Util.Log($"[MySQL] ProcessStreamOnline Fired -> DateTime: {dateTime}", Util.LogLevel.Info);
                using (var db = new DBConnection.Database())
                {
                    if (!force)
                    {
                        var currentStreamDbEntry = db.Streams.DefaultIfEmpty(null).Where(x =>
                            x.LastSeen.Between(DateTime.Now.ToUniversalTime().AddMinutes(-120),
                                DateTime.Now.ToUniversalTime()) || x.Start.Equals(dateTime));
                        var currentStream = currentStreamDbEntry.ToList().OrderBy(x => x.LastSeen).FirstOrDefault();
                        if (currentStream != null)
                        {
                            Util.Log(
                                $"[StreamEvent] Found an existing row in the Database for this ongoing stream, using StreamID {currentStream.Id} (Stream Started At {currentStream.Start}).",
                                Util.LogLevel.Info, ConsoleColor.Yellow);
                            Twitch.TwitchHandler.Instance.CurrentLiveStreamId = currentStream.Id;
                            db.Query<dynamic>(
                                $"UPDATE `streams` SET `end` = NULL WHERE `streams`.`id` = {currentStream.Id};");
                        }
                        else
                        {
                            Twitch.TwitchHandler.Instance.CurrentLiveStreamId = (int) db.Streams
                                .Value(p => p.Start, dateTime)
                                .Value(p => p.LastSeen, DateTime.Now.ToUniversalTime())
                                .InsertWithInt32Identity()!;

                            Twitch.TwitchHandler.Instance.LastUpdateTime = DateTime.Now.ToUniversalTime();

                            Util.Log(
                                $"New stream database entry created with ID {Twitch.TwitchHandler.Instance.CurrentLiveStreamId}",
                                Util.LogLevel.Info);

                            Twitch.TwitchHandler.Instance.ChatService.ProcessStreamOnline();
                        }
                    }
                    else
                    {
                        Twitch.TwitchHandler.Instance.CurrentLiveStreamId = (int)db.Streams
                            .Value(p => p.Start, dateTime)
                            .Value(p => p.LastSeen, DateTime.Now.ToUniversalTime())
                            .InsertWithInt32Identity()!;

                        Twitch.TwitchHandler.Instance.LastUpdateTime = DateTime.Now.ToUniversalTime();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to add current stream as being Online\r\n\r\n{ex.Message}");
            }
        }

        public void CleanDirtyStreams()
        {
            using (var db = new DBConnection.Database())
            {
                var dirtyStreamList = db.Streams.Where(x => x.End == null).ToList();
                foreach (var dirtyStream in dirtyStreamList)
                {
                    db.Streams.Where(x => x.Id.Equals(dirtyStream.Id))
                        .Set(p => p.End, DateTime.Now.ToUniversalTime() - new TimeSpan(0, 2, 0, 0))
                        .Set(p => p.LastSeen, DateTime.Now.ToUniversalTime() - new TimeSpan(0, 2, 0, 0))
                        .Update();
                    Util.Log($"Cleaning Dirty Stream {dirtyStream.Id} which started at {dirtyStream.Start}!", Util.LogLevel.Info);
                }
            }
        }

        internal void ProcessStreamUpdate()
        {
            if (Twitch.TwitchHandler.Instance.CurrentLiveStreamId == -1) return;

            using (var db = new DBConnection.Database())
            {
                db.Streams.Where(p => p.Id == Twitch.TwitchHandler.Instance.CurrentLiveStreamId)
                    .Set(p => p.LastSeen, DateTime.Now.ToUniversalTime())
                    .Update();
            }
        }

        internal void ProcessStreamOffline(DateTime dateTime, int? Viewers)
        {
            try
            {
                if (Twitch.TwitchHandler.Instance.CurrentLiveStreamId == -1)
                {
                    return;
                }

                using (var db = new DBConnection.Database())
                {
                    db.Streams
                        .Where(p => p.Id == Twitch.TwitchHandler.Instance.CurrentLiveStreamId)
                        .Set(p => p.End, dateTime)
                        .Set(p => p.Viewers, Viewers)
                        .Update();
                }

                Twitch.TwitchHandler.Instance.CurrentLiveStreamId = -1;
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to add current stream as being Offline\r\n\r\n{ex.Message}");
            }
        }
    }
}
