using System;
using System.Linq;
using System.Reflection.Metadata;
using LinqToDB;
using LinqToDB.SqlQuery;
using TwitchLib.Client.Models;

namespace TMRAgent.MySQL.Commands
{
    internal class AlbionOnlineLookup
    {
        public void ProcessCommandMessage(TwitchLib.Client.Models.ChatMessage message, string[] parameters)
        {
            switch (parameters[0].ToLower())
            {
                // Albion Online Lookup
                case "!!aol":
                    HandleAlbionOnlineLookup(message, parameters);
                    break;

                // Albion Online Register
                case "!!aor":
                    HandleAlbionOnlineRegister(message, parameters);
                    break;
            }
        }

        private void HandleAlbionOnlineLookup(ChatMessage message, string[] parameters)
        {
            var tc = Twitch.TwitchHandler.Instance.ChatService.GetTwitchClient();
            if (tc == null) return;

            // Requesting someone elses albion online name
            if (parameters.Length == 2)
            {
                var lookupName = parameters[1].Replace("@", "");
                var userLookupResult = MySqlHandler.Instance.Users.GetTwitchIdFromDbId(MySqlHandler.Instance.Users.GetUserByUsername(lookupName));
                if (userLookupResult == null)
                {
                    tc.SendMessage(message.Channel, $"Unable to find a user by the name of \"{lookupName}\"");
                    return;
                }

                var albionName = FindAlbionAccountByTwitchId(userLookupResult.Value);
                if (albionName == null)
                {
                    tc.SendMessage(message.Channel, $"{lookupName} has yet to register their Albion Online Name!");
                }
                else
                {
                    tc.SendMessage(message.Channel, $"@{lookupName}'s Albion Online Name is: {albionName}");
                }
            }
            else if (parameters.Length == 1)
            {
                // Requesting our own albion online name
                var ownAlbionName = FindAlbionAccountByTwitchId(int.Parse(message.UserId));
                if (ownAlbionName == null)
                {
                    tc.SendMessage(message.Channel, $"@{message.Username}, I am unable to find your Albion Online Username, register it with !!aor AlbionName");
                }
                else
                {
                    tc.SendMessage(message.Channel, $"@{message.Username}'s Albion Online Username is: {ownAlbionName}");
                }
            }
        }

        private void HandleAlbionOnlineRegister(ChatMessage message, string[] parameters)
        {
            var tc = Twitch.TwitchHandler.Instance.ChatService.GetTwitchClient();

            switch (parameters.Length)
            {
                case 2:
                    // Self register
                    var selfRegisterAlbionUserName = parameters[1];
                    try
                    {
                        AddOrUpdateAlbionNameToDatabase(int.Parse(message.UserId), selfRegisterAlbionUserName);
                        tc.SendMessage(message.Channel, $"Successfully registered Albion Online Name \"{selfRegisterAlbionUserName}\" for Twitch user @{message.Username}");
                    }
                    catch (Exception ex)
                    {
                        tc.SendMessage(message.Channel, $"Fatal Error while trying to add Albion Username to Twitch User: {ex.Message}");
                    }
                    break;

                case 3:
                    // Mod register
                    if (message.IsModerator || message.IsBroadcaster)
                    {
                        var modRegisterTwitchUserName = parameters[1].Replace("@","");
                        var modRegisterAlbionUserName = parameters[2];

                        var modRegisterTwitchUser = MySqlHandler.Instance.Users.GetTwitchIdFromDbId(MySqlHandler.Instance.Users.GetUserByUsername(modRegisterTwitchUserName));

                        if (modRegisterTwitchUser == null)
                        {
                            tc.SendMessage(message.Channel, $"Unable to find twitch user {modRegisterAlbionUserName}");
                            return;
                        }

                        try
                        {
                            AddOrUpdateAlbionNameToDatabase(modRegisterTwitchUser.Value, modRegisterAlbionUserName);
                            tc.SendMessage(message.Channel, $"Successfully registered Albion Online Name \"{modRegisterAlbionUserName}\" for Twitch user @{modRegisterTwitchUserName}");
                        }
                        catch (Exception ex)
                        {
                            tc.SendMessage(message.Channel, $"Fatal Error while trying to add Albion Username to Twitch User: {ex.Message}");
                        }

                    }
                    break;

                default:
                    tc.SendMessage(message.Channel, "Unknown usage for command !!aor (Albion Online Register)");
                    tc.SendMessage(message.Channel, "Usage: !!aor AlbionName | !!aor TwitchName AlbionName (Mod Only)");
                    break;
            }

        }

        private void AddOrUpdateAlbionNameToDatabase(int TwitchId, string AlbionUsername)
        {
            using (var db = new DBConnection.Database())
            {
                var existingEntry = db.AlbionOnlineLookups.Where(x => x.TwitchId.Equals(TwitchId));
                if (existingEntry.Any())
                {
                    existingEntry.Set(p => p.AlbionName, AlbionUsername).Update();
                    return;
                }

                db.AlbionOnlineLookups
                    .Value(p => p.TwitchId, TwitchId)
                    .Value(p => p.AlbionName, AlbionUsername)
                    .Insert();
            }
        }

        private string? FindAlbionAccountByTwitchId(int TwitchId)
        {
            var result = MySqlHandler.Instance.AlbionOnlineLookup.GetAlbionUserByTwitchId(TwitchId);
            return result;
        }
    }
}
