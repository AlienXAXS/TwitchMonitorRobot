using System.Linq;
using LinqToDB;

namespace TMRAgent.MySQL.Commands
{
    internal class RemoveModeratorCommand
    {
        public void Handle(TwitchLib.Client.Models.ChatMessage message, string[] parameters)
        {
            var tc = Twitch.TwitchHandler.Instance.GetTwitchClient();

            if (parameters.Length == 2)
            {
                var commandToBeRemoved = parameters[1].ToLower();
                if (!commandToBeRemoved.StartsWith("!"))
                    commandToBeRemoved = $"!{commandToBeRemoved}";

                using (var db = new DBConnection.Database())
                {
                    var dbCmd = db.ModCommands.DefaultIfEmpty(null).FirstOrDefault(x => x.Command.Equals(commandToBeRemoved));
                    if (dbCmd != null)
                    {
                        db.ModCommands.Where(x => x.Command == commandToBeRemoved).Delete();

                        tc.SendMessage(message.Channel, $"The command {commandToBeRemoved} has been successfully removed from the database");
                    }
                    else
                    {
                        tc.SendMessage(message.Channel, $"The command {commandToBeRemoved} does not exist");
                    }
                }

            }
            else
            {
                tc.SendMessage(message.Channel, $"Invalid use of command");
            }
        }
    }
}
