using System;
using System.Linq;
using TwitchLib.Client.Models;

namespace TMRAgent.MySQL.Commands
{
    internal class UserStatsCommand
    {
        public void Handle(ChatMessage chatMessage, string[] parameters)
        {
            using (var db = new DBConnection.Database())
            {
                Models.Users userDb;
                if ( parameters.Length == 1 )
                {
                    userDb = db.Users.DefaultIfEmpty(null).FirstOrDefault(x => x.TwitchId == int.Parse(chatMessage.UserId));
                } else
                {
                    if (parameters[1].ToLower().Equals("stream"))
                    {
                        HandleStreamStats(chatMessage);
                        return;
                    }

                    userDb = db.Users.DefaultIfEmpty(null).FirstOrDefault(x => x.Username == parameters[1]);
                }
                
                if ( userDb == null )
                {
                    Twitch.TwitchHandler.Instance.ChatService.GetTwitchClient()?.SendMessage(chatMessage.Channel, $"Sorry {chatMessage.DisplayName}, Something went wrong and I cannot get your stats!");
                    return;
                }

                var messageCount = db.Messages.Count(x => x.UserId == userDb.Id);
                var redeemSpent = 0;
                foreach ( var bitRedeem in db.BitRedeems.Where(x => x.UserId == userDb.Id))
                {
                    redeemSpent = redeemSpent + bitRedeem.Cost;
                }

                var responseMessage = $"{chatMessage.DisplayName}: {(parameters.Length == 2 ? $"{userDb.Username} has" : "You've")} sent {messageCount:n0} chat messages";

                if (redeemSpent > 0)
                {
                    responseMessage += " and redeemed {redeemSpent:n0} Brain Cells!.";
                }
                else
                {
                    responseMessage += ".";
                }

                var cmdsPreTable = db.Commands.AsEnumerable().Where(x => x.UserId == userDb.Id).GroupBy(x => x.Command);
                if (cmdsPreTable.Count() != 0)
                {
                    var mostUsedCommand = cmdsPreTable.OrderByDescending(x => x.Count()).First();

                    responseMessage += $" {(parameters.Length == 2 ? "Their" : "Your")} most used command is {mostUsedCommand.Key} which {(parameters.Length == 2 ? "they've" : "you've")} used {mostUsedCommand.Count():n0} times!";
                }

                Twitch.TwitchHandler.Instance.ChatService.GetTwitchClient()?.SendMessage(chatMessage.Channel, responseMessage);
            }
        }

        private void HandleStreamStats(ChatMessage chatMessage)
        {

            TimeSpan totalStreamDuration = new TimeSpan();

            using (var db = new DBConnection.Database())
            {
                var allStreams = db.Streams;
                
                // Collect stats
                var streamTotalCount = allStreams.Count();
                foreach (var stream in allStreams.Where(x => x.End != null))
                {
                    var totalStreamLength = (stream.End - stream.Start);
                    if (totalStreamLength.HasValue)
                    {
                        totalStreamDuration = totalStreamDuration.Add(totalStreamLength.Value);
                    }
                }

                var responseMessage = "[TMR] Stream Stats: ";

                if (Twitch.TwitchHandler.Instance.CurrentLiveStreamId != null)
                {
                    var currentStream =
                        allStreams.First(x => x.Id.Equals(Twitch.TwitchHandler.Instance.CurrentLiveStreamId));
                    var currentStreamDuration = (DateTime.Now.ToUniversalTime() - currentStream.Start);

                    responseMessage =
                        $"{responseMessage} Current stream up-time is {currentStreamDuration.Hours:n0}h {currentStreamDuration.Minutes:n0}m.";
                }

                responseMessage =
                    $"{responseMessage} I have seen a total of {streamTotalCount} streams that have a total duration of {totalStreamDuration.Days:n0} days & {totalStreamDuration.Hours:n0} hours!";

                Twitch.TwitchHandler.Instance.ChatService.GetTwitchClient()?.SendMessage(chatMessage.Channel, responseMessage);
            }
        }
    }
}
