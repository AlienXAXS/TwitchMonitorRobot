using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Models;
using LinqToDB;
using Org.BouncyCastle.Asn1.X509;

namespace TMRAgent.MySQL.Commands
{
    internal class UserManager
    {

        public void Handle(ChatMessage chatMessage, string[] parameters)
        {
            if (parameters.Length == 3)
            {
                var method = parameters[1].ToLower();
                var username = parameters[2].Replace("@","");

                switch (method)
                {
                    // Sets the user to be a moderator
                    case "mark_as_mod":
                        MarkUserAsModerator(chatMessage, username);
                        break;

                    // Sets the user to not be a moderator
                    case "mark_as_hive":
                        MarkUserAsHive(chatMessage, username);
                        break;
                }
            }
            else
            {
                Twitch.TwitchHandler.Instance.GetTwitchClient()
                    ?.SendMessage(chatMessage.Channel, "Invalid Usage: !!manage_user mark_as_mod/mark_as_hive Username");
            }
        }

        private void MarkUserAsModerator(ChatMessage chatMessage, string username)
        {
            var tc = Twitch.TwitchHandler.Instance.GetTwitchClient();
            var user = MySQL.MySqlHandler.Instance.Users.GetUserByUsername(username);

            if (user != null)
            {
                using (var db = new MySQL.DBConnection.Database())
                {
                    var userDbEntry = db.Users.Where(x => x.Id == user);
                    userDbEntry.Set(p => p.IsModerator, true)
                        .Update();
                }

                tc.SendMessage(chatMessage.Channel, $"[BOT] Successfully marked {username} as a Moderator within TMR.");
            }
            else
            {
                tc.SendMessage(chatMessage.Channel, $"[BOT] Unable to mark {username} as a moderator, I cannot find this user in my database");
            }
        }

        private void MarkUserAsHive(ChatMessage chatMessage, string username)
        {
            var tc = Twitch.TwitchHandler.Instance.GetTwitchClient();
            var user = MySQL.MySqlHandler.Instance.Users.GetUserByUsername(username);

            if (user != null)
            {
                using (var db = new MySQL.DBConnection.Database())
                {
                    var userDbEntry = db.Users.Where(x => x.Id == user);
                    userDbEntry.Set(p => p.IsModerator, false)
                        .Update();
                }

                tc.SendMessage(chatMessage.Channel, $"[BOT] Successfully marked {username} as a hive member within TMR.");
            }
            else
            {
                tc.SendMessage(chatMessage.Channel, $"[BOT] Unable to mark {username} as a hive member, I cannot find this user in my database");
            }
        }

        
    }
}
