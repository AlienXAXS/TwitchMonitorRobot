using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.Cms;

namespace TMRAgent.MySQL.Commands
{
    internal class StreamCommands
    {
        public void HandleForceStreamOnline(TwitchLib.Client.Models.ChatMessage message, string[] parameters)
        {
            var tc = Twitch.TwitchHandler.Instance.ChatService.GetTwitchClient();

            if (parameters.Length == 2)
            {
                var durationRaw = parameters[1].ToLower();
                var durationRawSplit = durationRaw.Split(':');
                if (durationRawSplit.Length == 3)
                {
                    if (int.TryParse(durationRawSplit[0], out var hours))
                    {
                        if (int.TryParse(durationRawSplit[1], out var minutes))
                        {
                            if (int.TryParse(durationRawSplit[2], out var seconds))
                            {
                                var duration = new TimeSpan(0, hours, minutes, seconds);
                                var startTime = DateTime.Now.ToUniversalTime() - duration;

                                try
                                {
                                    MySqlHandler.Instance.Streams.CleanDirtyStreams();
                                    MySqlHandler.Instance.Streams.ProcessStreamOnline(startTime, true);
                                    tc?.SendMessage(message.Channel, $"[TMR] Successfully forced a new stream to start, assuming start time of {startTime}UTC");
                                }
                                catch (Exception ex)
                                {
                                    tc?.SendMessage(message.Channel, $"[TMR] Unable to process request, MySQL Error: {ex.Message}");
                                }


                                return;
                            }
                        }
                    }

                    tc?.SendMessage(message.Channel,
                        $"[TMR] Invalid usage: !!stream_online CurrentDuration (Duration format: hh:mm:ss)");
                }
                else
                {
                    tc?.SendMessage(message.Channel,
                        $"[TMR] Invalid usage: !!stream_online CurrentDuration (Duration format: hh:mm:ss)");
                }
            }
            else
            {
                tc?.SendMessage(message.Channel,
                    $"[TMR] Invalid usage: !!stream_online CurrentDuration (Duration format: hh:mm:ss)");
            }
        }
    }
}
