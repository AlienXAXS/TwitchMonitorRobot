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
                    userDb = db.Users.DefaultIfEmpty(null).FirstOrDefault(x => x.Username == parameters[1]);
                }
                
                if ( userDb == null )
                {
                    Twitch.TwitchHandler.Instance.GetTwitchClient().SendMessage(chatMessage.Channel, $"Sorry {chatMessage.DisplayName}, Something went wrong and I cannot get your stats!");
                    return;
                }

                var messageCount = db.Messages.Count(x => x.UserId == userDb.Id);
                var redeemSpent = 0;
                foreach ( var bitRedeem in db.BitRedeems.Where(x => x.UserId == userDb.Id))
                {
                    redeemSpent = redeemSpent + bitRedeem.Cost;
                }

                var responseMessage = $"{chatMessage.DisplayName}: {(parameters.Length == 2 ? $"{userDb.Username} has" : "You've")} sent {messageCount:n0} chat messages and redeemed {redeemSpent:n0} Brain Cells!.";

                var cmdsPreTable = db.Commands.AsEnumerable().Where(x => x.UserId == userDb.Id).GroupBy(x => x.Command);
                if (cmdsPreTable.Count() != 0)
                {
                    var mostUsedCommand = cmdsPreTable.OrderByDescending(x => x.Count()).First();

                    responseMessage = $"{responseMessage} {(parameters.Length == 2 ? "Their" : "Your")} most used command is {mostUsedCommand.Key} which {(parameters.Length == 2 ? "they've" : "you've")} used {mostUsedCommand.Count():n0} times!";
                }

                Twitch.TwitchHandler.Instance.GetTwitchClient().SendMessage(chatMessage.Channel, responseMessage);
            }
        }
    }
}
