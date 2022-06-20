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
                    Twitch.TwitchLiveMonitor.Instance.CurrentLiveStreamId = (int)db.Streams
                        .Value(p => p.Start, dateTime)
                        .InsertWithInt32Identity();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to add current stream as being Online\r\n\r\n{ex.Message}");
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
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to add current stream as being Offline\r\n\r\n{ex.Message}");
            }
        }
    }
}
