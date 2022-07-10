using System.Linq;
using LinqToDB;
using TwitchLib.Client.Models;

namespace TMRAgent.MySQL.Commands
{
    internal class UserStatsCommand
    {
        public void Handle(ChatMessage chatMessage, string[] parameters)
        {
            using (var db = new DBConnection.Database())
            {
                Models.Users UserDB;
                if ( parameters.Length == 1 )
                {
                    UserDB = db.Users.DefaultIfEmpty(null).FirstOrDefault(x => x.TwitchId == int.Parse(chatMessage.UserId));
                } else
                {
                    UserDB = db.Users.DefaultIfEmpty(null).FirstOrDefault(x => x.Username == parameters[1]);
                }
                
                if ( UserDB == null )
                {
                    Twitch.TwitchHandler.Instance.GetTwitchClient().SendMessage(chatMessage.Channel, $"Sorry {chatMessage.DisplayName}, Something went wrong and I cannot get your stats!");
                    return;
                }

                var MessageCount = db.Messages.Count(x => x.UserId == UserDB.Id);
                var RedeemSpent = 0;
                foreach ( var bitRedeem in db.BitRedeems.Where(x => x.UserId == UserDB.Id))
                {
                    RedeemSpent = RedeemSpent + bitRedeem.Cost;
                }

                var ResponseMessage = $"{chatMessage.DisplayName}: {(parameters.Length == 2 ? $"{UserDB.Username} has" : "You've")} sent {MessageCount:n0} chat messages and redeemed {RedeemSpent:n0} Brain Cells!.";

                var cmdsPreTable = db.Commands.AsEnumerable().Where(x => x.UserId == UserDB.Id).GroupBy(x => x.Command);
                if (cmdsPreTable.Count() != 0)
                {
                    var mostUsedCommand = cmdsPreTable.OrderByDescending(x => x.Count()).First();

                    ResponseMessage = $"{ResponseMessage} {(parameters.Length == 2 ? "Their" : "Your")} most used command is {mostUsedCommand.Key} which {(parameters.Length == 2 ? "they've" : "you've")} used {mostUsedCommand.Count():n0} times!";
                }

                Twitch.TwitchHandler.Instance.GetTwitchClient().SendMessage(chatMessage.Channel, ResponseMessage);
            }
        }
    }
}
