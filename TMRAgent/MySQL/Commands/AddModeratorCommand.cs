using System.Linq;
using LinqToDB;

namespace TMRAgent.MySQL.Commands
{
    internal class AddModeratorCommand
    {
        public void Handle(TwitchLib.Client.Models.ChatMessage message, string[] parameters)
        {
            var tc = Twitch.TwitchHandler.Instance.GetTwitchClient();

            if ( parameters.Length == 2)
            {
                var commandToBeAdded = parameters[1].ToLower();
                if ( !commandToBeAdded.StartsWith("!") )
                    commandToBeAdded = $"!{commandToBeAdded}";

                using ( var db = new MySQL.DBConnection.Database() )
                {
                    var dbCmd = db.ModCommands.DefaultIfEmpty(null).FirstOrDefault(x => x.Command.Equals(commandToBeAdded));
                    if ( dbCmd == null )
                    {
                        db.ModCommands
                            .Value(p => p.Command, commandToBeAdded)
                            .Insert();

                        tc.SendMessage(message.Channel, $"The command {commandToBeAdded} has been successfully added to the database");
                    } else
                    {
                        tc.SendMessage(message.Channel, $"The command {commandToBeAdded} already exists");
                    }
                }

            } else
            {
                tc.SendMessage(message.Channel, $"Invalid use of command");
            }

        }
    }
}
