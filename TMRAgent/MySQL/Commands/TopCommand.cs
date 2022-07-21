using LinqToDB.Data;
using System;
using System.Linq;
using TwitchLib.Client.Models;

namespace TMRAgent.MySQL.Commands
{
    internal class TopCommand
    {
        internal void Handle(ChatMessage chatMessage, string[] parameters)
        {
            try
            {
                var tc = Twitch.TwitchHandler.Instance.ChatService.GetTwitchClient();

                if (parameters.Length == 2)
                {
                    var type = parameters[1].ToLower();
                    switch (type)
                    {
                        case "redeem":
                            using (var db = new DBConnection.Database())
                            {
                                var query = db.Query<dynamic>("SELECT count(bit_redeems.name) as Count, SUM(cost) AS RedeemCost, username as Username FROM bit_redeems LEFT JOIN users ON bit_redeems.userid = users.id GROUP BY users.username ORDER BY RedeemCost DESC LIMIT 3;").ToList();

                                if (query.Count > 0)
                                {

                                    var returnMsg = "Top Three Mind Bit Spenders: ";
                                    foreach (var item in query)
                                    {
                                        returnMsg = $"{returnMsg}@{item.Username} {item.RedeemCost:n0} bits | ";
                                    }

                                    returnMsg = returnMsg.Substring(0, returnMsg.Length - 2).Trim();

                                    tc?.SendMessage(Twitch.ConfigurationHandler.Instance.Configuration.TwitchChat.ChannelName, returnMsg);
                                };
                            }
                            break;

                        case "message":

                            break;
                    }
                }
            } catch (Exception ex)
            {
                ConsoleUtil.WriteToConsole($"[Error] TopCommand.Handle: {ex.Message}", ConsoleUtil.LogLevel.Error, ConsoleColor.Red);
            }
        }
    }
}
